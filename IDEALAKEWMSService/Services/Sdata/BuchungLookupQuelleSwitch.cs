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
    public Task<bool> ExistsAsync(int stockMovementId, CancellationToken ct = default) =>
        LeseAsync<bool, string>(c => _view.ExistsAsync(stockMovementId, c), c => _sdata.ExistsAsync(stockMovementId, c),
            gefunden => gefunden ? new[] { SageBookingCorrelation.Marker(stockMovementId) } : Array.Empty<string>(),
            s => s, Array.Empty<SdataVergleich.Feld<string>>(), ct);
}
