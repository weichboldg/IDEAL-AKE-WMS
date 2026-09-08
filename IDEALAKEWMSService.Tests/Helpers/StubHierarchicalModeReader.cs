using IDEALAKEWMSService.Common;

namespace IDEALAKEWMSService.Tests.Helpers;

public sealed class StubHierarchicalModeReader : IHierarchicalModeReader
{
    private readonly bool _value;
    public StubHierarchicalModeReader(bool value) => _value = value;
    public Task<bool> IsHierarchicalAsync(CancellationToken ct) => Task.FromResult(_value);
}
