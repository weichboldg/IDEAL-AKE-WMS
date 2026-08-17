using IdealAkeWms.Models;
using IdealAkeWms.Services.HierarchischeStruktur;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// Durchsetzungs-Decorator: einziger Choke-Point für Schreibversuche auf die
/// geschützten FA-Hierarchie-Keys (Master + Auto-Erledigt). Alle vier
/// ServiceSettings-Schreibwege (SaveSettings/Create/Edit/Delete) laufen durch
/// <c>UpsertAsync</c>/<c>DeleteAsync</c> — hier greift der Wächter live (ADR 0001,
/// Muster <c>CachedSettingRepository</c>). Reine Lese-Methoden reichen durch.
/// </summary>
public sealed class GuardedServiceSettingRepository : IServiceSettingRepository
{
    private readonly IServiceSettingRepository _inner;
    private readonly IProductionOrderRepository _productionOrders;

    public GuardedServiceSettingRepository(
        IServiceSettingRepository inner,
        IProductionOrderRepository productionOrders)
    {
        _inner = inner;
        _productionOrders = productionOrders;
    }

    public Task<List<ServiceSetting>> GetAllAsync() => _inner.GetAllAsync();
    public Task<List<ServiceSetting>> GetByCategoryAsync(string category) => _inner.GetByCategoryAsync(category);
    public Task<string?> GetValueAsync(string key) => _inner.GetValueAsync(key);

    public async Task UpsertAsync(string key, string value, string? category = null, string? description = null)
    {
        if (HierarchischeStrukturKeys.IsGuarded(key))
            await EnsureAllowedAsync(key, ParseBool(value));
        await _inner.UpsertAsync(key, value, category, description);
    }

    public async Task DeleteAsync(string key)
    {
        // Löschen eines geschützten Keys = Rückfall auf Default (false) = Deaktivieren.
        if (HierarchischeStrukturKeys.IsGuarded(key))
            await EnsureAllowedAsync(key, requestedValue: false);
        await _inner.DeleteAsync(key);
    }

    private async Task EnsureAllowedAsync(string key, bool requestedValue)
    {
        var current = ParseBool(await _inner.GetValueAsync(key));
        var dataExists = await _productionOrders.HierarchicalDataExistsAsync();
        var decision = HierarchischeStrukturGuard.Evaluate(
            HierarchischeStrukturKeys.Map(key), current, requestedValue, dataExists);
        if (!decision.Allowed)
            throw new HierarchischeStrukturGuardException(decision.RejectionReason!);
    }

    private static bool ParseBool(string? v) =>
        string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(v, "on", StringComparison.OrdinalIgnoreCase)
        || v == "1";
}
