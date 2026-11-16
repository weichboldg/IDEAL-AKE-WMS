using IdealAkeWms.Services.Beschichtung;
using IDEALAKEWMSService.Common;

namespace IDEALAKEWMSService.Tests.Helpers;

public sealed class StubHierarchicalModeReader : IHierarchicalModeReader
{
    private readonly bool _value;
    private readonly string? _kennzeichenQuelle;
    /// <param name="kennzeichenQuelle">Rohwert AppSetting BeschichtungKennzeichenQuelle (null = Zeile fehlt = Automatisch).</param>
    public StubHierarchicalModeReader(bool value, string? kennzeichenQuelle = null)
    {
        _value = value;
        _kennzeichenQuelle = kennzeichenQuelle;
    }
    public Task<bool> IsHierarchicalAsync(CancellationToken ct) => Task.FromResult(_value);
    public Task<BeschichtungQuellen> GetBeschichtungQuellenAsync(CancellationToken ct) =>
        Task.FromResult(BeschichtungResolver.ResolveQuellen(_kennzeichenQuelle, null, _value));
}
