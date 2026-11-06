using IdealAkeWms.Services.SageSdata;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>PpsArbeitsplaetzeInfo</c> (Namen exakt wie in der SData-Antwort, Typen P-S1).</summary>
public sealed record SdataArbeitsplatzZeile(string? Arbeitsplatznummer, string? Bezeichnung1, string? Schichtplannummer,
    string? Kostenstelle, bool? Aktiv, string? BdeTerminalId);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E6a/F8, G12): Arbeitsplaetze aus SData (<c>PpsArbeitsplaetzeInfo</c>, Vertrag
/// CommonWawiServicesResources) fuer BEIDE Abgleiche, die <see cref="ISageArbeitsplatzReader"/> lesen (Werkbank-Anlage und
/// Arbeitsgang-Kuerzel). Bildet <c>SageArbeitsplatzSql.TableSql</c> nach: nur <c>Aktiv = -1</c> (clientseitig — die Schreibweise eines
/// Bool-Filters im where ist am Server nicht belegt). <paramref name="quotedSource"/> und <c>mandant</c> wirken hier nicht: die
/// Ressource ist fest, den Mandanten legt das Dataset fest (P-S2, am IDEAL-Endpunkt erneut pruefen).
/// Wirft bei jedem Lesefehler (<see cref="SdataLeseException"/>), nie leere Liste statt Fehler.
/// </summary>
public sealed class SdataArbeitsplatzReader : ISageArbeitsplatzReader
{
    public const string Ressource = "PpsArbeitsplaetzeInfo";

    // TODO(P-A1): USER_ArbeitsSchritt steht am AKE-Test nicht im Schema (Abfrage damit = HTTP 500 "Ungueltiger Spaltenname").
    // Erst aufnehmen, wenn P-A1 es am IDEAL-Endpunkt belegt; bis dahin Arbeitsschritt = null und ArbeitsschrittGeliefert = false.
    public const string Select = "Arbeitsplatznummer,Bezeichnung1,Schichtplannummer,Kostenstelle,Aktiv,BdeTerminalId";

    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SdataArbeitsplatzReader> _logger;

    public SdataArbeitsplatzReader(ISageSdataReadClient client, IConfiguration configuration, ILogger<SdataArbeitsplatzReader> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SageArbeitsplatzReadResult> GetActiveAsync(string quotedSource, int mandant, CancellationToken ct = default) =>
        await LesenAsync(await SdataLeseZielLeser.LesenAsync(_configuration, ct), ct);

    public async Task<SageArbeitsplatzReadResult> LesenAsync(SdataLeseZiel ziel, CancellationToken ct)
    {
        var seiten = await _client.QueryAllAsync<SdataArbeitsplatzZeile>(ziel, SageSdataReadClient.VertragResources, Ressource,
            null, Select, "Arbeitsplatznummer", ct);

        // Getrimmt wird Nummer/Bezeichnung im Abgleich (wie beim SQL-Weg); die Zusatzfelder schreibt er unveraendert -> hier trimmen.
        var rows = seiten.Zeilen
            .Where(z => z.Aktiv == true)
            .Select(z => new SageArbeitsplatzDto(z.Arbeitsplatznummer, z.Bezeichnung1, Arbeitsschritt: null,
                z.BdeTerminalId?.Trim(), z.Kostenstelle?.Trim(), z.Schichtplannummer?.Trim()))
            .ToList();

        _logger.LogInformation("{Seiten} | Sage (SData) liefert {Count} aktive Arbeitsplaetze (ohne Arbeitsschritt-Kuerzel, P-A1).",
            seiten.Zusammenfassung, rows.Count);
        return new SageArbeitsplatzReadResult(true, rows, ArbeitsschrittGeliefert: false);
    }
}
