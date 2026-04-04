using Microsoft.Data.SqlClient;
using System.Text;

namespace IDEALAKEWMSService.Services;

public class PartRequisitionEmailService : IPartRequisitionEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IMailService _mailService;
    private readonly ILogger<PartRequisitionEmailService> _logger;

    public PartRequisitionEmailService(
        IConfiguration configuration,
        IMailService mailService,
        ILogger<PartRequisitionEmailService> logger)
    {
        _configuration = configuration;
        _mailService = mailService;
        _logger = logger;
    }

    public async Task<int> SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

        var requisitions = await LoadUnsentRequisitionsAsync(connectionString, ct);
        if (requisitions.Count == 0) return 0;

        _logger.LogInformation("{Count} ungesendete Bedarfsmeldungen gefunden.", requisitions.Count);

        var groups = requisitions
            .GroupBy(r => new { r.SentToEmails, r.ProductionOrderId })
            .ToList();

        int sentCount = 0;
        foreach (var group in groups)
        {
            var items = group.ToList();
            var emails = (group.Key.SentToEmails ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (emails.Count == 0)
            {
                _logger.LogWarning("Bedarfsmeldungen {Ids} haben keine Empfänger — übersprungen.",
                    string.Join(", ", items.Select(i => i.Id)));
                continue;
            }

            var highestPriority = GetHighestPriority(items.Select(i => i.Priority));
            var subject = BuildSubject(highestPriority, items[0].OrderNumber);
            var htmlBody = BuildHtmlBody(items);

            if (dryRun)
            {
                _logger.LogInformation("[DryRun] Mail '{Subject}' an {Emails} — {Count} Teile",
                    subject, group.Key.SentToEmails, items.Count);
            }
            else
            {
                try
                {
                    await _mailService.SendAsync(subject, htmlBody, emails, ct);
                    await MarkAsSentAsync(connectionString, items.Select(i => i.Id).ToList(), ct);
                    sentCount += items.Count;
                    _logger.LogInformation("Bedarfsmeldung versendet: '{Subject}' an {Count} Empfänger.",
                        subject, emails.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Versand der Bedarfsmeldung(en) {Ids}",
                        string.Join(", ", items.Select(i => i.Id)));
                }
            }
        }

        return sentCount;
    }

    private string BuildSubject(string priority, string orderNumber)
    {
        var prefix = priority switch
        {
            "Eilt" => "[EILT] ",
            "Dringend" => "[DRINGEND] ",
            _ => ""
        };
        return $"{prefix}Bedarfsmeldung \u2014 FA {orderNumber}";
    }

    private string GetHighestPriority(IEnumerable<string> priorities)
    {
        if (priorities.Any(p => p == "Eilt")) return "Eilt";
        if (priorities.Any(p => p == "Dringend")) return "Dringend";
        return "Normal";
    }

    private string BuildHtmlBody(List<RequisitionEmailRow> items)
    {
        var first = items[0];
        var sb = new StringBuilder();

        sb.Append("""
            <!DOCTYPE html>
            <html><head><meta charset="utf-8"></head>
            <body style="font-family: Arial, sans-serif; margin: 0; padding: 0;">
            <div style="background-color: #053153; padding: 20px; text-align: center;">
                <h1 style="color: white; margin: 0; font-size: 22px;">IDEAL AKE WMS &mdash; Bedarfsmeldung</h1>
            </div>
            <div style="padding: 20px; max-width: 700px; margin: 0 auto;">
            """);

        sb.Append($"""
            <h2 style="color: #053153; border-bottom: 2px solid #43A6E2; padding-bottom: 8px;">
                Fertigungsauftrag {first.OrderNumber}
            </h2>
            <table style="margin-bottom: 20px;">
                <tr><td style="padding: 4px 16px 4px 0; font-weight: bold;">Kunde:</td><td>{HtmlEncode(first.Customer ?? "\u2014")}</td></tr>
                <tr><td style="padding: 4px 16px 4px 0; font-weight: bold;">Artikelbezeichnung:</td><td>{HtmlEncode(first.OrderDescription ?? "\u2014")}</td></tr>
                <tr><td style="padding: 4px 16px 4px 0; font-weight: bold;">Produktionsdatum:</td><td>{first.ProductionDate?.ToString("dd.MM.yyyy") ?? "\u2014"}</td></tr>
                <tr><td style="padding: 4px 16px 4px 0; font-weight: bold;">Lieferdatum:</td><td>{first.DeliveryDate?.ToString("dd.MM.yyyy") ?? "\u2014"}</td></tr>
            </table>
            """);

        sb.Append("""
            <table style="width: 100%; border-collapse: collapse; margin-bottom: 20px;">
            <thead>
                <tr style="background-color: #43A6E2; color: white;">
                    <th style="padding: 8px; text-align: left; border: 1px solid #ddd;">Ressourcen-Nr</th>
                    <th style="padding: 8px; text-align: left; border: 1px solid #ddd;">Bezeichnung</th>
                    <th style="padding: 8px; text-align: right; border: 1px solid #ddd;">Menge</th>
                    <th style="padding: 8px; text-align: left; border: 1px solid #ddd;">ME</th>
                </tr>
            </thead>
            <tbody>
            """);

        foreach (var item in items)
        {
            sb.Append($"""
                <tr>
                    <td style="padding: 8px; border: 1px solid #ddd;">{HtmlEncode(item.ArticleNumber)}</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">{HtmlEncode(item.ArticleDescription ?? "\u2014")}</td>
                    <td style="padding: 8px; border: 1px solid #ddd; text-align: right;">{item.Quantity:N3}</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">{HtmlEncode(item.Unit ?? "\u2014")}</td>
                </tr>
                """);
        }

        sb.Append("</tbody></table>");

        sb.Append($"""
            <p><strong>Bestellt von:</strong> {HtmlEncode(first.CreatedBy ?? "\u2014")}</p>
            <p><strong>Zeitpunkt:</strong> {first.CreatedAt:dd.MM.yyyy, HH:mm}</p>
            """);

        if (!string.IsNullOrWhiteSpace(first.Notes))
            sb.Append($"<p><strong>Bemerkung:</strong> {HtmlEncode(first.Notes)}</p>");

        sb.Append("""
            <hr style="border: 1px solid #ddd; margin-top: 30px;">
            <p style="color: #999; font-size: 12px;">IDEAL AKE WMS &mdash; Automatisch generiert</p>
            </div></body></html>
            """);

        return sb.ToString();
    }

    private static string HtmlEncode(string value) => System.Net.WebUtility.HtmlEncode(value);

    private async Task<List<RequisitionEmailRow>> LoadUnsentRequisitionsAsync(string connectionString, CancellationToken ct)
    {
        const string sql = """
            SELECT r.Id, r.ProductionOrderId, r.ArticleNumber, r.ArticleDescription,
                   r.Quantity, r.Unit, r.Priority, r.Notes, r.SentToEmails,
                   r.CreatedAt, r.CreatedBy,
                   po.OrderNumber, po.Customer, po.Description1,
                   po.ProductionDate, po.DeliveryDate
            FROM PartRequisitions r
            INNER JOIN ProductionOrders po ON r.ProductionOrderId = po.Id
            WHERE r.EmailSentAt IS NULL AND r.Status = 'Offen'
            ORDER BY r.CreatedAt
            """;

        var results = new List<RequisitionEmailRow>();
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.CommandTimeout = 30;
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            results.Add(new RequisitionEmailRow
            {
                Id = reader.GetInt32(0),
                ProductionOrderId = reader.GetInt32(1),
                ArticleNumber = reader.GetString(2),
                ArticleDescription = reader.IsDBNull(3) ? null : reader.GetString(3),
                Quantity = reader.GetDecimal(4),
                Unit = reader.IsDBNull(5) ? null : reader.GetString(5),
                Priority = reader.GetString(6),
                Notes = reader.IsDBNull(7) ? null : reader.GetString(7),
                SentToEmails = reader.IsDBNull(8) ? null : reader.GetString(8),
                CreatedAt = reader.GetDateTime(9),
                CreatedBy = reader.IsDBNull(10) ? null : reader.GetString(10),
                OrderNumber = reader.GetString(11),
                Customer = reader.IsDBNull(12) ? null : reader.GetString(12),
                OrderDescription = reader.IsDBNull(13) ? null : reader.GetString(13),
                ProductionDate = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
                DeliveryDate = reader.IsDBNull(15) ? null : reader.GetDateTime(15)
            });
        }

        return results;
    }

    private async Task MarkAsSentAsync(string connectionString, List<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        // Use parameterized query for safety
        var parameters = ids.Select((id, i) => $"@id{i}").ToList();
        var sql = $"UPDATE PartRequisitions SET EmailSentAt = GETUTCDATE() WHERE Id IN ({string.Join(",", parameters)})";

        await using var cmd = new SqlCommand(sql, conn);
        for (int i = 0; i < ids.Count; i++)
            cmd.Parameters.AddWithValue($"@id{i}", ids[i]);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private class RequisitionEmailRow
    {
        public int Id { get; set; }
        public int ProductionOrderId { get; set; }
        public string ArticleNumber { get; set; } = string.Empty;
        public string? ArticleDescription { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? SentToEmails { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? Customer { get; set; }
        public string? OrderDescription { get; set; }
        public DateTime? ProductionDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
    }
}
