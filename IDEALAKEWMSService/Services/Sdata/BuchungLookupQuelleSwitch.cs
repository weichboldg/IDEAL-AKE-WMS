using IdealAkeWms.Services.SyncLogger;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E7/F9): Quellen-Weiche fuer <see cref="ISageBuchungLookupReader"/> (Schalter
/// <c>Sync:Quelle:LagerbuchungLookup</c>, Default View). View = <see cref="SageBuchungLookupReader"/> (unveraendert),
/// SData = <see cref="SdataBuchungLookupReader"/>. Im Modus SData wirft ein Fehler durch (Worker sendet dann nicht neu);
/// im Modus Vergleich gilt das View-Ergebnis, ein SData-Fehler wird nur protokolliert und gemeldet (Basis <see cref="SageQuelleWeiche"/>).
/// Registrierung: <see cref="SdataRegistrierung.AddBuchungLookupQuelle"/>.
/// </summary>
public class BuchungLookupQuelleSwitch : SageQuelleWeiche, ISageBuchungLookupReader
{
    private readonly ISageBuchungLookupReader _view;
    private readonly ISageBuchungLookupReader _sdata;

    public BuchungLookupQuelleSwitch(ISageBuchungLookupReader view, ISageBuchungLookupReader sdata, IConfiguration configuration,
        ISyncErrorNotifier errorNotifier, ISyncLogger syncLogger)
        : base(configuration, errorNotifier, syncLogger)
    {
        _view = view;
        _sdata = sdata;
    }

    protected override string Bereich => SageQuelleSchalter.LagerbuchungLookup;

    // Vergleich je Bewegung: "gefunden" = ein Schluessel SM#<id>#, "nicht gefunden" = keiner -> Abweichung (fehlt in View/SData).
    public Task<bool> ExistsAsync(string marker, CancellationToken ct = default) =>
        LeseAsync<bool, string>(c => _view.ExistsAsync(marker, c), c => _sdata.ExistsAsync(marker, c),
            gefunden => gefunden ? new[] { marker } : Array.Empty<string>(),
            s => s, Array.Empty<SdataVergleich.Feld<string>>(), ct);
}
