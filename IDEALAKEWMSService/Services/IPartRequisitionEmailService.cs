namespace IDEALAKEWMSService.Services;

public interface IPartRequisitionEmailService
{
    Task<int> SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default);
}
