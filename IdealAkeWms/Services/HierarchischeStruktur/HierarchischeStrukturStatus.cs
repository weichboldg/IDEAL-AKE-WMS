using IdealAkeWms.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace IdealAkeWms.Services.HierarchischeStruktur;

/// <summary>
/// Gecachter Anzeige-Zustand der Einwegtor-Sperrbedingung (Freigabe-Antwort 3):
/// die Anzeige liest hier gecacht, der Schreibpfad (<c>GuardedServiceSettingRepository</c>)
/// prüft live. Refresh am Start, nach jedem Master-Flip und (ab Etappe C) am Ende des
/// Materialisierungs-Syncs — bewusst kein Timer.
/// </summary>
public sealed class HierarchischeStrukturStatus
{
    private readonly IServiceScopeFactory _scopeFactory;
    private volatile bool _dataExists;

    public HierarchischeStrukturStatus(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public bool HierarchicalDataExists => _dataExists;
    public DateTimeOffset? LastRefreshedUtc { get; private set; }

    public async Task RefreshAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProductionOrderRepository>();
        _dataExists = await repo.HierarchicalDataExistsAsync();
        LastRefreshedUtc = DateTimeOffset.UtcNow;
    }
}
