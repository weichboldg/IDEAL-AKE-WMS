using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace IDEALAKEWMSService.Services;

public class WarehouseRequisitionEmailService : IWarehouseRequisitionEmailService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IMailService _mail;
    private readonly IConfiguration _config;
    private readonly ILogger<WarehouseRequisitionEmailService> _logger;

    public WarehouseRequisitionEmailService(
        ApplicationDbContext ctx,
        IWarehouseRequisitionRepository repo,
        IMailService mail,
        IConfiguration config,
        ILogger<WarehouseRequisitionEmailService> logger)
    {
        _ctx = ctx; _repo = repo; _mail = mail; _config = config; _logger = logger;
    }

    public async Task<EmailResult> SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var baseUrl = await GetBaseUrlAsync(ct);

        // Submit-Mails
        var submits = await _repo.GetPendingSubmitEmailsAsync();
        var submitCount = 0;
        foreach (var r in submits)
        {
            try
            {
                var emails = r.OrderRecipientGroup!.Recipients.Where(x => x.IsActive).Select(x => x.Email).Distinct().ToList();
                if (emails.Count == 0)
                {
                    errors.Add($"Lagerbestellung #{r.Id}: keine aktiven Empfaenger.");
                    continue;
                }
                var subject = $"Lagerbestellung #{r.Id} \u2014 Werkbank {r.ProductionWorkplace.Name}";
                var body = BuildSubmitBody(r, baseUrl);
                if (!dryRun)
                {
                    await _mail.SendAsync(subject, body, emails, ct);
                    await _repo.MarkEmailSentAsync(r.Id, DateTime.Now);
                }
                submitCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Submit-Mail fuer Lagerbestellung {Id} fehlgeschlagen.", r.Id);
                errors.Add($"#{r.Id}: {ex.Message}");
            }
        }

        // Storno-Mails (nur wenn vorher Submit-Mail rausging)
        var cancels = await _repo.GetPendingCancellationEmailsAsync();
        var cancelCount = 0;
        foreach (var r in cancels)
        {
            try
            {
                var emails = r.OrderRecipientGroup!.Recipients.Where(x => x.IsActive).Select(x => x.Email).Distinct().ToList();
                if (emails.Count == 0)
                {
                    errors.Add($"Storno #{r.Id}: keine aktiven Empfaenger.");
                    continue;
                }
                var subject = $"[STORNO] Lagerbestellung #{r.Id} \u2014 Werkbank {r.ProductionWorkplace.Name}";
                var body = BuildCancellationBody(r, baseUrl);
                if (!dryRun)
                {
                    await _mail.SendAsync(subject, body, emails, ct);
                    await _repo.MarkCancellationEmailSentAsync(r.Id, DateTime.Now);
                }
                cancelCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Storno-Mail fuer Lagerbestellung {Id} fehlgeschlagen.", r.Id);
                errors.Add($"#{r.Id}: {ex.Message}");
            }
        }

        return new EmailResult(submitCount, cancelCount, errors);
    }

    private async Task<string> GetBaseUrlAsync(CancellationToken ct)
    {
        var connectionString = _config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString)) return "";
        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
            "SELECT [Value] FROM [ServiceSettings] WHERE [Key] = @Key", conn);
        cmd.Parameters.AddWithValue("@Key", "Notifications:AppBaseUrl");
        var v = await cmd.ExecuteScalarAsync(ct);
        return v?.ToString() ?? "";
    }

    private static string BuildSubmitBody(WarehouseRequisition r, string baseUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<html><body style='font-family:Segoe UI, Arial; color:#000;'>");
        sb.AppendLine($"<h2 style='color:#053153;'>Lagerbestellung #{r.Id}</h2>");
        sb.AppendLine($"<p><strong>Werkbank:</strong> {r.ProductionWorkplace.Name}<br />");
        sb.AppendLine($"<strong>Erfasser:</strong> {r.CreatedBy}<br />");
        sb.AppendLine($"<strong>Submit:</strong> {r.SubmittedAt:dd.MM.yyyy HH:mm}</p>");
        sb.AppendLine("<table style='border-collapse:collapse; border:1px solid #888;'>");
        sb.AppendLine("<thead><tr style='background:#f0f0f0;'><th style='border:1px solid #888; padding:4px;'>Pos</th><th style='border:1px solid #888; padding:4px;'>Artikel-Nr</th><th style='border:1px solid #888; padding:4px;'>Bezeichnung</th><th style='border:1px solid #888; padding:4px;'>Menge</th><th style='border:1px solid #888; padding:4px;'>ME</th></tr></thead><tbody>");
        foreach (var i in r.Items.OrderBy(i => i.Position))
        {
            sb.AppendLine($"<tr><td style='border:1px solid #888; padding:4px;'>{i.Position}</td><td style='border:1px solid #888; padding:4px;'>{i.ArticleNumber}</td><td style='border:1px solid #888; padding:4px;'>{i.ArticleDescription}</td><td style='border:1px solid #888; padding:4px;'>{i.QuantityRequested}</td><td style='border:1px solid #888; padding:4px;'>{i.Unit}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");
        if (!string.IsNullOrEmpty(baseUrl))
        {
            sb.AppendLine($"<p style='margin-top:20px;'><a href='{baseUrl}/WarehousePicking/Details/{r.Id}' style='display:inline-block;background:#43A6E2;color:#fff;padding:10px 20px;border-radius:4px;text-decoration:none;'>Lagerbestellung oeffnen</a></p>");
        }
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string BuildCancellationBody(WarehouseRequisition r, string baseUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<html><body style='font-family:Segoe UI, Arial; color:#000;'>");
        sb.AppendLine($"<h2 style='color:#c0392b;'>[STORNO] Lagerbestellung #{r.Id}</h2>");
        sb.AppendLine($"<p><strong>Werkbank:</strong> {r.ProductionWorkplace.Name}<br />");
        sb.AppendLine($"<strong>Erfasser:</strong> {r.CreatedBy}<br />");
        sb.AppendLine($"<strong>Storniert:</strong> {r.CancelledAt:dd.MM.yyyy HH:mm}</p>");
        if (!string.IsNullOrEmpty(r.CancellationReason))
        {
            sb.AppendLine($"<p><strong>Grund:</strong> {r.CancellationReason}</p>");
        }
        sb.AppendLine("<p><strong>Bitte nicht weiter bearbeiten.</strong></p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
