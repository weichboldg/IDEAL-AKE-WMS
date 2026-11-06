using System.Text.Json;
using IdealAkeWms.Services.SageSdata;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>LagerbestaendeArtikelInfo</c> (Namen wie die Spalten von <c>KHKLagerplatzbestaende</c>).</summary>
public sealed record SdataBestandZeile(string? Artikelnummer, int? PlatzID, decimal? Bestand);

/// <summary>Protokoll-Hinweis des letzten SData-Lesens (Info-Zeile im Lagerbestand-Lauf; <paramref name="Warnung"/> = als Warnung).</summary>
public sealed record SdataHinweis(string Text, bool Warnung = false);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E2/F4): Lagerbestaende aus SData (<c>LagerbestaendeArtikelInfo</c>, Vertrag
/// CommonWawiServicesResources). Liefert dieselben Tupel wie <see cref="SageBestandReader"/>: Artikelnummer, Kurzbezeichnung des Platzes
/// (ueber <c>LagerplaetzeInfo</c> je PlatzID, wie der SQL-LEFT-JOIN auf KHKLagerplaetze), Summe Bestand je (Artikel, Platz, Lagerkennung);
/// Zeilen ohne Bestand fallen weg (SQL <c>LB.Bestand IS NOT NULL</c>). Wirft bei jedem Lesefehler und bei unlesbaren Zeilen —
/// eine leere oder falsch gemappte Liste hiesse im Abgleich „alles verschwunden“ (Nullsetzen).
/// Chargen/Seriennummern werden nur gezaehlt (<see cref="Hinweise"/>), nicht gespeichert.
/// </summary>
public sealed class SdataBestandReader : ISageBestandReader
{
    public const string RessourceBestand = "LagerbestaendeArtikelInfo";
    public const string RessourceChargen = "LagerbestaendeArtikelChargenInfo";
    public const string RessourceSeriennummern = "LagerbestaendeArtikelSeriennummernInfo";
    public const string SelectPlaetze = "PlatzID,Lagerkennung,Kurzbezeichnung";
    // Mandant wie SQL (SageBestandReader: A.Mandant = 1); Ableitung aus dem Dataset erst nach P-S2.
    public const string WhereBestand = "Mandant eq 1";
    // TODO(P-S1): LagerbestaendeArtikelInfo steht nicht im P-S1-Schema. Feldnamen = KHKLagerplatzbestaende-Spalten (Artikelnummer,
    // PlatzID, Bestand, Mandant) angenommen. Deshalb ohne select (ein falscher Name im select = HTTP 500); orderBy/where brauchen die
    // Namen trotzdem — falsch = HTTP 500 mit SQL in der Meldung, nie stilles Leer. Nach P-S1 select = "Artikelnummer,PlatzID,Bestand".
    public const string OrderByBestand = "Artikelnummer,PlatzID";
    public const string OrderByZaehlen = "Artikelnummer";

    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SdataBestandReader> _logger;

    public SdataBestandReader(ISageSdataReadClient client, IConfiguration configuration, ILogger<SdataBestandReader> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Hinweise des letzten Aufrufs (Ressourcen/Zeilen/Dauer, Anzahl Chargen/Seriennummern).</summary>
    public IReadOnlyList<SdataHinweis> Hinweise { get; private set; } = Array.Empty<SdataHinweis>();

    public async Task<List<SageBestandDto>> GetAllAsync(CancellationToken ct = default) =>
        await LesenAsync(await SdataLeseZielLeser.LesenAsync(_configuration, ct), ct);

    public async Task<List<SageBestandDto>> LesenAsync(SdataLeseZiel ziel, CancellationToken ct)
    {
        var hinweise = new List<SdataHinweis>();
        Hinweise = hinweise;
        var bestand = await _client.QueryAllAsync<SdataBestandZeile>(ziel, SageSdataReadClient.VertragResources, RessourceBestand,
            WhereBestand, null, OrderByBestand, ct);
        var plaetze = await _client.QueryAllAsync<SdataLagerplatzZeile>(ziel, SageSdataReadClient.VertragResources,
            SdataLagerplatzReader.RessourceLagerplaetze, null, SelectPlaetze, "PlatzID", ct);
        hinweise.Add(new(bestand.Zusammenfassung));
        hinweise.Add(new(plaetze.Zusammenfassung));

        var result = Abbilden(bestand.Zeilen, plaetze.Zeilen);

        await ZaehlenAsync(ziel, RessourceChargen, "Chargen", hinweise, ct);
        await ZaehlenAsync(ziel, RessourceSeriennummern, "Seriennummern", hinweise, ct);
        _logger.LogInformation("Sage (SData) liefert {Count} Bestand-Tupel. {Hinweise}", result.Count, string.Join(" | ", hinweise.Select(h => h.Text)));
        return result;
    }

    /// <summary>Gruppierung wie SQL <c>GROUP BY Artikelnummer, Kurzbezeichnung, Lagerkennung</c> (Gross/Klein und Leerzeichen am Ende egal).</summary>
    internal static List<SageBestandDto> Abbilden(IReadOnlyList<SdataBestandZeile> zeilen, IReadOnlyList<SdataLagerplatzZeile> plaetze)
    {
        // Unlesbare Zeile = Feldname/Schema passt nicht: werfen statt still ueberspringen (sonst Nullsetzen-Kandidaten).
        var kaputt = zeilen.Count(z => string.IsNullOrWhiteSpace(z.Artikelnummer) || z.PlatzID is null);
        if (kaputt > 0)
            throw new SdataLeseException(RessourceBestand, 0,
                $"{kaputt} von {zeilen.Count} Zeilen ohne Artikelnummer/PlatzID – Feldnamen der Ressource prüfen (P-S1).");
        if (zeilen.Count > 0 && zeilen.All(z => z.Bestand is null))
            throw new SdataLeseException(RessourceBestand, 0, $"{zeilen.Count} Zeilen, aber keine mit Bestand – Feldname 'Bestand' prüfen (P-S1).");

        var platzById = plaetze.Where(p => p.PlatzID is not null).GroupBy(p => p.PlatzID!.Value).ToDictionary(g => g.Key, g => g.First());
        return zeilen
            .Where(z => z.Bestand is not null)
            .Select(z => (z, Platz: platzById.GetValueOrDefault(z.PlatzID!.Value)))
            // ponytail: Platz unbekannt -> Schluessel PlatzID statt der Lagerkennung der Bestandszeile (SQL); der Abgleich
            // ueberspringt Zeilen ohne Lagerplatz ohnehin, nur die Zahl "uebersprungen" kann bei solchen Dateninkonsistenzen abweichen.
            .GroupBy(x => (Artikel: x.z.Artikelnummer!.TrimEnd().ToUpperInvariant(),
                           Platz: x.Platz?.Kurzbezeichnung?.TrimEnd().ToUpperInvariant(),
                           Ort: x.Platz is null ? "#" + x.z.PlatzID : x.Platz.Lagerkennung?.TrimEnd().ToUpperInvariant()))
            .Select(g => new SageBestandDto(g.First().z.Artikelnummer, g.First().Platz?.Kurzbezeichnung, g.Sum(x => x.z.Bestand!.Value)))
            .ToList();
    }

    /// <summary>F4: Chargen/Seriennummern nur zaehlen. Ein Fehler hier ist eine Warnung, kein Abbruch des Bestand-Abgleichs (Vorbereitung).</summary>
    private async Task ZaehlenAsync(SdataLeseZiel ziel, string ressource, string was, List<SdataHinweis> hinweise, CancellationToken ct)
    {
        try
        {
            // TODO(P-S1): Feldname Artikelnummer fuer orderBy angenommen (Ressource nicht im P-S1-Schema).
            var r = await _client.QueryAllAsync<JsonElement>(ziel, SageSdataReadClient.VertragResources, ressource, null, null, OrderByZaehlen, ct);
            hinweise.Add(new($"{was} (nur gezählt, nicht gespeichert): {r.Zeilen.Count} Zeile(n) in {ressource}."));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "SData {Ressource} nicht lesbar.", ressource);
            var text = $"{was} nicht gezählt – {ex.Message}";
            hinweise.Add(new(text.Length <= 900 ? text : text[..900], Warnung: true)); // SyncLogs.Message = 1000
        }
    }
}
