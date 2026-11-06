using System.Globalization;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>PpsFaBelegeInfo</c> (Namen exakt wie in der SData-Antwort).</summary>
public sealed record SdataFaKopfZeile(int? BelID, int? StrukturID, string? Artikelnummer);

/// <summary>Zeile aus <c>PpsFaBelegePositionenInfo</c> (Namen exakt wie in der SData-Antwort).</summary>
public sealed record SdataFaPositionZeile(int? BelPosID, int? BelID, int? Position, int? BelIDNachfolger, string? RessourceTyp,
    string? RessourceNummer, decimal? Menge);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E4a/F6, G11): FA-Struktur-<b>Kanten</b> aus SData (<c>PpsFaBelegeInfo</c> +
/// <c>PpsFaBelegePositionenInfo</c>, Vertrag CommonWawiServicesResources), projiziert auf <see cref="FaHierarchyNode"/> nach der Regel der
/// View <c>vw_IDEAL-AKE_Kommissionierung_FAListe</c> (FAListe.sql:21-53): je Struktur eine Wurzel (<c>VaterFA</c>/<c>Position</c> NULL,
/// <c>SubFA = HauptFA</c>) plus eine Zeile je MA-Position jeder FA der Struktur (<c>VaterFA</c> = BelID, <c>SubFA</c> = BelIDNachfolger,
/// 0 = Blatt). Belegt werden nur <c>HauptFA</c>, <c>VaterFA</c>, <c>SubFA</c>, <c>Position</c>, <c>HauptArtnr</c>, <c>Artnr</c>,
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
    public const string SelectKoepfe = "BelID,StrukturID,Artikelnummer";
    public const string SelectPositionen = "BelPosID,BelID,Position,BelIDNachfolger,RessourceTyp,RessourceNummer,Menge";
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
    /// kommt als HTTP 500 zurueck (Vergleichslauf mit Fehler + Mail), nie als stilles Leer. E4b ergaenzt <c>IstEingelastet</c>/<c>Aktiv</c>.
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

        var positionen = new List<SdataFaPositionZeile>();
        var ids = koepfe.Zeilen.Where(k => k.BelID is not null).Select(k => k.BelID!.Value).ToList();
        var bloecke = SageSdataQuellen.OrBloecke("BelID", ids); // "or" ist am AKE-Test belegt, "in" nicht
        foreach (var block in bloecke)
            positionen.AddRange((await _client.QueryAllAsync<SdataFaPositionZeile>(ziel, SageSdataReadClient.VertragResources,
                RessourcePositionen, block, SelectPositionen, "BelPosID", ct)).Zeilen);
        hinweise.Add(new($"Quelle SData: {RessourcePositionen}, {bloecke.Count} Block/Bloecke, {positionen.Count} Zeile(n)"));

        var knoten = Projizieren(koepfe.Zeilen, positionen, DateTime.Now, hinweise);
        _logger.LogInformation("Sage (SData) liefert {Knoten} FA-Struktur-Knoten aus {Koepfe} FAs.", knoten.Count, koepfe.Zeilen.Count);
        // TODO(P-F2): FAInfos bleiben SQL (G11) – InfosAnzahl 0 laesst den Abgleich im (gesperrten) Modus SData ueberspringen statt leeren.
        return new FaStrukturLesung(knoten, new(), knoten.Count, 0);
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
