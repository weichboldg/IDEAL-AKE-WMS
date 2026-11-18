using System.Globalization;
using System.Text.Json;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IDEALAKEWMSService.Common;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>PpsFaBelegeInfo</c> (Namen exakt wie in der SData-Antwort). <c>IstEingelastet</c>/<c>Aktiv</c>: Sage-Boolean
/// (-1/0, <see cref="SageBoolJsonConverter"/>); null = Feld fehlt in der Antwort, der E4b-Filter wirft dann.</summary>
public sealed record SdataFaKopfZeile(int? BelID, int? StrukturID, string? Artikelnummer, bool? IstEingelastet, bool? Aktiv);

/// <summary>
/// Zeile aus <c>PpsFaBelegePositionenInfo</c> (Namen exakt wie in der SData-Antwort). Die Felder ab <c>ArbeitsgangNummer</c> stehen nur
/// als Beschreibung („Menge/Soll/Ist, Rüst-/Stückzeit Soll/Ist“) im P-S1-Auszug, ihre Namen sind KHK-Annahmen und deshalb NICHT in
/// <see cref="SdataFaStrukturReader.SelectPositionen"/> (ein falscher Name im select = HTTP 500) — sie bleiben bis P-S1 null.
/// <c>Bedarfstermin</c> als Text (Datumsformat dieses Vertrags unbelegt), geparst in <see cref="SdataFaStrukturReader.PositionenProjizieren"/>.
/// </summary>
public sealed record SdataFaPositionZeile(int? BelPosID, int? BelID, int? Position, int? BelIDNachfolger, string? RessourceTyp,
    string? RessourceNummer, decimal? Menge, int? PlatzID = null, int? Status = null, decimal? Schwund = null, string? Bedarfstermin = null,
    string? ArbeitsgangNummer = null, decimal? Sollmenge = null, decimal? Istmenge = null, decimal? SollRuestzeit = null,
    decimal? IstRuestzeit = null, decimal? SollStueckzeit = null, decimal? IstStueckzeit = null);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E4a/F6, G11): FA-Struktur-<b>Kanten</b> aus SData (<c>PpsFaBelegeInfo</c> +
/// <c>PpsFaBelegePositionenInfo</c>, Vertrag CommonWawiServicesResources), projiziert auf <see cref="FaHierarchyNode"/> nach der Regel der
/// View <c>vw_IDEAL-AKE_Kommissionierung_FAListe</c> (FAListe.sql:21-53): je Struktur eine Wurzel (<c>VaterFA</c>/<c>Position</c> NULL,
/// <c>SubFA = HauptFA</c>) plus eine Zeile je MA-Position jeder FA der Struktur (<c>VaterFA</c> = BelID, <c>SubFA</c> = BelIDNachfolger,
/// 0 = Blatt). E4b: Kopfmenge nur eingelastete + aktive FAs (<see cref="KoepfeFiltern"/>); dazu alle Positionen dieser FAs als
/// <see cref="ProductionOrderPosition"/> (<see cref="FaStrukturLesung.Positionen"/>, Lagerplatz ueber <c>LagerplaetzeInfo</c>). Belegt werden nur <c>HauptFA</c>, <c>VaterFA</c>, <c>SubFA</c>, <c>Position</c>, <c>HauptArtnr</c>, <c>Artnr</c>,
/// <c>Sollmenge</c>/<c>Fertigungmenge</c>; FAInfos (PPSImport) und die uebrigen Knotenfelder liefert SData nicht (G11).
/// <para>
/// Spec 2026-11-16-fa-struktur-ideal-ueber-sdata: FAInfos liefert der Reader nie (<see cref="FaStrukturLesung.InfosAnzahl"/> = 0); im
/// Modus SData liest die Weiche sie hybrid aus der View (E5). Beendete Haupt-FAs (E3) liest der Reader ueber <c>dtsFaBelegsuche</c> nach,
/// nur wenn <see cref="RealTimeDataErlaubt"/>.
/// </para>
/// Wirft bei jedem Lesefehler (<see cref="SdataLeseException"/>), nie leere Liste statt Fehler.
/// </summary>
public sealed class SdataFaStrukturReader : ISageFaStrukturReader
{
    public const string RessourceKoepfe = "PpsFaBelegeInfo";
    public const string RessourcePositionen = "PpsFaBelegePositionenInfo";
    public const string SelectKoepfe = "BelID,StrukturID,Artikelnummer,IstEingelastet,Aktiv";
    // TODO(P-S1): PlatzID/Status/Schwund/Bedarfstermin stehen als Namen im Schema-Auszug (sage-sdata-commonwawiservicesresources), Typen
    // unbelegt. Nach P-S1 ergaenzen: Soll-/Istmenge, Soll-/Ist-Ruest- und Stueckzeit, Arbeitsgangnummer (Namen siehe SdataFaPositionZeile).
    public const string SelectPositionen = "BelPosID,BelID,Position,BelIDNachfolger,RessourceTyp,RessourceNummer,Menge,PlatzID,Status,Schwund,Bedarfstermin";
    /// <summary>G8: offene FAs + beendete der letzten so vielen Tage (Vollabgleich der Menge je Lauf, kein Delta).</summary>
    public const int BeendetFensterTage = 30;

    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SdataFaStrukturReader> _logger;

    public SdataFaStrukturReader(ISageSdataReadClient client, IConfiguration configuration, ILogger<SdataFaStrukturReader> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Hinweise des letzten Aufrufs (Ressourcen/Zeilen/Dauer, Strukturen ohne Haupt-FA).</summary>
    public IReadOnlyList<SdataHinweis> Hinweise { get; private set; } = Array.Empty<SdataHinweis>();

    /// <summary>
    /// Spec 2026-11-16-fa-struktur-ideal-ueber-sdata, Freigabe-Antwort 1: RealTimeData (<c>dtsFaBelegsuche</c>) ist fuer den Betrieb
    /// (noch) nicht von DPS freigegeben. Der Dienst liest es deshalb NUR lesend und NUR im Modus Vergleich - die Weiche setzt den Schalter
    /// je Aufruf (<see cref="FaStrukturQuelleSwitch"/>). Default false: ohne Weiche nie RealTimeData.
    /// </summary>
    public bool RealTimeDataErlaubt { get; set; }

    /// <summary>
    /// TODO(P-S1/G8): <c>IstBeendet</c>/<c>Istendtermin</c> stehen in keinem $schema-Auszug von <c>PpsFaBelegeInfo</c> (Namen aus
    /// <c>dtsFaBelegsuche</c>, das dieselbe Tabelle KHKPpsFaBelege liest). SData setzt <c>where</c> direkt in SQL um: ein falscher Name
    /// kommt als HTTP 500 zurueck (Vergleichslauf mit Fehler + Mail), nie als stilles Leer. <c>IstEingelastet</c>/<c>Aktiv</c> (E4b) filtert
    /// <see cref="KoepfeFiltern"/> im Code — die Schreibweise eines Bool-Filters im where ist am Server nicht belegt (Muster E1).
    /// </summary>
    public static string WhereKoepfe(DateTime heute) =>
        $"IstBeendet eq 0 or Istendtermin ge @{heute.Date.AddDays(-BeendetFensterTage).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}@";

    public async Task<FaStrukturLesung> LesenAsync(CancellationToken ct = default) =>
        await LesenAsync(await SdataLeseZielLeser.LesenAsync(_configuration, ct), DateTime.Today, ct);

    public async Task<FaStrukturLesung> LesenAsync(SdataLeseZiel ziel, DateTime heute, CancellationToken ct)
    {
        var hinweise = new List<SdataHinweis>();
        Hinweise = hinweise;

        var koepfe = await _client.QueryAllAsync<SdataFaKopfZeile>(ziel, SageSdataReadClient.VertragResources, RessourceKoepfe,
            WhereKoepfe(heute), SelectKoepfe, "BelID", ct);
        hinweise.Add(new(koepfe.Zusammenfassung));
        var menge = KoepfeFiltern(koepfe.Zeilen, hinweise);

        // E3 (F3): Haupt-FAs, die PpsFaBelegeInfo nicht liefert (beendet, 1059463), ueber dtsFaBelegsuche nachlesen - nur fuer die
        // Struktur (Wurzel + Wurzel-Positionen), nicht fuer ProductionOrderPositions (Schreibverhalten unveraendert).
        // E4: im selben Aufruf die USER_OS*-Felder aller FAs der Struktur (FA-Knoten).
        // v1.99.40 (ADR 0033): Sage-Namen der USER_OS*-Felder aus Sync:Userfeld:FaStruktur:*; intern gilt der logische Name (= Default).
        var userfelder = await ServiceSettings.UserfelderSafeAsync(_configuration, ct);
        foreach (var h in userfelder.Hinweise) hinweise.Add(new(h, Warnung: true));
        var nichtKonfiguriert = FelderFa.Where(f => userfelder.Feld(SageUserfelder.FaStruktur, f) is null).ToList();
        if (nichtKonfiguriert.Count > 0)
            hinweise.Add(new($"FA-Struktur: {nichtKonfiguriert.Count} Sage-Benutzerfeld(er) nicht konfiguriert – nicht gelesen, bleiben leer: "
                + string.Join(", ", nichtKonfiguriert) + "."));
        var (wurzeln, faFelder) = await FaKoepfeLesenAsync(ziel, menge, hinweise, ct, userfelder);
        var struktur = menge.Concat(wurzeln).ToList();

        var positionen = new List<SdataFaPositionZeile>();
        var ids = struktur.Where(k => k.BelID is not null).Select(k => k.BelID!.Value).ToList();
        var bloecke = SageSdataQuellen.OrBloecke("BelID", ids); // "or" ist am AKE-Test belegt, "in" nicht
        foreach (var block in bloecke)
            positionen.AddRange((await _client.QueryAllAsync<SdataFaPositionZeile>(ziel, SageSdataReadClient.VertragResources,
                RessourcePositionen, block, SelectPositionen, "BelPosID", ct)).Zeilen);
        hinweise.Add(new($"Quelle SData: {RessourcePositionen}, {bloecke.Count} Block/Bloecke, {positionen.Count} Zeile(n)"));

        // E4: Artikelfelder (Matchcode, Bezeichnungen, Gruppe, Bestellartikel, Hauptlagerplatz, Blatt-USER_OS*) je Artnr der Knoten.
        var artnr = positionen.Where(p => string.Equals(p.RessourceTyp?.Trim(), "MA", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.RessourceNummer).Concat(struktur.Select(k => k.Artikelnummer))
            .Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => SdataArtikelReader.Schluessel(n))
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
        var userArtikel = userfelder.Konfiguriert(SageUserfelder.FaStruktur, FelderArtikel.Where(f => f.StartsWith("USER_", StringComparison.Ordinal)));
        var artikelGelesen = await SdataArtikelReader.BenutzerfelderLesenAsync(_client, ziel,
            FelderArtikel.Where(f => !f.StartsWith("USER_", StringComparison.Ordinal)).Concat(userArtikel.Select(u => u.Feld)).ToList(), artnr, hinweise, ct,
            "Feldzuordnung FA-Struktur prüfen (Sage-Benutzerfelder: FA-Struktur in den Service-Einstellungen; Spec 2026-11-16-fa-struktur-ideal-ueber-sdata, P-FS5).");
        // Sage-Name → logischer Name (FelderSetzen kennt nur die logischen)
        var artikelFelder = artikelGelesen.ToDictionary(kv => kv.Key, kv =>
        {
            var d = new Dictionary<string, string?>(kv.Value, StringComparer.OrdinalIgnoreCase);
            foreach (var (logisch, feld) in userArtikel)
                if (!string.Equals(logisch, feld, StringComparison.OrdinalIgnoreCase)) { d[logisch] = d.GetValueOrDefault(feld); d.Remove(feld); }
            return d;
        }, StringComparer.OrdinalIgnoreCase);

        var plaetze = new List<SdataLagerplatzZeile>();
        if (positionen.Any(p => p.PlatzID > 0) || artikelFelder.Values.Any(f => Zahl(f.GetValueOrDefault("PlatzID")) > 0))
        {
            var gelesen = await _client.QueryAllAsync<SdataLagerplatzZeile>(ziel, SageSdataReadClient.VertragResources,
                SdataLagerplatzReader.RessourceLagerplaetze, null, SdataArtikelReader.SelectPlaetze, "PlatzID", ct);
            hinweise.Add(new(gelesen.Zusammenfassung));
            plaetze.AddRange(gelesen.Zeilen);
        }

        var jetzt = DateTime.Now;
        var knoten = Projizieren(struktur, positionen, jetzt, hinweise, faFelder, artikelFelder, plaetze);
        var faPositionen = PositionenProjizieren(menge, positionen, plaetze, jetzt, hinweise);
        _logger.LogInformation("Sage (SData) liefert {Knoten} FA-Struktur-Knoten und {Positionen} Positionen aus {Koepfe} FAs.",
            knoten.Count, faPositionen.Count, menge.Count);
        // FAInfos bleiben SQL (G11): InfosAnzahl 0; im Modus SData ergaenzt die Weiche sie aus der View (E5).
        return new FaStrukturLesung(knoten, new(), knoten.Count, 0, faPositionen);
    }

    /// <summary>Strukturen der Menge ohne eigenen Kopf (BelID = StrukturID).</summary>
    public static List<int> FehlendeWurzeln(IReadOnlyList<SdataFaKopfZeile> menge)
    {
        var ids = menge.Where(k => k.BelID is not null).Select(k => k.BelID!.Value).ToHashSet();
        return menge.Where(k => k.StrukturID is not null).Select(k => k.StrukturID!.Value).Distinct().Where(s => !ids.Contains(s)).Order().ToList();
    }

    public const string SelectBelegsucheKopf = "BelID,StrukturID,Artikelnummer";

    /// <summary>E4, FA-Knoten (Wurzel, Unter-FA): <c>USER_OS*</c> am FA-Kopf (<c>dtsFaBelegsuche</c>, Namen belegt L10). Arbeitshypothese bis
    /// P-FS1 (Freigabe-Antwort 2), wird nur verglichen. Kein Filter auf USER-Felder (HTTP 500, L10).</summary>
    /// <summary>In <c>dtsFaBelegsuche</c> tragen die Kopf-Benutzerfelder das Tabellen-Praefix (L8, QA IDEAL 2026-11-16 R1). Ohne Praefix
    /// antwortet Sage 200 und laesst die Felder still weg.</summary>
    public const string PraefixBelegsuche = "KHKPpsFaBelege_";

    public static readonly string[] FelderFa =
    {
        "USER_OSBemerkungPN", "USER_OSFertigungsInfoPN", "USER_OSKommissionieren", "USER_OSAbteilung", "USER_OSArbeitsschritte",
        "USER_OSArtikeltyp", "USER_OSMaterial", "USER_OSVMBedarf", "USER_OSBeschichtet", "USER_OSBreite", "USER_OSHoehe", "USER_OSTiefe",
        "USER_OSEKBedarf",
    };

    /// <summary>E4, Artikel je Artnr: Stammfelder fuer alle Knoten + <c>USER_OS*</c> fuer Blaetter (Namen am Artikel belegt L10). Beschichtet,
    /// Breite, Hoehe, Tiefe, EKBedarf gibt es am Artikel nicht (Luecke, Rueckfrage 4) - sie bleiben an Blaettern leer und werden verglichen.</summary>
    public static readonly string[] FelderArtikel =
    {
        "Matchcode", "Bezeichnung1", "Bezeichnung2", "Artikelgruppe", "IstBestellartikel", "PlatzID",
        "USER_OSBemerkungPN", "USER_OSFertigungsInfoPN", "USER_OSKommissionieren", "USER_OSAbteilung", "USER_OSArbeitsschritte",
        "USER_OSArtikeltyp", "USER_OSMaterial", "USER_OSVMBedarf",
    };

    /// <summary>
    /// E3+E4: ein <c>dtsFaBelegsuche</c>-Lauf (RealTimeData, BelID-Bloecke, <c>orderBy=BelID</c>) ueber alle FAs der Menge und die fehlenden
    /// Haupt-FAs. Liefert die Koepfe fehlender Haupt-FAs (nur Zeilen mit BelID = StrukturID) und die <see cref="FelderFa"/> je BelID.
    /// Nur wenn <see cref="RealTimeDataErlaubt"/>; sonst ein Hinweis, keine Wurzel-Ergaenzung (Warnung "ohne Wurzel" bleibt) und leere
    /// FA-Felder. HTTP-/Lesefehler werfen (Vertrag des Readers).
    /// </summary>
    private async Task<(List<SdataFaKopfZeile> Wurzeln, Dictionary<int, Dictionary<string, string?>> Felder)> FaKoepfeLesenAsync(
        SdataLeseZiel ziel, IReadOnlyList<SdataFaKopfZeile> menge, List<SdataHinweis> hinweise, CancellationToken ct, SageUserfelder userfelder)
    {
        var fehlend = FehlendeWurzeln(menge);
        var ids = menge.Where(k => k.BelID is not null).Select(k => k.BelID!.Value).Concat(fehlend).Distinct().Order().ToList();
        if (ids.Count == 0) return (new(), new());
        if (!RealTimeDataErlaubt)
        {
            hinweise.Add(new($"RealTimeData ({SageSdataQuellen.FaBelegsuche}) liest der Dienst nur im Modus Vergleich (Freigabe-Antwort 1) – "
                + $"FA-Felder leer, {fehlend.Count} Haupt-FA(s) nicht in {RessourceKoepfe} nicht nachgelesen."));
            return (new(), new());
        }
        var fa = userfelder.Konfiguriert(SageUserfelder.FaStruktur, FelderFa);
        var zeilen = await BelegsucheAsync(ziel, ids, string.Join(",", fa.Select(k => PraefixBelegsuche + k.Feld).Prepend(SelectBelegsucheKopf)), hinweise, ct,
            userfelder);
        var felder = new Dictionary<int, Dictionary<string, string?>>();
        foreach (var z in zeilen)
            if (Zahl(z, "BelID") is { } belId)
                felder.TryAdd(belId, fa.ToDictionary(k => k.Logisch, k => Text(z, PraefixBelegsuche + k.Feld), StringComparer.OrdinalIgnoreCase));
        var wurzeln = zeilen.Select(z => new SdataFaKopfZeile(Zahl(z, "BelID"), Zahl(z, "StrukturID"), Text(z, "Artikelnummer"), null, null))
            .Where(k => k.BelID is not null && k.BelID == k.StrukturID && fehlend.Contains(k.BelID.Value))
            .DistinctBy(k => k.BelID).ToList();
        if (fehlend.Count > 0)
            hinweise.Add(new($"{wurzeln.Count} von {fehlend.Count} Haupt-FA(s) ohne Kopf in {RessourceKoepfe} über {SageSdataQuellen.FaBelegsuche} ergänzt"
                + (wurzeln.Count > 0 ? ": " + string.Join(", ", wurzeln.Take(20).Select(w => w.BelID)) : "") + "."));
        return (wurzeln, felder);
    }

    /// <summary>Zeilen aus <c>dtsFaBelegsuche</c> je BelID-Block (Felder ohne Gross/Klein). Wirft bei HTTP-/Verbindungsfehler.</summary>
    private async Task<List<Dictionary<string, JsonElement>>> BelegsucheAsync(SdataLeseZiel ziel, IReadOnlyList<int> ids, string select,
        List<SdataHinweis> hinweise, CancellationToken ct, SageUserfelder? userfelder = null)
    {
        var zeilen = new List<Dictionary<string, JsonElement>>();
        var bloecke = SageSdataQuellen.OrBloecke("BelID", ids);
        long ms = 0;
        foreach (var block in bloecke)
        {
            var r = await _client.GetAsync<Dictionary<string, JsonElement>>(ziel, SageSdataQuellen.FaBelegsuche, block, select, "BelID", ct);
            if (!r.Call.Erfolgreich)
                throw new SdataLeseException(SageSdataQuellen.FaBelegsuche, r.Call.Status,
                    userfelder?.Erklaeren(r.Call.Fehler ?? "unbekannter Fehler") ?? r.Call.Fehler ?? "unbekannter Fehler");
            zeilen.AddRange(r.Zeilen.Select(z => new Dictionary<string, JsonElement>(z, StringComparer.OrdinalIgnoreCase)));
            ms += r.Call.DauerMs;
        }
        hinweise.Add(new($"Quelle SData (RealTimeData): {SageSdataQuellen.FaBelegsuche}, {bloecke.Count} Block/Bloecke, {zeilen.Count} Zeile(n), {ms} ms"));
        return zeilen;
    }

    private static string? Text(IReadOnlyDictionary<string, JsonElement> z, string feld) =>
        z.TryGetValue(feld, out var w) ? SdataArtikelReader.AlsText(w) : null;

    private static int? Zahl(IReadOnlyDictionary<string, JsonElement> z, string feld) => Zahl(Text(z, feld));

    private static int? Zahl(string? text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;

    /// <summary>
    /// E4: Feldzuordnung an EINER Stelle (Spec-Tabelle „Knotenfelder“, Arbeitshypothese bis P-FS1/P-FS5 - korrigiert wird nur hier).
    /// FA-Knoten (<c>SubFA != 0</c>, auch die Wurzel) nehmen die <c>USER_OS*</c> ihres FA-Kopfs, Blaetter die des Artikels; Stammfelder
    /// kommen fuer alle Knoten vom Artikel der <c>Artnr</c>. Bool wie der View-Weg (<see cref="SageFaStrukturReader.SageBool"/>), Dezimal
    /// NULL → 0.
    /// </summary>
    internal static void FelderSetzen(FaHierarchyNode n, IReadOnlyDictionary<string, string?>? fa, IReadOnlyDictionary<string, string?>? artikel,
        IReadOnlyDictionary<int, string?> platzJe)
    {
        var os = n.SubFA != 0 ? fa : artikel;
        string? F(IReadOnlyDictionary<string, string?>? d, string k) => d?.GetValueOrDefault(k);
        n.Matchcode = F(artikel, "Matchcode");
        n.Bezeichnung1 = F(artikel, "Bezeichnung1"); // TODO(P-FS5): View nimmt fuer Unter-FAs den Beleg bevorzugt - Feldnamen am FA-Kopf ungeprueft
        n.Bezeichnung2 = F(artikel, "Bezeichnung2");
        n.Artikelgruppe = F(artikel, "Artikelgruppe"); // TODO(P-FS5): View liefert "CODE - Bezeichnung", hier nur der Code
        n.Beschaffungsartikel = SageFaStrukturReader.SageBool(Wert(F(artikel, "IstBestellartikel"))); // offen: Rueckfrage 3
        n.Hauptlagerplatz = Zahl(F(artikel, "PlatzID")) is int platz && platz > 0 ? platzJe.GetValueOrDefault(platz) : null;
        n.BemerkungPN = F(os, "USER_OSBemerkungPN");
        n.FertigungsInfoPN = F(os, "USER_OSFertigungsInfoPN");
        n.Kommissionieren = F(os, "USER_OSKommissionieren");
        n.Arbeitsbereich = F(os, "USER_OSAbteilung"); // offen: Rueckfrage 3, Name allein ist kein Beleg
        n.Arbeitsschritte = F(os, "USER_OSArbeitsschritte");
        n.Artikeltyp = F(artikel, "USER_OSArtikeltyp"); // R4 (ADR 0032): fuer alle Knoten vom Artikel, am FA-Kopf leer
        n.Material = F(os, "USER_OSMaterial");
        n.VMBedarf = F(os, "USER_OSVMBedarf");
        n.Beschichtet = SageFaStrukturReader.SageBool(Wert(F(os, "USER_OSBeschichtet"))); // Blatt: keine Quelle (Rueckfrage 4)
        n.Breite = Dezimal(F(os, "USER_OSBreite"));
        n.Hoehe = Dezimal(F(os, "USER_OSHoehe"));
        n.Tiefe = Dezimal(F(os, "USER_OSTiefe"));
        n.EKBedarf = SageFaStrukturReader.SageBool(Wert(F(os, "USER_OSEKBedarf")));
    }

    /// <summary>Text als Zahl, wenn er eine ist (so greift die Zahl-Regel des View-Wegs: -1/1 → true), sonst als Text ("Ja").</summary>
    private static object? Wert(string? text) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : text;

    private static decimal Dezimal(string? text) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    /// <summary>
    /// E4b-Lesefilter (F6, R7): nur FAs mit <c>IstEingelastet</c> und <c>Aktiv</c> (Sage -1). Nicht eingelastete/inaktive (z. B.
    /// Fertigungsart Einkauf) fehlen danach in der Menge und werden wie verschwunden behandelt (Materialisierung: <c>SageMissingSince</c>).
    /// Fehlt eines der Felder in einer Zeile, wirft der Filter (Feldname/Schema passt nicht) statt still alles auszufiltern.
    /// </summary>
    public static List<SdataFaKopfZeile> KoepfeFiltern(IReadOnlyList<SdataFaKopfZeile> koepfe, List<SdataHinweis>? hinweise = null)
    {
        var kaputt = koepfe.Count(k => k.IstEingelastet is null || k.Aktiv is null);
        if (kaputt > 0)
            throw new SdataLeseException(RessourceKoepfe, 0, $"{kaputt} von {koepfe.Count} Zeilen ohne IstEingelastet/Aktiv – Feldnamen prüfen (P-S1).");
        var menge = koepfe.Where(k => k.IstEingelastet == true && k.Aktiv == true).ToList();
        var raus = koepfe.Where(k => k.IstEingelastet != true || k.Aktiv != true).Select(k => k.BelID).ToList();
        if (raus.Count > 0)
            hinweise?.Add(new($"{raus.Count} FA(s) nicht eingelastet oder inaktiv – nicht in der SData-Menge: "
                + string.Join(", ", raus.Take(20)) + (raus.Count > 20 ? " …" : "")));
        return menge;
    }

    /// <summary>
    /// E4b: je FA der Menge alle Positionen (jeder Ressourcetyp) als <see cref="ProductionOrderPosition"/>; <c>StrukturID</c> aus dem Kopf,
    /// Lagerplatz-Kurzbezeichnung ueber die PlatzID. Positionen fremder FAs fallen weg. Fehlende <c>BelPosID</c> wirft (Pflichtfeld,
    /// UNIQUE-Schluessel); eine doppelte BelPosID (Paging-Verschiebung) wird einmal uebernommen und gemeldet, statt den Full-Refresh am
    /// UNIQUE-Index scheitern zu lassen. Unbekannte PlatzID und unlesbarer Bedarfstermin werden gemeldet, nicht still geschluckt.
    /// </summary>
    public static List<ProductionOrderPosition> PositionenProjizieren(IReadOnlyList<SdataFaKopfZeile> koepfe,
        IReadOnlyList<SdataFaPositionZeile> positionen, IReadOnlyList<SdataLagerplatzZeile> plaetze, DateTime syncedAt,
        List<SdataHinweis>? hinweise = null)
    {
        var strukturJe = koepfe.Where(k => k.BelID is not null && k.StrukturID is not null)
            .GroupBy(k => k.BelID!.Value).ToDictionary(g => g.Key, g => g.First().StrukturID!.Value);
        var platzJe = plaetze.Where(p => p.PlatzID is not null).GroupBy(p => p.PlatzID!.Value)
            .ToDictionary(g => g.Key, g => g.First().Kurzbezeichnung?.Trim());

        var result = new List<ProductionOrderPosition>();
        var gesehen = new HashSet<int>();
        var doppelt = new List<int>();
        var platzFehlt = new SortedSet<int>();
        var terminKaputt = 0;
        foreach (var p in positionen.OrderBy(p => p.BelID).ThenBy(p => p.Position))
        {
            if (p.BelID is not { } belId || !strukturJe.TryGetValue(belId, out var strukturId)) continue;
            if (p.BelPosID is not { } belPosId)
                throw new SdataLeseException(RessourcePositionen, 0, $"Position ohne BelPosID (FA {belId}) – Feldnamen prüfen (P-S1).");
            if (!gesehen.Add(belPosId)) { doppelt.Add(belPosId); continue; }

            string? kurz = null;
            if (p.PlatzID is { } platzId && platzId > 0 && !platzJe.TryGetValue(platzId, out kurz)) platzFehlt.Add(platzId);
            DateTime? termin = null;
            if (!string.IsNullOrWhiteSpace(p.Bedarfstermin))
            {
                if (DateTimeOffset.TryParse(p.Bedarfstermin, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var t)) termin = t.DateTime;
                else terminKaputt++;
            }

            result.Add(new ProductionOrderPosition
            {
                BelPosID = belPosId, BelID = belId, StrukturID = strukturId, Position = p.Position,
                RessourceTyp = p.RessourceTyp?.Trim(), RessourceNummer = p.RessourceNummer?.Trim(), ArbeitsgangNummer = p.ArbeitsgangNummer?.Trim(),
                BelIDNachfolger = p.BelIDNachfolger, Menge = p.Menge, Sollmenge = p.Sollmenge, Istmenge = p.Istmenge,
                SollRuestzeit = p.SollRuestzeit, IstRuestzeit = p.IstRuestzeit, SollStueckzeit = p.SollStueckzeit, IstStueckzeit = p.IstStueckzeit,
                Status = p.Status, PlatzID = p.PlatzID, LagerplatzKurzbezeichnung = string.IsNullOrEmpty(kurz) ? null : kurz,
                Schwund = p.Schwund, Bedarfstermin = termin, SyncedAt = syncedAt,
            });
        }

        if (doppelt.Count > 0)
            hinweise?.Add(new($"{doppelt.Count} doppelte BelPosID in {RessourcePositionen} (einmal übernommen): "
                + string.Join(", ", doppelt.Take(20)), Warnung: true));
        if (platzFehlt.Count > 0)
            hinweise?.Add(new($"{platzFehlt.Count} PlatzID(s) der Positionen nicht in {SdataLagerplatzReader.RessourceLagerplaetze} (Kurzbezeichnung leer): "
                + string.Join(", ", platzFehlt.Take(20)), Warnung: true));
        if (terminKaputt > 0)
            hinweise?.Add(new($"{terminKaputt} Bedarfstermin(e) nicht lesbar (leer übernommen) – Datumsformat prüfen (P-S1).", Warnung: true));
        return result;
    }

    /// <summary>
    /// Regel F6 (FAListe.sql:21-53): Wurzel je Struktur (Kopf mit BelID = StrukturID) + je MA-Position einer FA der Menge ein Knoten.
    /// Positionen anderer Ressourcetypen (AG …) und FAs ausserhalb der Kopfmenge fallen weg. Eine Struktur ohne Haupt-FA in der Menge
    /// (z. B. Haupt-FA laenger als <see cref="BeendetFensterTage"/> beendet) bekommt keine Wurzel und wird als Hinweis gemeldet.
    /// </summary>
    public static List<FaHierarchyNode> Projizieren(IReadOnlyList<SdataFaKopfZeile> koepfe, IReadOnlyList<SdataFaPositionZeile> positionen,
        DateTime syncedAt, List<SdataHinweis>? hinweise = null, IReadOnlyDictionary<int, Dictionary<string, string?>>? faFelder = null,
        IReadOnlyDictionary<string, Dictionary<string, string?>>? artikelFelder = null, IReadOnlyList<SdataLagerplatzZeile>? plaetze = null)
    {
        if (koepfe.Any(k => k.BelID is null || k.StrukturID is null))
            throw new SdataLeseException(RessourceKoepfe, 0, "Zeile ohne BelID/StrukturID – Feldnamen prüfen (P-S1).");
        if (positionen.Any(p => p.BelID is null))
            throw new SdataLeseException(RessourcePositionen, 0, "Zeile ohne BelID – Feldnamen prüfen (P-S1).");

        var kopfJeId = koepfe.GroupBy(k => k.BelID!.Value).ToDictionary(g => g.Key, g => g.First());
        var result = new List<FaHierarchyNode>();
        var ohneWurzel = new List<int>();

        foreach (var struktur in koepfe.GroupBy(k => k.StrukturID!.Value).OrderBy(g => g.Key))
        {
            var hauptFa = struktur.Key;
            var hauptArtnr = kopfJeId.GetValueOrDefault(hauptFa)?.Artikelnummer;
            if (kopfJeId.ContainsKey(hauptFa))
                result.Add(new FaHierarchyNode
                {
                    HauptFA = hauptFa, VaterFA = null, Position = null, SubFA = hauptFa,
                    HauptArtnr = hauptArtnr, Artnr = hauptArtnr, SyncedAt = syncedAt,
                });
            else
                ohneWurzel.Add(hauptFa);
        }

        foreach (var p in positionen.OrderBy(p => p.BelID).ThenBy(p => p.Position))
        {
            if (!string.Equals(p.RessourceTyp?.Trim(), "MA", StringComparison.OrdinalIgnoreCase)) continue;
            if (!kopfJeId.TryGetValue(p.BelID!.Value, out var kopf)) continue;
            var hauptFa = kopf.StrukturID!.Value;
            result.Add(new FaHierarchyNode
            {
                HauptFA = hauptFa,
                VaterFA = p.BelID,
                Position = p.Position,
                SubFA = p.BelIDNachfolger ?? 0,
                HauptArtnr = kopfJeId.GetValueOrDefault(hauptFa)?.Artikelnummer,
                Artnr = p.RessourceNummer,
                Sollmenge = p.Menge ?? 0m,
                Fertigungmenge = p.Menge ?? 0m,
                SyncedAt = syncedAt,
            });
        }

        if (ohneWurzel.Count > 0)
            hinweise?.Add(new($"{ohneWurzel.Count} Struktur(en) ohne Haupt-FA in der SData-Menge (keine Wurzel): "
                + string.Join(", ", ohneWurzel.Take(20)) + (ohneWurzel.Count > 20 ? " …" : ""), Warnung: true));

        // E4: Knotenfelder (FelderSetzen). Ohne gelesene Felder (alte Aufrufer, Tests der Kanten) bleiben sie leer wie bisher.
        if (faFelder is null && artikelFelder is null) return result;
        var platzJe = (plaetze ?? Array.Empty<SdataLagerplatzZeile>()).Where(p => p.PlatzID is not null).GroupBy(p => p.PlatzID!.Value)
            .ToDictionary(g => g.Key, g => g.First().Kurzbezeichnung);
        foreach (var n in result)
            FelderSetzen(n, n.SubFA != 0 ? faFelder?.GetValueOrDefault(n.SubFA) : null,
                artikelFelder?.GetValueOrDefault(SdataArtikelReader.Schluessel(n.Artnr)), platzJe);
        return result;
    }
}
