using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace IDEALAKEWMSService.Services;

public class MailService : IMailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailService> _logger;

    public MailService(IConfiguration configuration, ILogger<MailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string subject, string htmlBody, IEnumerable<string> recipients, string? textBody = null, CancellationToken ct = default)
    {
        var recipientList = recipients.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        if (recipientList.Count == 0)
        {
            _logger.LogWarning("Mail-Versand übersprungen: keine Empfänger konfiguriert.");
            return;
        }

        var smtpHost = _configuration["MailSettings:SmtpHost"];
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogWarning("Mail-Versand übersprungen: SmtpHost nicht konfiguriert.");
            return;
        }

        var smtpPort = _configuration.GetValue<int>("MailSettings:SmtpPort", 25);
        var useSsl = _configuration.GetValue<bool>("MailSettings:SmtpUseSsl", false);
        var username = _configuration["MailSettings:SmtpUsername"];
        var password = _configuration["MailSettings:SmtpPassword"];
        var senderName = _configuration["MailSettings:SenderName"] ?? "IDEAL AKE WMS";
        var fromAddress = _configuration["MailSettings:FromAddress"] ?? "wms@ake.at";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, fromAddress));
        foreach (var recipient in recipientList)
            message.To.Add(MailboxAddress.Parse(recipient));

        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        if (!string.IsNullOrWhiteSpace(textBody))
            bodyBuilder.TextBody = textBody;
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var sslOption = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
            await client.ConnectAsync(smtpHost, smtpPort, sslOption, ct);

            if (!string.IsNullOrEmpty(username))
                await client.AuthenticateAsync(username, password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Mail '{Subject}' erfolgreich an {Count} Empfänger gesendet.", subject, recipientList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Versand der Mail '{Subject}'.", subject);
            throw;
        }
    }
}
