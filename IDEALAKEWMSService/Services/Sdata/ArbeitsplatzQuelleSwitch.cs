using IdealAkeWms.Services.SyncLogger;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E6a, G12): Quellen-Weiche fuer <see cref="ISageArbeitsplatzReader"/> (Schalter
/// <c>Sync:Quelle:Arbeitsplaetze</c>, Default View). View = <see cref="SageArbeitsplatzReader"/> (unveraendert),
/// SData = <see cref="SdataArbeitsplatzReader"/>. Gilt fuer BEIDE Leser der Schnittstelle — Werkbank-Anlage (A7) und
/// Arbeitsgang-Kuerzel (A8, kein eigener Schalter). Registrierung: <see cref="SdataRegistrierung.AddArbeitsplatzQuelle"/>.
/// </summary>
public class ArbeitsplatzQuelleSwitch : SageQuelleWeiche, ISageArbeitsplatzReader
{
    /// <summary>Vergleich je Arbeitsplatznummer (getrimmt) auf dem Namen, den der View-Weg schreibt.
    /// TODO(P-A1): Arbeitsschritt erst vergleichen, wenn SData ihn liefert — sonst waere jeder Schluessel abweichend.</summary>
    public static readonly SdataVergleich.Feld<SageArbeitsplatzDto>[] VergleichsFelder =
    {
        new("Name", d => d.Bezeichnung1?.Trim()),
    };

    private readonly ISageArbeitsplatzReader _view;
    private readonly ISageArbeitsplatzReader _sdata;

    public ArbeitsplatzQuelleSwitch(ISageArbeitsplatzReader view, ISageArbeitsplatzReader sdata, IConfiguration configuration,
        ISyncErrorNotifier errorNotifier, ISyncLogger syncLogger)
        : base(configuration, errorNotifier, syncLogger)
    {
        _view = view;
        _sdata = sdata;
    }

    protected override string Bereich => SageQuelleSchalter.Arbeitsplaetze;

    public Task<SageArbeitsplatzReadResult> GetActiveAsync(string quotedSource, int mandant, CancellationToken ct = default) =>
        LeseAsync<SageArbeitsplatzReadResult, SageArbeitsplatzDto>(
            c => _view.GetActiveAsync(quotedSource, mandant, c),
            c => _sdata.GetActiveAsync(quotedSource, mandant, c),
            r => r.Rows, d => d.Arbeitsplatznummer?.Trim(), VergleichsFelder, ct);
}
