using IDEALAKEWMSService.Common;
using IdealAkeWms.Services.SyncLogger;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E0, Technischer Loesungsentwurf): Basis der Quellen-Weichen
/// (<c>…QuelleSwitch : I…Reader</c>, Muster <c>BomRepositoryMasterSwitch</c>, ADR 0013). Eine Weiche je Reader-Interface
/// liest bei JEDEM Aufruf <c>Sync:Quelle:&lt;Bereich&gt;</c> (DB-first, DB-Fehler = View) und delegiert:
/// <list type="bullet">
/// <item><c>View</c>: nur der bestehende SQL-Reader (Verhalten unveraendert).</item>
/// <item><c>SData</c>: nur der SData-Reader; dessen Fehler werfen durch (kein stiller Rueckfall auf View).</item>
/// <item><c>Vergleich</c>: beide, Rueckgabe = View-Ergebnis; Abweichungen per <see cref="SdataVergleich"/> in den Lauf
/// <see cref="SyncLogServices.SageQuellenvergleich"/> (Referenz = Bereich). Ein SData-Fehler beendet den Vergleichslauf mit
/// Fehler und wirft weiter (G10: auch im Vergleich Fehlermail ueber den Abgleich).</item>
/// </list>
/// Warnungen (unbekannter Wert, SData gesperrt) landen je Aufruf genau einmal im Lauf <c>SageQuellenvergleich</c>.
/// Die Abgleich-Services bleiben unveraendert — sie sehen nur ihr Reader-Interface.
/// </summary>
public abstract class SageQuelleWeiche
{
    private readonly IConfiguration _configuration;
    private readonly ISyncLogger _syncLogger;

    protected SageQuelleWeiche(IConfiguration configuration, ISyncLogger syncLogger)
    {
        _configuration = configuration;
        _syncLogger = syncLogger;
    }

    /// <summary>Key-Suffix, eine Konstante aus <see cref="SageQuelleSchalter"/>.</summary>
    protected abstract string Bereich { get; }

    /// <summary>Gesetzt (z. B. <c>"P-F2"</c>) = Umschalten auf SData gesperrt, bis die Pruefabfrage beantwortet ist.</summary>
    protected virtual string? SdataGesperrtBis => null;

    /// <summary>Rohwert des Schalters; ueberschreibbar fuer Tests.</summary>
    protected virtual Task<string?> LeseSchalterAsync(CancellationToken ct) =>
        ServiceSettings.GetValueSafeAsync(_configuration, SageQuelleSchalter.Key(Bereich), ct);

    protected async Task<T> LeseAsync<T, TZeile>(
        Func<CancellationToken, Task<T>> view,
        Func<CancellationToken, Task<T>> sdata,
        Func<T, IEnumerable<TZeile>> zeilen,
        Func<TZeile, string?> schluessel,
        IReadOnlyList<SdataVergleich.Feld<TZeile>> felder,
        CancellationToken ct)
    {
        var wahl = SageQuelleSchalter.Parse(Bereich, await LeseSchalterAsync(ct), SdataGesperrtBis);
        if (wahl.Warnung is not null)
        {
            await using var warnLauf = await _syncLogger.BeginRunAsync(SyncLogServices.SageQuellenvergleich, ct);
            await warnLauf.LogWarningAsync(wahl.Warnung, Bereich, ct);
            await warnLauf.FinishSuccessAsync(ct: ct);
        }

        switch (wahl.Quelle)
        {
            case SageQuelle.SData:
                return await sdata(ct);
            case SageQuelle.Vergleich:
                break;
            default:
                return await view(ct);
        }

        var viewErgebnis = await view(ct);
        await using var lauf = await _syncLogger.BeginRunAsync(SyncLogServices.SageQuellenvergleich, ct);
        T sdataErgebnis;
        try
        {
            sdataErgebnis = await sdata(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await lauf.FinishFailedAsync($"{Bereich}: SData nicht lesbar – {ex.Message}", ct: ct);
            throw;
        }

        var e = SdataVergleich.Vergleiche(zeilen(viewErgebnis), zeilen(sdataErgebnis), schluessel, felder);
        await lauf.LogInfoAsync($"{Bereich}: {e.AbweichendeSchluessel} abweichende Schlüssel (View {e.SchluesselView}, SData {e.SchluesselSData}); geschrieben wird nur aus der View.", Bereich, ct);
        foreach (var a in e.Beispiele)
        {
            var text = $"{Bereich} [{a.Schluessel}] {a.Feld}: View '{a.ViewWert}' / SData '{a.SDataWert}'";
            await lauf.LogWarningAsync(text.Length <= 1000 ? text : text[..1000], Bereich, ct); // SyncLogs.Message = 1000
        }
        await lauf.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["Schlüssel View"] = e.SchluesselView,
            ["Schlüssel SData"] = e.SchluesselSData,
            ["abweichend"] = e.AbweichendeSchluessel,
        }, ct: ct);
        return viewErgebnis;
    }
}
