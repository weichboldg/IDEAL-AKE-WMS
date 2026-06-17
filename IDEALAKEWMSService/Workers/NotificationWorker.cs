using IDEALAKEWMSService.Services;
using System.Text;

namespace IDEALAKEWMSService.Workers;

public class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public NotificationWorker(ILogger<NotificationWorker> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationWorker gestartet.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalMinutes = _configuration.GetValue<int>("WorkerSettings:NotificationCheckIntervalMinutes", 60);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var stockCheck = scope.ServiceProvider.GetRequiredService<IStockCheckService>();
                var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();

                var enabled = true;
                // ServiceSetting aus DB lesen wenn möglich
                try
                {
                    var enabledValue = await GetServiceSettingAsync("Notifications:MeldebestandEnabled", stoppingToken);
                    enabled = !string.Equals(enabledValue, "false", StringComparison.OrdinalIgnoreCase);
                }
                catch { /* Fallback: enabled */ }

                if (!enabled)
                {
                    _logger.LogDebug("Meldebestand-Benachrichtigung deaktiviert (Notifications:MeldebestandEnabled = false).");
                }
                else
                {
                    _logger.LogInformation("Meldebestand-Prüfung startet...");
                    var items = await stockCheck.GetArticlesBelowReorderLevelAsync(stoppingToken);

                    if (items.Count == 0)
                    {
                        _logger.LogInformation("Kein Artikel unter Meldebestand — keine Mail erforderlich.");
                    }
                    else
                    {
                        _logger.LogInformation("{Count} Artikel unter Meldebestand gefunden, sende Benachrichtigung.", items.Count);

                        var recipients = await stockCheck.GetNotificationRecipientsAsync(stoppingToken);
                        var subject = await GetServiceSettingAsync("Notifications:MeldebestandSubject", stoppingToken)
                            ?? "Meldebestand unterschritten — IDEAL AKE WMS";
                        var baseUrl = await GetServiceSettingAsync("Notifications:AppBaseUrl", stoppingToken) ?? "";

                        var htmlBody = BuildMeldebestandHtml(items, baseUrl);
                        await mailService.SendAsync(subject, htmlBody, recipients, ct: stoppingToken);
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unerwarteter Fehler im NotificationWorker.");
            }

            _logger.LogDebug("NotificationWorker: Nächster Durchlauf in {IntervalMinutes} Minuten.", intervalMinutes);
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }

        _logger.LogInformation("NotificationWorker gestoppt.");
    }

    private async Task<string?> GetServiceSettingAsync(string key, CancellationToken ct)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString)) return null;

        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
            "SELECT [Value] FROM [ServiceSettings] WHERE [Key] = @Key", conn);
        cmd.Parameters.AddWithValue("@Key", key);
        return await cmd.ExecuteScalarAsync(ct) as string;
    }

    private static string BuildMeldebestandHtml(List<StockBelowReorderItem> items, string baseUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            <!DOCTYPE html>
            <html lang="de">
            <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width,initial-scale=1" />
            <title>Meldebestand unterschritten</title>
            </head>
            <body style="margin:0;padding:0;font-family:Arial,Helvetica,sans-serif;background:#f4f6f9;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6f9;padding:20px 0;">
            <tr><td align="center">
            <table width="640" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);">

            <!-- Header -->
            <tr>
              <td style="background:#053153;padding:24px 32px;">
                <h1 style="margin:0;color:#ffffff;font-size:20px;font-weight:bold;letter-spacing:0.5px;">IDEAL AKE WMS</h1>
                <p style="margin:6px 0 0;color:#43A6E2;font-size:14px;">Meldebestand unterschritten</p>
              </td>
            </tr>

            <!-- Intro -->
            <tr>
              <td style="padding:24px 32px 16px;">
                <p style="margin:0;color:#333;font-size:15px;">
                  Die folgenden Artikel haben ihren Meldebestand unterschritten und sollten zeitnah nachbestellt werden:
                </p>
              </td>
            </tr>

            <!-- Tabelle -->
            <tr>
              <td style="padding:0 32px 24px;">
                <table width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;font-size:13px;">
                  <thead>
                    <tr style="background:#053153;color:#ffffff;">
                      <th style="padding:10px 12px;text-align:left;border-radius:4px 0 0 0;">Artikelnummer</th>
                      <th style="padding:10px 12px;text-align:left;">Bezeichnung</th>
                      <th style="padding:10px 12px;text-align:right;">Meldebestand</th>
                      <th style="padding:10px 12px;text-align:right;border-radius:0 4px 0 0;">Aktuell</th>
                    </tr>
                  </thead>
                  <tbody>
            """);

        foreach (var (i, item) in items.Select((x, i) => (i, x)))
        {
            var rowBg = i % 2 == 0 ? "#ffffff" : "#f8f9fa";
            var stockColor = item.CurrentStock <= 0 ? "#dc3545" : "#E87A1E";
            var locations = item.StorageLocations.Count > 0 ? string.Join(", ", item.StorageLocations) : "—";

            sb.AppendLine($"""
                      <tr style="background:{rowBg};border-bottom:1px solid #eee;">
                        <td style="padding:10px 12px;font-weight:bold;color:#053153;">{System.Web.HttpUtility.HtmlEncode(item.ArticleNumber)}</td>
                        <td style="padding:10px 12px;color:#555;">
                          {System.Web.HttpUtility.HtmlEncode(item.Description ?? "—")}
                          <br/><span style="font-size:11px;color:#999;">Lager: {System.Web.HttpUtility.HtmlEncode(locations)}</span>
                        </td>
                        <td style="padding:10px 12px;text-align:right;color:#333;">{item.ReorderLevel:N2} {System.Web.HttpUtility.HtmlEncode(item.Unit ?? "")}</td>
                        <td style="padding:10px 12px;text-align:right;color:{stockColor};font-weight:bold;">{item.CurrentStock:N2} {System.Web.HttpUtility.HtmlEncode(item.Unit ?? "")}</td>
                      </tr>
                """);
        }

        sb.AppendLine("          </tbody></table>");
        sb.AppendLine("      </td></tr>");

        // Link zur App
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            sb.AppendLine($"""
                <tr>
                  <td style="padding:0 32px 24px;">
                    <a href="{baseUrl}/StockOverview"
                       style="display:inline-block;background:#43A6E2;color:#ffffff;padding:10px 20px;border-radius:4px;text-decoration:none;font-size:14px;font-weight:bold;">
                      Zur Bestandsübersicht
                    </a>
                  </td>
                </tr>
                """);
        }

        sb.AppendLine($"""
            <!-- Footer -->
            <tr>
              <td style="background:#f8f9fa;padding:16px 32px;border-top:1px solid #eee;">
                <p style="margin:0;color:#999;font-size:12px;">
                  Diese Nachricht wurde automatisch vom IDEAL AKE WMS Service generiert.<br />
                  Zeitpunkt: {DateTime.Now:dd.MM.yyyy HH:mm} Uhr
                </p>
              </td>
            </tr>

            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """);

        return sb.ToString();
    }
}
