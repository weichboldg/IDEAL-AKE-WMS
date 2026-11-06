using IdealAkeWms.Models;
using IDEALAKEWMSService.Common;
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// View-/SQL-Weg des FA-Struktur-Abgleichs (A5). Der Lese-Code ist UNVERAENDERT aus <see cref="FaHierarchySyncService"/>
/// herausgezogen (Spec 2026-11-06-sage-sdata-stammdaten-sync, E4a/F6): View-Namen aus <c>Sync:FaHierarchyListeViewName</c>/
/// <c>…InfosViewName</c> (Whitelist + QUOTENAME), Existenzpruefung beider Views, Roh-Zeilenzahlen fuer den Empty-Guard,
/// Knoten-Select mit EXISTS auf FAInfos und FAInfos-Select (<see cref="FaHierarchySql"/>).
/// </summary>
public sealed class SageFaStrukturReader : ISageFaStrukturReader
{
    private readonly IConfiguration _config;

    public SageFaStrukturReader(IConfiguration config)
    {
        _config = config;
    }

    public async Task<FaStrukturLesung> LesenAsync(CancellationToken ct = default)
    {
        // 1) View-Namen aus ServiceSettings lesen + validieren (Whitelist + QUOTENAME). Ungueltig -> FaHierarchyViewNameException, kein SQL.
        var listeRaw = await ServiceSettings.GetValueAsync(_config, "Sync:FaHierarchyListeViewName", ct);
        if (string.IsNullOrWhiteSpace(listeRaw))
            listeRaw = "[vw_IDEAL_AKE_WMS_FAListe]";
        var infosRaw = await ServiceSettings.GetValueAsync(_config, "Sync:FaHierarchyInfosViewName", ct);
        if (string.IsNullOrWhiteSpace(infosRaw))
            infosRaw = "[vw_IDEAL_AKE_WMS_FAInfos]";

        var quotedListe = FaHierarchySql.ValidateAndQuote(listeRaw);
        var quotedInfos = FaHierarchySql.ValidateAndQuote(infosRaw);

        var sageConnStr = _config.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");

        await using var conn = new SqlConnection(sageConnStr);
        await conn.OpenAsync(ct);

        // 1b) Existenz beider Views VOR jedem Read (Spec 2026-10-14-ideal-sage-views-umstellung, Rueckfrage 6):
        //     Eine fehlende FA-View ist ein Ausfall der FA-Struktur und darf nicht still sein — throw in den catch
        //     des Abgleichs (LogError + FinishFailed), die Fehlermail schickt RunResilientAsync. Klare Meldung statt
        //     SqlException; die Zieltabellen bleiben unberuehrt.
        foreach (var quoted in new[] { quotedListe, quotedInfos })
            if (!await FaHierarchySql.SourceExistsAsync(conn, quoted, ct))
                throw new InvalidOperationException(
                    $"{FaHierarchySql.SourceMissingMessage(quoted)} — FA-Struktur-Sync abgebrochen.");

        // 2) Empty-Guard: ungefilterte Roh-Zeilenzahlen BEIDER Views VOR jeder Mutation
        //    (sonst maskiert die Existenzpruefung einen leeren FAInfos-Read als "0 Struktur-Zeilen").
        var listeCount = await ScalarCountAsync(conn, FaHierarchySql.BuildCountSql(quotedListe), ct);
        var infosCount = await ScalarCountAsync(conn, FaHierarchySql.BuildCountSql(quotedInfos), ct);
        if (listeCount == 0 || infosCount == 0)
            return new FaStrukturLesung(new(), new(), listeCount, infosCount);

        // 3) Zielzeilen im Speicher aufbauen (Existenzpruefung im SQL, Anforderung 2).
        var syncedAt = DateTime.Now;
        var nodes = await ReadNodesAsync(conn, FaHierarchySql.BuildNodeSelectSql(quotedListe, quotedInfos), syncedAt, ct);
        var infos = await ReadOrderInfosAsync(conn, FaHierarchySql.BuildInfosSelectSql(quotedInfos), syncedAt, ct);
        return new FaStrukturLesung(nodes, infos, listeCount, infosCount);
    }

    private static async Task<long> ScalarCountAsync(SqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        var v = await cmd.ExecuteScalarAsync(ct);
        return v is null or DBNull ? 0L : Convert.ToInt64(v);
    }

    private static async Task<List<FaHierarchyNode>> ReadNodesAsync(
        SqlConnection conn, string sql, DateTime syncedAt, CancellationToken ct)
    {
        var result = new List<FaHierarchyNode>();
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await using var r = await cmd.ExecuteReaderAsync(ct);

        int oHauptFA = r.GetOrdinal("HauptFA"), oVaterFA = r.GetOrdinal("VaterFA"),
            oSubFA = r.GetOrdinal("SubFA"), oPosition = r.GetOrdinal("Position"),
            oHauptArtnr = r.GetOrdinal("HauptArtnr"), oArtnr = r.GetOrdinal("Artnr"),
            oMatchcode = r.GetOrdinal("Matchcode"), oBez1 = r.GetOrdinal("Bezeichnung1"),
            oBez2 = r.GetOrdinal("Bezeichnung2"), oSoll = r.GetOrdinal("Sollmenge"),
            oFert = r.GetOrdinal("Fertigungmenge"), oBesch = r.GetOrdinal("Beschaffungsartikel"),
            oArtGrp = r.GetOrdinal("Artikelgruppe"), oHlp = r.GetOrdinal("Hauptlagerplatz"),
            oBemPN = r.GetOrdinal("BemerkungPN"), oFInfoPN = r.GetOrdinal("FertigungsInfoPN"),
            oKomm = r.GetOrdinal("Kommissionieren"), oArbBer = r.GetOrdinal("Arbeitsbereich"),
            oArbSchr = r.GetOrdinal("Arbeitsschritte"), oArtTyp = r.GetOrdinal("Artikeltyp"),
            oBeschichtet = r.GetOrdinal("Beschichtet"), oMaterial = r.GetOrdinal("Material"),
            oBreite = r.GetOrdinal("Breite"), oHoehe = r.GetOrdinal("Hoehe"),
            oTiefe = r.GetOrdinal("Tiefe"), oEK = r.GetOrdinal("EKBedarf"),
            oVM = r.GetOrdinal("VMBedarf");

        while (await r.ReadAsync(ct))
        {
            result.Add(new FaHierarchyNode
            {
                HauptFA = Int32(r, oHauptFA),
                VaterFA = IntN(r, oVaterFA),
                SubFA = Int32(r, oSubFA),
                Position = IntN(r, oPosition),
                HauptArtnr = Str(r, oHauptArtnr),
                Artnr = Str(r, oArtnr),
                Matchcode = Str(r, oMatchcode),
                Bezeichnung1 = Str(r, oBez1),
                Bezeichnung2 = Str(r, oBez2),
                Sollmenge = Dec(r, oSoll),
                Fertigungmenge = Dec(r, oFert),
                Beschaffungsartikel = Bool(r, oBesch),
                Artikelgruppe = Str(r, oArtGrp),
                Hauptlagerplatz = Str(r, oHlp),
                BemerkungPN = Str(r, oBemPN),
                FertigungsInfoPN = Str(r, oFInfoPN),
                Kommissionieren = Str(r, oKomm),
                Arbeitsbereich = Str(r, oArbBer),
                Arbeitsschritte = Str(r, oArbSchr),
                Artikeltyp = Str(r, oArtTyp),
                Beschichtet = Bool(r, oBeschichtet),
                Material = Str(r, oMaterial),
                Breite = Dec(r, oBreite),
                Hoehe = Dec(r, oHoehe),
                Tiefe = Dec(r, oTiefe),
                EKBedarf = Bool(r, oEK),
                VMBedarf = Str(r, oVM),
                SyncedAt = syncedAt,
            });
        }
        return result;
    }

    private static async Task<List<FaHierarchyOrderInfo>> ReadOrderInfosAsync(
        SqlConnection conn, string sql, DateTime syncedAt, CancellationToken ct)
    {
        var result = new List<FaHierarchyOrderInfo>();
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await using var r = await cmd.ExecuteReaderAsync(ct);

        int oHauptFA = r.GetOrdinal("HauptFA"), oMontage = r.GetOrdinal("MontageAbteilung"),
            oABNr = r.GetOrdinal("ABNr"), oPos = r.GetOrdinal("Pos"), oKunde = r.GetOrdinal("Kunde"),
            oStatus = r.GetOrdinal("Status"), oDienst = r.GetOrdinal("Dienstleister"),
            oRAL = r.GetOrdinal("RAL"), oBemUhr = r.GetOrdinal("Bemerkung_Uhrzeit"),
            oKO = r.GetOrdinal("KO_Termin"), oFE = r.GetOrdinal("FE_Termin"),
            oStartB = r.GetOrdinal("Start_Beschichtung"), oRetour = r.GetOrdinal("Beschichten_Retour"),
            oNeuPT = r.GetOrdinal("Neuer_PT_PPS"), oVerlade = r.GetOrdinal("Verladetermin_Vsl"),
            oMStd = r.GetOrdinal("Montagestunden"), oPrio = r.GetOrdinal("Prio");

        while (await r.ReadAsync(ct))
        {
            result.Add(new FaHierarchyOrderInfo
            {
                HauptFA = Int32(r, oHauptFA),
                MontageAbteilung = Str(r, oMontage),
                ABNr = Str(r, oABNr),
                Pos = Str(r, oPos),
                Kunde = Str(r, oKunde),
                Status = Str(r, oStatus),
                Dienstleister = Str(r, oDienst),
                RAL = Str(r, oRAL),
                Bemerkung_Uhrzeit = Str(r, oBemUhr),
                KO_Termin = DateN(r, oKO),
                FE_Termin = DateN(r, oFE),
                Start_Beschichtung = DateN(r, oStartB),
                Beschichten_Retour = DateN(r, oRetour),
                Neuer_PT_PPS = DateN(r, oNeuPT),
                Verladetermin_Vsl = DateN(r, oVerlade),
                Montagestunden = DecN(r, oMStd),
                Prio = IntN(r, oPrio),
                SyncedAt = syncedAt,
            });
        }
        return result;
    }

    private static string? Str(SqlDataReader r, int o) => r.IsDBNull(o) ? null : Convert.ToString(r.GetValue(o));
    private static int Int32(SqlDataReader r, int o) => r.IsDBNull(o) ? 0 : Convert.ToInt32(r.GetValue(o));
    private static int? IntN(SqlDataReader r, int o) => r.IsDBNull(o) ? (int?)null : Convert.ToInt32(r.GetValue(o));
    private static decimal Dec(SqlDataReader r, int o) => r.IsDBNull(o) ? 0m : Convert.ToDecimal(r.GetValue(o));
    private static decimal? DecN(SqlDataReader r, int o) => r.IsDBNull(o) ? (decimal?)null : Convert.ToDecimal(r.GetValue(o));
    private static DateTime? DateN(SqlDataReader r, int o) => r.IsDBNull(o) ? (DateTime?)null : Convert.ToDateTime(r.GetValue(o));

    /// <summary>Sage-VB6-Boolean-Mapping: 'Ja'/'-1'/'1'/true =&gt; true; 'Nein'/'0'/NULL =&gt; false.</summary>
    private static bool Bool(SqlDataReader r, int o)
    {
        if (r.IsDBNull(o)) return false;
        var v = r.GetValue(o);
        if (v is bool b) return b;
        if (v is string s)
        {
            s = s.Trim();
            return s.Equals("Ja", StringComparison.OrdinalIgnoreCase)
                || s.Equals("true", StringComparison.OrdinalIgnoreCase)
                || s == "-1" || s == "1";
        }
        try { return Convert.ToDecimal(v) != 0m; } // -1 (VB6) und 1 (bit) => true, 0 => false
        catch { return false; }
    }
}
