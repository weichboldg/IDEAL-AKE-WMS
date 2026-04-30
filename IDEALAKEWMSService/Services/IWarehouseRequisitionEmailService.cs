namespace IDEALAKEWMSService.Services;

public interface IWarehouseRequisitionEmailService
{
    Task<EmailResult> SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default);
}

public record EmailResult(int SubmitsSent, int CancellationsSent, List<string> Errors);
