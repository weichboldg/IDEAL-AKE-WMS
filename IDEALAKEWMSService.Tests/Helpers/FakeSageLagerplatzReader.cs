using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Tests.Helpers;

public class FakeSageLagerplatzReader : ISageLagerplatzReader
{
    public List<SageLagerplatzDto> Records { get; set; } = new();
    public Func<List<SageLagerplatzDto>>? RecordsFactory { get; set; }
    public Exception? ThrowOnRead { get; set; }

    public Task<List<SageLagerplatzDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        if (ThrowOnRead != null)
            throw ThrowOnRead;
        return Task.FromResult(RecordsFactory?.Invoke() ?? Records);
    }
}
