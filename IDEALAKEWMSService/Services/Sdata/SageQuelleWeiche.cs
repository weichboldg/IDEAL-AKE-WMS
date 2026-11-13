using IDEALAKEWMSService.Common;
using IdealAkeWms.Services.SageSdata;
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
/// <see cref="SyncLogServices.SageQuellenvergleich"/> (Referenz = Bereich). Ein SData-Fehler beendet nur den Vergleichslauf mit
/// Fehler, loest die Fehlermail aus (G10) und blockiert den Abgleich NICHT: er laeuft mit den View-Zeilen weiter (E1-Korrektur).</item>
/// </list>
/// Warnungen (unbekannter Wert, SData gesperrt) landen je Aufruf genau einmal im Lauf <c>SageQuellenvergleich</c>.
/// Die Abgleich-Services bleiben unveraendert — sie sehen nur ihr Reader-Interface. <see cref="GenutzteQuelle"/> nennt die
/// tatsaechlich gewaehlte Quelle des letzten Aufrufs (Info-Zeile „Quelle: …“ im Protokolllauf des Bereichs, F1).
/// </summary>
public abstract class SageQuelleWeiche
{
    private readonly IConfiguration _configuration;
    private readonly ISyncErrorNotifier _errorNotifier;
    private readonly ISyncLogger _syncLogger;

    protected SageQuelleWeiche(IConfiguration configuration, ISyncErrorNotifier errorNotifier, ISyncLogger syncLogger)
    {
        _configuration = configuration;
        _errorNotifier = errorNotifier;
        _syncLogger = syncLogger;
    }

    /// <summary>Quelle des letzten Aufrufs nach Auswertung des Schalters (unbekannt/gesperrt = View); null = noch kein Aufruf.
    /// Wird VOR dem Lesen gesetzt, damit der Abgleich auch im Fehlerfall weiss, welche Quelle gelesen hat (G10).</summary>
    public SageQuelle? GenutzteQuelle { get; private set; }

    /// <summary>Key-Suffix, eine Konstante aus <see cref="SageQuelleSchalter"/>.</summary>
    protected abstract string Bereich { get; }

    /// <summary>Warnung des letzten Aufrufs (SData gesperrt, unbekannter Wert) oder null — fuer die Info-Zeile im Lauf des Bereichs (B6).</summary>
    public string? QuellenWarnung { get; private set; }

    protected IConfiguration Configuration => _configuration;

    /// <summary>Gesetzt (z. B. „P-F2 …“) = Umschalten auf SData gesperrt, bis die Pruefabfrage beantwortet ist. Regel an EINEM Ort:
    /// <see cref="SageQuelleSperre"/> (auch Grundlage des Warnhinweises in /ServiceSettings).</summary>
    protected virtual Task<string?> SdataGesperrtBisAsync(CancellationToken ct) => Task.FromResult(SageQuelleSperre.Grund(Bereich, null));

    /// <summary>Zeile „Quelle: …“ im Lauf des Bereichs (F1); wirkt die Quelle wegen Sperre/unbekanntem Wert wie View, als Warnung
    /// mit Grund (B6) — nicht nur im Lauf „Sage-Quellenvergleich“.</summary>
    public async Task LogQuelleAsync(ISyncRun run, CancellationToken ct)
    {
        if (QuellenWarnung is { } w) await run.LogWarningAsync($"Quelle: {GenutzteQuelle} – {w}", ct: ct);
        else await run.LogInfoAsync($"Quelle: {GenutzteQuelle}", ct: ct);
        if (ZugangsWarnung is { } z) await run.LogWarningAsync(z, ct: ct); // v1.99.14 F6: einmal je Lauf, ohne Werte
    }

    /// <summary>v1.99.14 (F6): Rueckfall auf appsettings-Zugangsdaten beim letzten SData-/Vergleich-Aufruf, sonst null.</summary>
    public string? ZugangsWarnung { get; private set; }

    /// <summary>Warnung des Zugangs; ueberschreibbar fuer Tests (kein DB-Zugriff). Lesefehler = keine Warnung (der SData-Aufruf meldet ihn).</summary>
    protected virtual async Task<string?> LeseZugangsWarnungAsync(CancellationToken ct)
    {
        try { return (await SdataLeseZielLeser.ZugangAsync(_configuration, ct)).Warnung; }
        catch (Exception ex) when (ex is not OperationCanceledException) { return null; }
    }

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
        var wahl = SageQuelleSchalter.Parse(Bereich, await LeseSchalterAsync(ct), await SdataGesperrtBisAsync(ct));
        GenutzteQuelle = wahl.Quelle;
        QuellenWarnung = wahl.Warnung;
        ZugangsWarnung = wahl.Quelle == SageQuelle.View ? null : await LeseZugangsWarnungAsync(ct);
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
            // Vergleich darf den View-Abgleich nie blockieren: Fehler protokollieren + melden (G10), mit den View-Zeilen weiter.
            await lauf.FinishFailedAsync($"{Bereich}: SData nicht lesbar – {ex.Message}", ct: ct);
            await _errorNotifier.NotifyAsync($"Sage-Quellenvergleich {Bereich}", ex, ct);
            return viewErgebnis;
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
