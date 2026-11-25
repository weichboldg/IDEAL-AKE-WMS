using System.Data;
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// v1.99.71 (Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage, ADR 0036): Lager und Lagerplaetze per Direkt-SQL in Sage anlegen, ueber die
/// bestehende <c>ConnectionStrings:SageConnection</c> (Dienstkonto mit Objekt-GRANTs, Freigabe-Antwort 2). Je Aufruf EINE Transaktion,
/// idempotent (Pruefen und Anlegen unter UPDLOCK/HOLDLOCK), Ergebnis erst nach dem COMMIT (S5). Der SQL-Text ist wortgleich aus der Spec.
/// Fachliche SQL-Fehler = <see cref="SageLagerplatzAnlageArt.Abgelehnt"/>; technische (<see cref="IstTechnisch"/>) = Exception.
/// </summary>
public sealed class SqlSageLagerplatzAnlage : ISageLagerplatzAnlage
{
    // ponytail: Mandant fest 1 wie die Reader, Setting erst bei zweitem Mandanten
    internal const short Mandant = 1;
    internal const int CommandTimeoutSeconds = 30;

    internal const string AbweichungZusatz = "Sage-Tabelle oder -Prozedur weicht ab – Prüfabfragen P-1 bis P-3 der Spec, Anlage am Standort abschalten";

    /// <summary>Sage-Schreibbefehl je Platz (Spec „Sage-Schreibbefehl je Platz“, Ableitung Anhang 8.2).</summary>
    internal const string PlatzSql = """
        SET XACT_ABORT ON;
        SET DEADLOCK_PRIORITY LOW;                                            -- S4: WMS ist Deadlock-Opfer, nicht der Sage-Dialog
        DECLARE @LagerBez varchar(40), @B1 varchar(50), @B2 varchar(50), @B3 varchar(50), @Aktiv smallint, @Ebenen smallint,
                @InvUeb datetime, @InvZaehl datetime, @InvJahr smallint, @PlatzID int, @Kurz varchar(50), @Bez varchar(200), @n int,
                @Angelegt bit = 0;
        BEGIN TRAN;
          SELECT @LagerBez = o.Bezeichnung, @Aktiv = o.Aktiv,
                 @B1 = t.BezeichnungDimension1, @B2 = t.BezeichnungDimension2, @B3 = t.BezeichnungDimension3
          FROM KHKLagerorte o WITH (UPDLOCK, HOLDLOCK)
          JOIN KHKLagertechnikenArten t ON t.Lagertechnikkennung = o.Lagertechnik
          WHERE o.Mandant = @Mandant AND o.Lagerkennung = @Lager;
          IF @@ROWCOUNT = 0 THROW 50010, 'Lager in Sage nicht gefunden, ohne Lagertechnik oder mit unbekannter Lagertechnik', 1;   -- H5: Lagertechnik nullable
          IF @Aktiv <> -1 THROW 50013, 'Lager ist in Sage inaktiv', 1;      -- inaktive Lager liest der Abgleich nicht (SageLagerplatzReader.cs:28)
          SET @Ebenen = IIF(@B1 IS NULL,0,1) + IIF(@B2 IS NULL,0,1) + IIF(@B3 IS NULL,0,1);
          IF @Ebenen = 0 THROW 50011, 'Lagertechnik ohne Dimensionen - keine Plaetze moeglich', 1;
          IF @D1 < 1 OR (@Ebenen >= 2 AND @D2 < 1) OR (@Ebenen < 2 AND @D2 <> 0) OR (@Ebenen >= 3 AND @D3 < 1) OR (@Ebenen < 3 AND @D3 <> 0)
             THROW 50012, 'Koordinaten passen nicht zur Lagertechnik', 1;     -- Klartext im C# mit @B1..@B3 ergänzen
          SET @Kurz = CONCAT(@Lager, ';', @D1, ';', @D2, ';', @D3);
          SET @Bez  = CONCAT(@LagerBez, ';', @B1, ' ', @D1, IIF(@Ebenen >= 2, CONCAT(';', @B2, ' ', @D2), ''), IIF(@Ebenen >= 3, CONCAT(';', @B3, ' ', @D3), ''));
          IF LEN(@Bez) > 110 THROW 50015, 'Platzbezeichnung laenger als 110 Zeichen', 1;
          -- F5: vorhanden / Konflikt (kein Unique-Index in Sage, Anhang 2.4); Kurzbezeichnung mandantenweit (WMS-Code ist global eindeutig)
          SELECT @n = COUNT(*), @PlatzID = MIN(PlatzID) FROM KHKLagerplaetze WITH (UPDLOCK, HOLDLOCK)
          WHERE Mandant = @Mandant
            AND ((Lagerkennung = @Lager AND Dimension1 = @D1 AND Dimension2 = @D2 AND Dimension3 = @D3) OR Kurzbezeichnung = @Kurz);
          IF @n > 1 OR (@n = 1 AND NOT EXISTS (SELECT 1 FROM KHKLagerplaetze WHERE PlatzID = @PlatzID AND Mandant = @Mandant AND Lagerkennung = @Lager
                                                AND Dimension1 = @D1 AND Dimension2 = @D2 AND Dimension3 = @D3 AND Kurzbezeichnung = @Kurz))
             THROW 50014, 'Konflikt in Sage: Koordinaten oder Kurzbezeichnung schon anders belegt', 1;
          IF @n = 0
          BEGIN
            SELECT @InvUeb = LetztesInventurUebernahmedatum, @InvZaehl = LetztesInventurZaehldatum, @InvJahr = LetztesInventurjahr
            FROM KHKLagerplaetze WHERE Mandant = @Mandant AND Lagerkennung = @Lager AND Dimension1 = 0 AND Dimension2 = 0 AND Dimension3 = 0;
            EXEC spSysTan @Table = 'KHKLagerplaetze', @Mandant = @Mandant, @Increment = 1, @Tan = @PlatzID OUTPUT;   -- nie MAX+1 (Anhang 2.1)
            INSERT INTO KHKLagerplaetze (PlatzID, Mandant, Lagerkennung, Dimension1, Dimension2, Dimension3, Dimensionsebene, Kurzbezeichnung,
                   Platzbezeichnung, IstGesperrt, Laenge, Breite, Hoehe, Volumen, Tragkraft, Status, Inventurmethode, IstGesperrtInventur,
                   LetztesInventurUebernahmedatum, LetztesInventurZaehldatum, LetztesInventurjahr)
            VALUES (@PlatzID, @Mandant, @Lager, @D1, @D2, @D3, @Ebenen, @Kurz, @Bez, 0, 0, 0, 0, 0, 0, 0, 0, 0, @InvUeb, @InvZaehl, @InvJahr);
            UPDATE o SET AnzahlDimension1 = x.M1, AnzahlDimension2 = x.M2, AnzahlDimension3 = x.M3, AnzahlLagerplaetze = x.Anz, IstPlatzverwaltet = -1
            FROM KHKLagerorte o CROSS APPLY (SELECT MAX(Dimension1) M1, MAX(Dimension2) M2, MAX(Dimension3) M3, COUNT(*) Anz
                                             FROM KHKLagerplaetze p WHERE p.Mandant = o.Mandant AND p.Lagerkennung = o.Lagerkennung) x
            WHERE o.Mandant = @Mandant AND o.Lagerkennung = @Lager;          -- Zähler aus den Daten, in derselben Transaktion (Anhang 5.4, besser als Sage)
            SET @Angelegt = 1;
          END
        COMMIT;
        -- S5: Ergebnis erst nach dem COMMIT; S6: Sperr-/Masswerte fuer die Uebernahme; Antwort 8: Ziel-DB fuer das Protokoll
        SELECT p.PlatzID, @Angelegt AS Angelegt, p.Platzbezeichnung, p.IstGesperrt, p.IstGesperrtInventur, p.Laenge, p.Breite, p.Hoehe, p.Volumen,
               p.Tragkraft, CONCAT(@@SERVERNAME, '/', DB_NAME()) AS SageDb
        FROM KHKLagerplaetze p WHERE p.PlatzID = @PlatzID AND p.Mandant = @Mandant;
        """;

    /// <summary>Sage-Schreibbefehl je Lager (Spec F3a, Ableitung Anhang 8.1, ohne USER_-Spalten).</summary>
    internal const string LagerSql = """
        SET XACT_ABORT ON;
        SET DEADLOCK_PRIORITY LOW;                                            -- S4
        DECLARE @PlatzID int, @Bez0 varchar(40), @Tech0 varchar(10), @Aktiv0 smallint, @Kurz varchar(50) = CONCAT(@Lager, ';0;0;0'),
                @Angelegt bit = 0, @BezOut varchar(40);
        BEGIN TRAN;
          SELECT @Bez0 = Bezeichnung, @Tech0 = Lagertechnik, @Aktiv0 = Aktiv
          FROM KHKLagerorte WITH (UPDLOCK, HOLDLOCK) WHERE Mandant = @Mandant AND Lagerkennung = @Lager;
          IF @@ROWCOUNT = 1
          BEGIN
            IF NOT (@Bez0 = @Bez AND @Tech0 = @Technik AND @Aktiv0 = -1)
               THROW 50003, 'Lagerkennung in Sage schon anders belegt oder inaktiv', 1;
            SELECT @PlatzID = PlatzID FROM KHKLagerplaetze
            WHERE Mandant = @Mandant AND Lagerkennung = @Lager AND Dimension1 = 0 AND Dimension2 = 0 AND Dimension3 = 0;
            SET @BezOut = @Bez0;                                                                  -- vorhanden (auch ohne Freigabe, H4)
          END
          ELSE
          BEGIN
            IF @NeuanlageErlaubt = 0
               THROW 50006, 'Lagertechnik fuer die Lager-Anlage an diesem Standort nicht freigegeben', 1;   -- H4: nur fuer die Neuanlage
            IF NOT EXISTS (SELECT 1 FROM KHKLagertechnikenArten WHERE Lagertechnikkennung = @Technik) THROW 50001, 'Lagertechnik in Sage unbekannt', 1;
            IF NOT EXISTS (SELECT 1 FROM KHKGruppen WHERE Mandant = 0 AND Typ = 40025 AND Tag = 'SWF') THROW 50002, 'WarenflussKZ SWF in Sage unbekannt', 1;
            IF EXISTS (SELECT 1 FROM KHKLagerplaetze WITH (UPDLOCK, HOLDLOCK) WHERE Mandant = @Mandant AND Kurzbezeichnung = @Kurz)
               THROW 50005, 'Kurzbezeichnung des Basisplatzes in Sage schon vergeben', 1;      -- AKE: Hauptlager;0;0;0 (P-6)
            -- S1/P-9a: KHKArtikelVariantenLagerplatz hat keine Lagerkennung, nur PlatzID -> Join ueber den Platz (Sage prueft das ebenso, Anhang 4.1)
            IF EXISTS (SELECT 1 FROM KHKArtikelVariantenLagerplatz a JOIN KHKLagerplaetze p ON p.PlatzID = a.PlatzID AND p.Mandant = a.Mandant
                       WHERE p.Mandant = @Mandant AND p.Lagerkennung = @Lager)
               THROW 50004, 'Artikel-Lagerplatz-Zuordnungen zu Plaetzen dieser Lagerkennung vorhanden', 1;
            -- S1: verwaiste Plaetze derselben Kennung (ohne Lager) wuerden sonst still Teil des neuen Lagers
            IF EXISTS (SELECT 1 FROM KHKLagerplaetze WITH (UPDLOCK, HOLDLOCK) WHERE Mandant = @Mandant AND Lagerkennung = @Lager)
               THROW 50007, 'Lagerplaetze mit dieser Lagerkennung ohne Lager in Sage vorhanden', 1;
            EXEC spSysTan @Table = 'KHKLagerplaetze', @Mandant = @Mandant, @Increment = 1, @Tan = @PlatzID OUTPUT;
            INSERT INTO KHKLagerorte
              (Lagerkennung, Mandant, Bezeichnung, Lagertechnik, WarenflussKZ, Aktiv, AnzahlLagerplaetze, IstDispoLager, IstFremdlager,
               IstKommissionslager, IstSperrlager, IstWELager, IstHauptWELager, IstWALager, IstHauptWALager, MitArtikelzuordnung,
               Einlagerungsregel, Einlagerungsprioritaet, Auslagerungsregel, Auslagerungsprioritaet, Memo)
            VALUES (@Lager, @Mandant, @Bez, @Technik, 'SWF', -1, 1, -1, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, NULL);  -- SWF: alle WE/WA-Kennzeichen 0
            INSERT INTO KHKLagerplaetze (PlatzID, Mandant, Lagerkennung, Dimension1, Dimension2, Dimension3, Dimensionsebene, Kurzbezeichnung,
                   Platzbezeichnung, IstGesperrt, Laenge, Breite, Hoehe, Volumen, Tragkraft, Status, Inventurmethode, IstGesperrtInventur)
            VALUES (@PlatzID, @Mandant, @Lager, 0, 0, 0, 0, @Kurz, @Bez, 0, 0, 0, 0, 0, 0, 0, 0, 0);       -- Inventurdaten NULL wie Sage (4.3)
            SELECT @Angelegt = 1, @BezOut = @Bez;
          END
        COMMIT;
        SELECT @PlatzID AS PlatzID, @Angelegt AS Angelegt, @BezOut AS Platzbezeichnung, CONCAT(@@SERVERNAME, '/', DB_NAME()) AS SageDb;   -- S5: nach COMMIT
        """;

    /// <summary>Nur bei 50012: Dimensionsnamen der Lagertechnik fuer den Klartext (reines SELECT).</summary>
    internal const string DimensionenSql = """
        SELECT t.BezeichnungDimension1, t.BezeichnungDimension2, t.BezeichnungDimension3
        FROM KHKLagerorte o JOIN KHKLagertechnikenArten t ON t.Lagertechnikkennung = o.Lagertechnik
        WHERE o.Mandant = @Mandant AND o.Lagerkennung = @Lager
        """;

    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlSageLagerplatzAnlage> _logger;

    public SqlSageLagerplatzAnlage(IConfiguration configuration, ILogger<SqlSageLagerplatzAnlage> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SageLagerplatzAnlageErgebnis> AnlegenAsync(string lagerkennung, int d1, int d2, int d3, CancellationToken ct)
    {
        await using var conn = await OeffnenAsync(ct);
        await using var cmd = PlatzBefehl(conn, lagerkennung, d1, d2, d3);
        try
        {
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return SageLagerplatzAnlageErgebnis.Abgelehnt("Sage lieferte kein Ergebnis");
            return new SageLagerplatzAnlageErgebnis(
                Bit(r, "Angelegt") ? SageLagerplatzAnlageArt.Angelegt : SageLagerplatzAnlageArt.Vorhanden,
                Int(r, "PlatzID"), Text(r, "Platzbezeichnung"), null,
                IstGesperrt: Bool(r, "IstGesperrt"), IstGesperrtInventur: Bool(r, "IstGesperrtInventur"),
                Laenge: Dez(r, "Laenge"), Breite: Dez(r, "Breite"), Hoehe: Dez(r, "Hoehe"), Volumen: Dez(r, "Volumen"), Tragkraft: Dez(r, "Tragkraft"),
                SageDb: Text(r, "SageDb"));
        }
        catch (SqlException ex) when (!IstTechnisch(ex))
        {
            var dimensionen = ex.Number == 50012 ? await DimensionenAsync(conn, lagerkennung, ct) : null;
            return SageLagerplatzAnlageErgebnis.Abgelehnt(Klartext(ex.Number, ex.Message, dimensionen));
        }
    }

    public async Task<SageLagerplatzAnlageErgebnis> LagerAnlegenAsync(string lagerkennung, string bezeichnung, string lagertechnik,
        bool neuanlageErlaubt, CancellationToken ct)
    {
        await using var conn = await OeffnenAsync(ct);
        await using var cmd = LagerBefehl(conn, lagerkennung, bezeichnung, lagertechnik, neuanlageErlaubt);
        try
        {
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return SageLagerplatzAnlageErgebnis.Abgelehnt("Sage lieferte kein Ergebnis");
            return new SageLagerplatzAnlageErgebnis(
                Bit(r, "Angelegt") ? SageLagerplatzAnlageArt.Angelegt : SageLagerplatzAnlageArt.Vorhanden,
                Int(r, "PlatzID"), Text(r, "Platzbezeichnung"), null, SageDb: Text(r, "SageDb"));
        }
        catch (SqlException ex) when (!IstTechnisch(ex))
        {
            return SageLagerplatzAnlageErgebnis.Abgelehnt(Klartext(ex.Number, ex.Message, null));
        }
    }

    private async Task<SqlConnection> OeffnenAsync(CancellationToken ct)
    {
        var cs = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");
        var conn = new SqlConnection(cs);
        try
        {
            await conn.OpenAsync(ct);
            return conn;
        }
        catch
        {
            await conn.DisposeAsync();
            throw;
        }
    }

    /// <summary>Typisierte Parameter (S4): varchar statt nvarchar, damit die Vergleiche unter der Sage-Kollation Index-Seeks bleiben.</summary>
    internal static SqlCommand PlatzBefehl(SqlConnection? conn, string lager, int d1, int d2, int d3)
    {
        var cmd = new SqlCommand(PlatzSql, conn) { CommandTimeout = CommandTimeoutSeconds };
        cmd.Parameters.Add("@Lager", SqlDbType.VarChar, 10).Value = lager;
        cmd.Parameters.Add("@D1", SqlDbType.Int).Value = d1;
        cmd.Parameters.Add("@D2", SqlDbType.Int).Value = d2;
        cmd.Parameters.Add("@D3", SqlDbType.Int).Value = d3;
        cmd.Parameters.Add("@Mandant", SqlDbType.SmallInt).Value = Mandant;
        return cmd;
    }

    internal static SqlCommand LagerBefehl(SqlConnection? conn, string lager, string bezeichnung, string technik, bool neuanlageErlaubt)
    {
        var cmd = new SqlCommand(LagerSql, conn) { CommandTimeout = CommandTimeoutSeconds };
        cmd.Parameters.Add("@Lager", SqlDbType.VarChar, 10).Value = lager;
        cmd.Parameters.Add("@Bez", SqlDbType.VarChar, 40).Value = bezeichnung;
        cmd.Parameters.Add("@Technik", SqlDbType.VarChar, 10).Value = technik;
        cmd.Parameters.Add("@Mandant", SqlDbType.SmallInt).Value = Mandant;
        cmd.Parameters.Add("@NeuanlageErlaubt", SqlDbType.Bit).Value = neuanlageErlaubt;
        return cmd;
    }

    private async Task<string?> DimensionenAsync(SqlConnection conn, string lager, CancellationToken ct)
    {
        try
        {
            await using var cmd = new SqlCommand(DimensionenSql, conn) { CommandTimeout = CommandTimeoutSeconds };
            cmd.Parameters.Add("@Lager", SqlDbType.VarChar, 10).Value = lager;
            cmd.Parameters.Add("@Mandant", SqlDbType.SmallInt).Value = Mandant;
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return null;
            var namen = Enumerable.Range(0, 3).Where(i => !r.IsDBNull(i)).Select(i => r.GetString(i).Trim()).ToList();
            return namen.Count == 0 ? null : string.Join(", ", namen);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Dimensionen der Lagertechnik von {Lager} nicht lesbar.", lager);
            return null;
        }
    }

    /// <summary>Fehlerklassen S3 (abschliessend): technisch = Timeout, Deadlock, Lock-Timeout, fehlendes Recht, DB/Anmeldung, Transport/Netz,
    /// Schweregrad &gt;= 20, sowie Nicht-SqlException aus dem Verbindungsaufbau. Alles andere (THROW 500xx, 515, 207, 208, 2812, 8152, 2627 …)
    /// ist fachlich – ein Datenfehler an einem Eintrag blockiert nie die Warteschlange (AK 34).</summary>
    public static bool IstTechnisch(Exception ex) => ex switch
    {
        SqlException s => IstTechnisch(s.Number, s.Class),
        InvalidOperationException or IOException or TimeoutException => true,
        _ => false,
    };

    internal static readonly int[] TechnischeNummern = { -2, 1205, 1222, 229, 230, 262, 4060, 18456, 233, 10053, 10054, 10060, 53, -1 };

    public static bool IstTechnisch(int number, byte klasse) => klasse >= 20 || TechnischeNummern.Contains(number);

    /// <summary>Klartext eines fachlichen SQL-Fehlers fuer Outbox/Liste (max. 1000 Zeichen kappt der Aufrufer).</summary>
    public static string Klartext(int number, string message, string? dimensionen)
    {
        var text = $"Sage-Fehler {number}: {message}";
        if (number == 50012)
            text += dimensionen is null
                ? " (genutzte Dimensionen ab 1, nicht genutzte 0)"
                : $" (Dimensionen der Lagertechnik: {dimensionen}; genutzte ab 1, nicht genutzte 0)";
        if (number is 515 or 207 or 208 or 2812)
            text += " – " + AbweichungZusatz;
        return text;
    }

    private static bool Bit(SqlDataReader r, string spalte) => r[spalte] is not DBNull and var v && Convert.ToBoolean(v);
    private static int? Int(SqlDataReader r, string spalte) => r[spalte] is DBNull ? null : Convert.ToInt32(r[spalte]);
    private static string? Text(SqlDataReader r, string spalte) => r[spalte] is DBNull ? null : Convert.ToString(r[spalte]);
    private static bool? Bool(SqlDataReader r, string spalte) => r[spalte] is DBNull ? null : Convert.ToInt32(r[spalte]) != 0;   // KHKBoolean -1/0
    private static decimal? Dez(SqlDataReader r, string spalte) =>
        r[spalte] is DBNull ? null : Math.Round(Convert.ToDecimal(r[spalte]), 3, MidpointRounding.AwayFromZero);   // wie SdataLagerplatzReader.Runde
}
