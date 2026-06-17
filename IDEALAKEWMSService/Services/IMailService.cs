namespace IDEALAKEWMSService.Services;

public interface IMailService
{
    Task SendAsync(string subject, string htmlBody, IEnumerable<string> recipients, string? textBody = null, CancellationToken ct = default);
}
