namespace IDEALAKEWMSService.Services;

public interface ISyncErrorNotifier
{
    /// <summary>Sendet eine Fehlermail (falls aktiviert + Empfaenger konfiguriert). Wirft NIE.</summary>
    Task NotifyAsync(string stepName, Exception ex, CancellationToken ct = default);
}
