using IdealAkeWms.Services.SyncLogger;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E2): Quellen-Weiche fuer <see cref="ISageBestandReader"/> (Schalter
/// <c>Sync:Quelle:Lagerbestand</c>, Default View). View = <see cref="SageBestandReader"/> (unveraendert),
/// SData = <see cref="SdataBestandReader"/>. Registrierung: <see cref="SdataRegistrierung.AddBestandQuelle"/>.
/// </summary>
public class BestandQuelleSwitch : SageQuelleWeiche, ISageBestandReader
{
    /// <summary>Vergleich je (Artikelnummer, Lagerplatz) auf dem Bestand — das Feld, aus dem der Abgleich bucht (AK 10).</summary>
    public static readonly SdataVergleich.Feld<SageBestandDto>[] VergleichsFelder =
    {
        new("Bestand", d => d.Bestand),
    };

    private readonly ISageBestandReader _view;
    private readonly ISageBestandReader _sdata;

    public BestandQuelleSwitch(ISageBestandReader view, ISageBestandReader sdata, IConfiguration configuration,
        ISyncErrorNotifier errorNotifier, ISyncLogger syncLogger)
        : base(configuration, errorNotifier, syncLogger)
    {
        _view = view;
        _sdata = sdata;
    }

    protected override string Bereich => SageQuelleSchalter.Lagerbestand;

    /// <summary>Hinweise des SData-Readers (Zeilen, Chargen/Seriennummern) — leer, wenn diesmal nur die View gelesen wurde.</summary>
    public IReadOnlyList<SdataHinweis> SdataHinweise =>
        GenutzteQuelle is SageQuelle.SData or SageQuelle.Vergleich && _sdata is SdataBestandReader r ? r.Hinweise : Array.Empty<SdataHinweis>();

    public Task<List<SageBestandDto>> GetAllAsync(CancellationToken ct = default) =>
        LeseAsync<List<SageBestandDto>, SageBestandDto>(_view.GetAllAsync, _sdata.GetAllAsync, l => l, Schluessel, VergleichsFelder, ct);

    internal static string Schluessel(SageBestandDto d) => $"{d.Artikelnummer?.Trim()} @ {d.Lagerplatz?.Trim()}";
}
