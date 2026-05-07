using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Tests.Helpers;

public class FakeSageBestandReader : ISageBestandReader
{
    public List<SageBestandDto> Records { get; set; } = new();
    public Exception? ThrowOnRead { get; set; }

    public Task<List<SageBestandDto>> GetAllAsync(CancellationToken ct = default)
    {
        if (ThrowOnRead != null)
            throw ThrowOnRead;
        return Task.FromResult(Records);
    }
}
