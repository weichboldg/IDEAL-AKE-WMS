using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Tests.Helpers;

public class FakeSageZusatzinfoReader : ISageZusatzinfoReader
{
    public bool ViewExists { get; set; } = true;
    public List<SageZusatzinfoRow> Rows { get; set; } = new();
    public Exception? ThrowOnRead { get; set; }

    public Task<SageZusatzinfoReadResult> ReadAsync(CancellationToken ct = default)
    {
        if (ThrowOnRead != null)
            throw ThrowOnRead;
        return Task.FromResult(new SageZusatzinfoReadResult(ViewExists, Rows));
    }
}
