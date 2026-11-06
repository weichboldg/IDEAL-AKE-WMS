using System.Globalization;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;

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
/// TODO(P-F2): Deshalb liefert der Reader <b>keine</b> FAInfos (<see cref="FaStrukturLesung.InfosAnzahl"/> = 0 → der Abgleich wuerde
/// ueberspringen) und die Weiche sperrt <c>SData</c>. Vor dem Entsperren: FAInfos weiter per SQL lesen und die Knotenfelder belegen.
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

        var positionen = new List<SdataFaPositionZeile>();
        var ids = menge.Where(k => k.BelID is not null).Select(k => k.BelID!.Value).ToList();
        var bloecke = SageSdataQuellen.OrBloecke("BelID", ids); // "or" ist am AKE-Test belegt, "in" nicht
        foreach (var block in bloecke)
            positionen.AddRange((await _client.QueryAllAsync<SdataFaPositionZeile>(ziel, SageSdataReadClient.VertragResources,
                RessourcePositionen, block, SelectPositionen, "BelPosID", ct)).Zeilen);
        hinweise.Add(new($"Quelle SData: {RessourcePositionen}, {bloecke.Count} Block/Bloecke, {positionen.Count} Zeile(n)"));

        var plaetze = new List<SdataLagerplatzZeile>();
        if (positionen.Any(p => p.PlatzID > 0))
        {
            var gelesen = await _client.QueryAllAsync<SdataLagerplatzZeile>(ziel, SageSdataReadClient.VertragResources,
                SdataLagerplatzReader.RessourceLagerplaetze, null, SdataArtikelReader.SelectPlaetze, "PlatzID", ct);
            hinweise.Add(new(gelesen.Zusammenfassung));
            plaetze.AddRange(gelesen.Zeilen);
        }

        var jetzt = DateTime.Now;
        var knoten = Projizieren(menge, positionen, jetzt, hinweise);
        var faPositionen = PositionenProjizieren(menge, positionen, plaetze, jetzt, hinweise);
        _logger.LogInformation("Sage (SData) liefert {Knoten} FA-Struktur-Knoten und {Positionen} Positionen aus {Koepfe} FAs.",
            knoten.Count, faPositionen.Count, menge.Count);
        // TODO(P-F2): FAInfos bleiben SQL (G11) – InfosAnzahl 0 laesst den Abgleich im (gesperrten) Modus SData ueberspringen statt leeren.
        return new FaStrukturLesung(knoten, new(), knoten.Count, 0, faPositionen);
    }

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
        DateTime syncedAt, List<SdataHinweis>? hinweise = null)
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
        return result;
    }
}
