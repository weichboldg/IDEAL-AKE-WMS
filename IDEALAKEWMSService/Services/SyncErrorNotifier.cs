using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Versendet bei jedem Sync-Fehler eine Detail-Fehlermail (Schritt, Zeit, Maschine,
/// Version, Exception + Stacktrace). <c>ErrorNotification:Enabled</c> + <c>Recipients</c>
/// werden seit v1.25.0 DB-first aus der <c>[ServiceSettings]</c>-Tabelle gelesen
/// (steuerbar unter <c>/ServiceSettings</c>; appsettings.json wird dafuer nicht mehr
/// gelesen). Wirft NIE — ein Versand-Fehler darf den Worker nicht crashen.
/// </summary>
public class SyncErrorNotifier : ISyncErrorNotifier
{
    private readonly IMailService _mail;
    private readonly IConfiguration _config;
    private readonly ILogger<SyncErrorNotifier> _logger;

    public SyncErrorNotifier(IMailService mail, IConfiguration config, ILogger<SyncErrorNotifier> logger)
    {
        _mail = mail;
        _config = config;
        _logger = logger;
    }

    public async Task NotifyAsync(string stepName, Exception ex, CancellationToken ct = default)
    {
        try
        {
            var enabled = await IDEALAKEWMSService.Common.ServiceSettings.GetBoolSafeAsync(_config, "ErrorNotification:Enabled", false, ct);
            var recipientsRaw = await IDEALAKEWMSService.Common.ServiceSettings.GetValueSafeAsync(_config, "ErrorNotification:Recipients", ct);
            var recipients = (recipientsRaw ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!enabled || recipients.Length == 0)
            {
                _logger.LogDebug(
                    "Fehlermail nicht versendet (Enabled={Enabled}, Empfaenger={Count}) fuer Schritt {Step}.",
                    enabled, recipients.Length, stepName);
                return;
            }

            var subject = $"[IDEAL-AKE-WMS] Sync-Fehler: {stepName}";
            var when = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            var machine = Environment.MachineName;
            var version = IDEALAKEWMSService.AppVersion.Version;

            var text = new StringBuilder();
            text.AppendLine($"Schritt:  {stepName}");
            text.AppendLine($"Zeit:     {when}");
            text.AppendLine($"Maschine: {machine}");
            text.AppendLine($"Version:  {version}");
            text.AppendLine();
            text.AppendLine($"Fehler: {ex.Message}");
            text.AppendLine();
            text.AppendLine("Details:");
            text.AppendLine(ex.ToString());

            var html = new StringBuilder();
            html.Append("<p><strong>Sync-Fehler</strong></p>");
            html.Append("<table>");
            html.Append($"<tr><td><strong>Schritt:</strong></td><td>{WebUtility.HtmlEncode(stepName)}</td></tr>");
            html.Append($"<tr><td><strong>Zeit:</strong></td><td>{WebUtility.HtmlEncode(when)}</td></tr>");
            html.Append($"<tr><td><strong>Maschine:</strong></td><td>{WebUtility.HtmlEncode(machine)}</td></tr>");
            html.Append($"<tr><td><strong>Version:</strong></td><td>{WebUtility.HtmlEncode(version)}</td></tr>");
            html.Append("</table>");
            html.Append($"<p><strong>Fehler:</strong> {WebUtility.HtmlEncode(ex.Message)}</p>");
            html.Append($"<pre>{WebUtility.HtmlEncode(ex.ToString())}</pre>");

            await _mail.SendAsync(subject, html.ToString(), recipients, text.ToString(), ct);
        }
        catch (Exception sendEx)
        {
            _logger.LogError(sendEx, "Fehlermail-Versand fehlgeschlagen fuer Schritt {Step}.", stepName);
        }
    }
}
