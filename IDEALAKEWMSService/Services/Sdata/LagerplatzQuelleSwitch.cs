using IdealAkeWms.Services.SyncLogger;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E1): Quellen-Weiche fuer <see cref="ISageLagerplatzReader"/> (Schalter
/// <c>Sync:Quelle:Lagerplaetze</c>, Default View). View = <see cref="SageLagerplatzReader"/> (unveraendert),
/// SData = <see cref="SdataLagerplatzReader"/>. Registrierung: <see cref="SdataRegistrierung.AddLagerplatzQuelle"/>.
/// </summary>
public class LagerplatzQuelleSwitch : SageQuelleWeiche, ISageLagerplatzReader
{
    /// <summary>Vergleich je Kurzbezeichnung (= Code im WMS) auf den Feldern, die der View-Weg schreibt (AK 8).</summary>
    public static readonly SdataVergleich.Feld<SageLagerplatzDto>[] VergleichsFelder =
    {
        new("Zone", d => d.Lagerkennung?.Trim()),
        new("Description", d => d.Platzbezeichnung?.Trim()),
        new("SageLagerplatzId", d => d.PlatzId),
    };

    private readonly ISageLagerplatzReader _view;
    private readonly ISageLagerplatzReader _sdata;

    public LagerplatzQuelleSwitch(ISageLagerplatzReader view, ISageLagerplatzReader sdata, IConfiguration configuration,
        ISyncErrorNotifier errorNotifier, ISyncLogger syncLogger)
        : base(configuration, errorNotifier, syncLogger)
    {
        _view = view;
        _sdata = sdata;
    }

    protected override string Bereich => SageQuelleSchalter.Lagerplaetze;

    public Task<List<SageLagerplatzDto>> GetAllActiveAsync(CancellationToken ct = default) =>
        LeseAsync<List<SageLagerplatzDto>, SageLagerplatzDto>(_view.GetAllActiveAsync, _sdata.GetAllActiveAsync,
            l => l, Schluessel, VergleichsFelder, ct);

    // Aktiver Lagerort ohne Plaetze (LEFT JOIN): eigener Schluessel je Lagerort statt eines gemeinsamen Leer-Schluessels.
    internal static string Schluessel(SageLagerplatzDto d) =>
        string.IsNullOrWhiteSpace(d.Kurzbezeichnung) ? $"(ohne Platz) {d.Lagerkennung?.Trim()}" : d.Kurzbezeichnung.Trim();
}
