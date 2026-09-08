using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services.HierarchischeStruktur;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// BOM-Bridge-Weiche (Spec Design A): entscheidet PRO AUFRUF am Master <c>ProduktionsauftragHierarchisch</c>,
/// ob die FA-Hierarchie (<see cref="FaHierarchyBomRepository"/>) oder der unveraenderte AKE-Pfad
/// (<see cref="CachedBomRepository"/> → <see cref="BomRepository"/> → <see cref="BomCacheRepository"/>) bedient.
/// Deckt BEIDE Interfaces ab — auch die bisher ungeschuetzte <see cref="IBomCacheRepository"/>-Registrierung.
/// Die Ziele werden per Delegate LAZY aufgeloest: <see cref="BomRepository"/> haengt selbst an
/// <see cref="IBomCacheRepository"/> (= diese Weiche); eine eager Konstruktion waere ein DI-Zyklus.
/// Nachfolger des frueheren Minimal-Guard-Decorators aus Spec 2026-08-18 (mit dieser Weiche entfernt).
/// </summary>
public sealed class BomRepositoryMasterSwitch : IBomRepository, IBomCacheRepository
{
    private readonly IServiceSettingRepository _settings;
    private readonly Func<IBomRepository> _hierarchicalBom;
    private readonly Func<IBomCacheRepository> _hierarchicalCache;
    private readonly Func<IBomRepository> _flatBom;
    private readonly Func<IBomCacheRepository> _flatCache;

    public BomRepositoryMasterSwitch(
        IServiceSettingRepository settings,
        Func<IBomRepository> hierarchicalBom,
        Func<IBomCacheRepository> hierarchicalCache,
        Func<IBomRepository> flatBom,
        Func<IBomCacheRepository> flatCache)
    {
        _settings = settings;
        _hierarchicalBom = hierarchicalBom;
        _hierarchicalCache = hierarchicalCache;
        _flatBom = flatBom;
        _flatCache = flatCache;
    }

    private async Task<bool> IsHierarchicalAsync()
    {
        var master = await _settings.GetValueAsync(HierarchischeStrukturKeys.Master);
        return string.Equals(master, "true", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IBomRepository> BomAsync() => await IsHierarchicalAsync() ? _hierarchicalBom() : _flatBom();
    private async Task<IBomCacheRepository> CacheAsync() => await IsHierarchicalAsync() ? _hierarchicalCache() : _flatCache();

    public async Task<BomQueryResult> GetBomItemsAsync(BomKey key, BomScope scope = BomScope.DirectChildren)
        => await (await BomAsync()).GetBomItemsAsync(key, scope);

    public async Task<BomQueryResult?> GetByArticleNumberAsync(BomKey key)
        => await (await CacheAsync()).GetByArticleNumberAsync(key);

    public async Task<HashSet<string>> GetArticleNumbersWithCoatingPartsAsync(
        string lackierteilCategoryName,
        List<string> articleNumbers)
        => await (await CacheAsync()).GetArticleNumbersWithCoatingPartsAsync(lackierteilCategoryName, articleNumbers);

    public async Task<Dictionary<string, (string Hash, DateTime CachedAt)>> GetHeaderHashesAsync(
        List<string> articleNumbers)
        => await (await CacheAsync()).GetHeaderHashesAsync(articleNumbers);

    public async Task UpsertBomAsync(
        string articleNumber,
        string source,
        string contentHash,
        List<BomItem> items)
        => await (await CacheAsync()).UpsertBomAsync(articleNumber, source, contentHash, items);

    public async Task<List<string>> GetDeviceArticleNumbersByComponentAsync(string componentArticleNumber)
        => await (await CacheAsync()).GetDeviceArticleNumbersByComponentAsync(componentArticleNumber);

    public async Task<Dictionary<string, decimal>> GetComponentMengePerDeviceAsync(string componentArticleNumber)
        => await (await CacheAsync()).GetComponentMengePerDeviceAsync(componentArticleNumber);

    public async Task DeleteOrphansAsync(List<string> currentArticleNumbers)
        => await (await CacheAsync()).DeleteOrphansAsync(currentArticleNumbers);
}
