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

    public Task SendAsync(string subject, string htmlBody, IEnumerable<string> recipients, string? textBody = null, CancellationToken ct = default) =>
        SendMitOptionenAsync(new MailNachricht(subject, htmlBody, textBody, recipients.ToList()), ct);

    public async Task<bool> SendMitOptionenAsync(MailNachricht n, CancellationToken ct = default)
    {
        var recipientList = n.Empfaenger.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        if (recipientList.Count == 0)
        {
            _logger.LogWarning("Mail-Versand übersprungen: keine Empfänger konfiguriert.");
            return false;
        }

        // v1.99.10: Pickup-Ordner (DEV/QA) - Mail als .eml ablegen statt senden; hat Vorrang vor SmtpHost.
        var pickup = _configuration["MailSettings:PickupDirectory"];
        var smtpHost = _configuration["MailSettings:SmtpHost"];
        if (string.IsNullOrWhiteSpace(pickup) && string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogWarning("Mail-Versand übersprungen: SmtpHost nicht konfiguriert.");
            return false;
        }

        var senderName = _configuration["MailSettings:SenderName"] ?? "IDEAL AKE WMS";
        var fromAddress = string.IsNullOrWhiteSpace(n.Absender) ? _configuration["MailSettings:FromAddress"] ?? "wms@ake.at" : n.Absender.Trim();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, fromAddress));
        foreach (var recipient in recipientList)
            message.To.Add(MailboxAddress.Parse(recipient));
        if (!string.IsNullOrWhiteSpace(n.AntwortAn))
            message.ReplyTo.Add(MailboxAddress.Parse(n.AntwortAn.Trim()));

        message.Subject = n.Betreff;

        var bodyBuilder = new BodyBuilder { HtmlBody = n.Html };
        if (!string.IsNullOrWhiteSpace(n.Text))
            bodyBuilder.TextBody = n.Text;
        foreach (var bild in n.Bilder ?? Array.Empty<MailBild>())
        {
            var res = bodyBuilder.LinkedResources.Add(bild.ContentId, bild.Daten, ContentType.Parse(bild.MimeTyp));
            res.ContentId = bild.ContentId;
        }
        message.Body = bodyBuilder.ToMessageBody();

        if (!string.IsNullOrWhiteSpace(pickup))
        {
            Directory.CreateDirectory(pickup);
            var datei = Path.Combine(pickup, $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.eml");
            await message.WriteToAsync(datei, ct);
            _logger.LogInformation("Mail '{Subject}' an {Count} Empfänger NICHT gesendet, sondern abgelegt: {Datei}", n.Betreff, recipientList.Count, datei);
            return true;
        }

        var smtpPort = _configuration.GetValue<int>("MailSettings:SmtpPort", 25);
        var useSsl = _configuration.GetValue<bool>("MailSettings:SmtpUseSsl", false);
        var username = _configuration["MailSettings:SmtpUsername"];
        var password = _configuration["MailSettings:SmtpPassword"];

        using var client = new SmtpClient();
        try
        {
            var sslOption = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
            await client.ConnectAsync(smtpHost, smtpPort, sslOption, ct);

            if (!string.IsNullOrEmpty(username))
                await client.AuthenticateAsync(username, password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Mail '{Subject}' erfolgreich an {Count} Empfänger gesendet.", n.Betreff, recipientList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Versand der Mail '{Subject}'.", n.Betreff);
            throw;
        }
    }
}
