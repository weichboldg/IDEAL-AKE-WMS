using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.99.70 (Spec 2026-11-25-kommissionierauftraege-nachtrag-nachts): EINE Registrierung fuer ServiceSettings-Repository, BOM-Weiche,
/// FA-Hierarchie-Knoten und <see cref="PickingOrderWorkflow"/> – Web-App und Dienst (Lauf „Kommissionier-Nachtrag“) rufen sie auf. Der
/// BOM-Graph ist absichtlich zyklisch und nur ueber die lazy Delegates aufloesbar (BomDiResolutionTests); zwei Kopien liefen still auseinander.
/// Voraussetzung beim Aufrufer: ApplicationDbContext, IMemoryCache, IConfiguration (OseonConnection), <see cref="IProductionOrderRepository"/>,
/// <see cref="IProductionOrderPickingStatusRepository"/>, <see cref="ICurrentUserService"/>.
/// </summary>
public static class PickingOrderRegistrierung
{
    public static IServiceCollection AddKommissionierauftraege(this IServiceCollection services)
    {
        // ServiceSettings: Durchsetzungs-Decorator (FA-Hierarchie-Einwegtor, Teil 7) um das echte Repo.
        // Einziger Choke-Point fuer Schreibversuche auf die geschuetzten Keys (Master + Auto-Erledigt).
        services.AddScoped<ServiceSettingRepository>();
        services.AddScoped<IServiceSettingRepository>(sp =>
            new GuardedServiceSettingRepository(
                sp.GetRequiredService<ServiceSettingRepository>(),
                sp.GetRequiredService<IProductionOrderRepository>()));

        services.AddScoped<BomCacheRepository>();
        services.AddScoped<BomRepository>();
        services.AddScoped<CachedBomRepository>();
        services.AddScoped<FaHierarchyBomRepository>();
        // BOM-Bridge (Spec 2026-09-08): EINE Weiche fuer beide Interfaces, entscheidet pro Aufruf am Master.
        // Lazy-Delegates, weil BomRepository selbst IBomCacheRepository (= die Weiche) injiziert — eager waere ein Zyklus.
        services.AddScoped<BomRepositoryMasterSwitch>(sp => new BomRepositoryMasterSwitch(
            sp.GetRequiredService<IServiceSettingRepository>(),
            hierarchicalBom: () => sp.GetRequiredService<FaHierarchyBomRepository>(),
            hierarchicalCache: () => sp.GetRequiredService<FaHierarchyBomRepository>(),
            flatBom: () => sp.GetRequiredService<CachedBomRepository>(),
            flatCache: () => sp.GetRequiredService<BomCacheRepository>()));
        services.AddScoped<IBomRepository>(sp => sp.GetRequiredService<BomRepositoryMasterSwitch>());
        services.AddScoped<IBomCacheRepository>(sp => sp.GetRequiredService<BomRepositoryMasterSwitch>());

        // FA-Hierarchie (IDEAL): Repo + Cache-Decorator (ADR 0001, 5-min IMemoryCache analog BOM-Cache)
        services.AddScoped<FaHierarchyNodeRepository>();
        services.AddScoped<IFaHierarchyNodeRepository, CachedFaHierarchyNodeRepository>();
        services.AddScoped<FaHierarchyTreeBuilder>();

        services.AddScoped<IPickingAreaRepository, PickingAreaRepository>(); // v1.88.0 Kommissionierbereiche (ungecacht)
        services.AddScoped<IPickingOrderRepository, PickingOrderRepository>(); // v1.88.0 Kommissionierauftraege (ungecacht)
        services.AddScoped<PickingOrderWorkflow>();
        return services;
    }
}
