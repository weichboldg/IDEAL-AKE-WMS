using IdealAkeWms.Services.SageSdata;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>LagerorteInfo</c> (Namen exakt wie in der SData-Antwort).</summary>
public sealed record SdataLagerortZeile(string? Lagerkennung, string? Bezeichnung, string? Lagertechnik, bool? IstDispoLager, bool? Aktiv);

/// <summary>Zeile aus <c>LagerplaetzeInfo</c> (Namen exakt wie in der SData-Antwort).</summary>
public sealed record SdataLagerplatzZeile(int? PlatzID, string? Lagerkennung, string? Kurzbezeichnung, string? Platzbezeichnung,
    bool? IstGesperrt, bool? IstGesperrtInventur, decimal? Breite, decimal? Hoehe, decimal? Laenge, decimal? Volumen, decimal? Tragkraft);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E1/F3): Lagerplaetze aus SData (<c>LagerorteInfo</c> + <c>LagerplaetzeInfo</c>,
/// Vertrag CommonWawiServicesResources). Bildet die SQL-Semantik von <see cref="SageLagerplatzReader"/> nach:
/// <c>KHKLagerorte LEFT JOIN KHKLagerplaetze ON Lagerkennung WHERE Mandant = 1 AND Aktiv = -1</c> — also nur Plaetze aktiver
/// Lagerorte, und ein aktiver Lagerort ohne Plaetze liefert eine Zeile ohne Kurzbezeichnung (der Abgleich zaehlt sie wie heute
/// als uebersprungen). Wirft bei jedem Lesefehler (<see cref="SdataLeseException"/>), nie leere Liste statt Fehler.
/// </summary>
public sealed class SdataLagerplatzReader : ISageLagerplatzReader
{
    public const string RessourceLagerorte = "LagerorteInfo";
    public const string RessourceLagerplaetze = "LagerplaetzeInfo";
    public const string SelectLagerorte = "Lagerkennung,Bezeichnung,Lagertechnik,IstDispoLager,Aktiv";
    public const string SelectLagerplaetze = "PlatzID,Lagerkennung,Kurzbezeichnung,Platzbezeichnung,IstGesperrt,IstGesperrtInventur,Breite,Hoehe,Laenge,Volumen,Tragkraft";
    // Mandant wie SQL (SageLagerplatzReader: lo.Mandant = 1). P-S2: am AKE-Test legt das Dataset den Mandanten ohnehin fest.
    // Aktiv wird clientseitig gefiltert (36 Zeilen) — die Schreibweise eines Bool-Filters im where ist am Server nicht belegt.
    public const string WhereLagerorte = "Mandant eq 1";

    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SdataLagerplatzReader> _logger;

    public SdataLagerplatzReader(ISageSdataReadClient client, IConfiguration configuration, ILogger<SdataLagerplatzReader> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<SageLagerplatzDto>> GetAllActiveAsync(CancellationToken ct = default) =>
        await LesenAsync(await SdataLeseZielLeser.LesenAsync(_configuration, ct), ct);

    public async Task<List<SageLagerplatzDto>> LesenAsync(SdataLeseZiel ziel, CancellationToken ct)
    {
        var orte = await _client.QueryAllAsync<SdataLagerortZeile>(ziel, SageSdataReadClient.VertragResources, RessourceLagerorte,
            WhereLagerorte, SelectLagerorte, "Lagerkennung", ct);
        var plaetze = await _client.QueryAllAsync<SdataLagerplatzZeile>(ziel, SageSdataReadClient.VertragResources, RessourceLagerplaetze,
            null, SelectLagerplaetze, "PlatzID", ct);
        _logger.LogInformation("{Orte} | {Plaetze}", orte.Zusammenfassung, plaetze.Zusammenfassung);

        // SQL-Join "=": Gross/Klein egal, Leerzeichen am Ende egal (SQL-Server-Kollation).
        var plaetzeJeOrt = plaetze.Zeilen
            .Where(p => p.Lagerkennung is not null)
            .ToLookup(p => p.Lagerkennung!.TrimEnd(), StringComparer.OrdinalIgnoreCase);

        var result = new List<SageLagerplatzDto>();
        foreach (var o in orte.Zeilen.Where(o => o.Aktiv == true && !string.IsNullOrWhiteSpace(o.Lagerkennung)))
        {
            var ort = new SageLagerortDto(o.Lagerkennung!.Trim(), o.Bezeichnung?.Trim(), o.Lagertechnik?.Trim(), o.IstDispoLager == true);
            var zuOrt = plaetzeJeOrt[o.Lagerkennung!.TrimEnd()].ToList();
            if (zuOrt.Count == 0)
            {
                result.Add(new SageLagerplatzDto(o.Lagerkennung, null, null, null, ort)); // LEFT JOIN ohne Platz
                continue;
            }
            foreach (var p in zuOrt)
                result.Add(new SageLagerplatzDto(o.Lagerkennung, p.Kurzbezeichnung, p.Platzbezeichnung, p.PlatzID, ort,
                    p.IstGesperrt, p.IstGesperrtInventur, Runde(p.Breite), Runde(p.Hoehe), Runde(p.Laenge), Runde(p.Volumen), Runde(p.Tragkraft)));
        }

        _logger.LogInformation("Sage (SData) liefert {Count} aktive Lagerplaetze.", result.Count);
        return result;
    }

    // TODO(P-S1): Nachkommastellen der Sage-Felder unbelegt; Spalten sind decimal(18,3). Runden, damit der Abgleich nicht bei jedem
    // Lauf eine Abweichung zur gespeicherten (gerundeten) Zahl sieht.
    private static decimal? Runde(decimal? wert) => wert is null ? null : Math.Round(wert.Value, 3, MidpointRounding.AwayFromZero);
}
