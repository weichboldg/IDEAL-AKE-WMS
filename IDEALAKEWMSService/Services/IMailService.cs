namespace IDEALAKEWMSService.Services;

public interface IMailService
{
    Task SendAsync(string subject, string htmlBody, IEnumerable<string> recipients, CancellationToken ct = default);
}
