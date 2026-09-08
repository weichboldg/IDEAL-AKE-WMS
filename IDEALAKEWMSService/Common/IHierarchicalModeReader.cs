using Microsoft.Extensions.Configuration;

namespace IDEALAKEWMSService.Common;

/// <summary>
/// Master-Read <c>ProduktionsauftragHierarchisch</c> fuer die Klasse-D-Gates (BOM-Bridge, Spec Design H).
/// Kapselt <see cref="ServiceSettings.GetBoolSafeAsync"/> (DB-first, ADR 0008), damit die Gates ohne
/// SQL Server testbar sind (Tests injizieren einen Stub). Safe-Reader: DB-Hickup → false (= AKE-Verhalten).
/// </summary>
public interface IHierarchicalModeReader
{
    Task<bool> IsHierarchicalAsync(CancellationToken ct);
}

public sealed class HierarchicalModeReader : IHierarchicalModeReader
{
    private readonly IConfiguration _configuration;
    public HierarchicalModeReader(IConfiguration configuration) => _configuration = configuration;

    public Task<bool> IsHierarchicalAsync(CancellationToken ct) =>
        ServiceSettings.GetBoolSafeAsync(_configuration, "ProduktionsauftragHierarchisch", false, ct);
}
