using System.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>v1.88.0: siehe <see cref="IPickingOrderRepository"/>.</summary>
public class PickingOrderRepository : IPickingOrderRepository
{
    private readonly ApplicationDbContext _db;

    public PickingOrderRepository(ApplicationDbContext db) => _db = db;

    public async Task<int> GetMaxLaufendeNummerAsync(int jahr) =>
        await _db.PickingOrders.Where(o => o.Jahr == jahr).MaxAsync(o => (int?)o.LaufendeNummer) ?? 0;

    public Task<List<PickingOrderLine>> GetAktiveZeilenAsync(int productionOrderId) =>
        _db.PickingOrderLines.Where(z => z.ProductionOrderId == productionOrderId && z.PickingOrder.Status != PickingOrderStatus.Storniert)
            .ToListAsync();

    public async Task<Dictionary<string, (string? Lagerort, string? Artikelgruppe)>> GetArtikelBereichsdatenAsync(IReadOnlyCollection<string> articleNumbers)
    {
        if (articleNumbers.Count == 0) return new(StringComparer.OrdinalIgnoreCase);
        var rows = await _db.Articles.Where(a => articleNumbers.Contains(a.ArticleNumber))
            .Select(a => new
            {
                a.ArticleNumber,
                a.ArticleGroup,
                Lagerort = a.PrimaryStorageLocation != null && a.PrimaryStorageLocation.StorageArea != null
                    ? a.PrimaryStorageLocation.StorageArea.Lagerkennung : null,
            })
            .ToListAsync();
        return rows.GroupBy(r => r.ArticleNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (g.First().Lagerort, g.First().ArticleGroup), StringComparer.OrdinalIgnoreCase);
    }

    public Task<List<PickingOrder>> GetByProductionOrderAsync(int productionOrderId) =>
        _db.PickingOrders.Include(o => o.PickingArea).Where(o => o.ProductionOrderId == productionOrderId).OrderBy(o => o.Id).ToListAsync();

    public async Task<Dictionary<int, List<PickingOrder>>> GetByProductionOrdersAsync(IReadOnlyCollection<int> productionOrderIds)
    {
        if (productionOrderIds.Count == 0) return new();
        var orders = await _db.PickingOrders.Include(o => o.PickingArea)
            .Where(o => o.ProductionOrderId != null && productionOrderIds.Contains(o.ProductionOrderId.Value)).OrderBy(o => o.Id).ToListAsync();
        return orders.GroupBy(o => o.ProductionOrderId!.Value).ToDictionary(g => g.Key, g => g.ToList());
    }

    public Task<List<PickingOrder>> GetListeAsync(bool nurOffene)
    {
        var q = _db.PickingOrders
            .Include(o => o.PickingArea)
            .Include(o => o.ProductionOrder!).ThenInclude(p => p.ProductionWorkplace)
            .Include(o => o.ProductionOrder!).ThenInclude(p => p.PickingStatus)
            .AsQueryable();
        if (nurOffene)
            q = q.Where(o => (o.Status == PickingOrderStatus.Offen || o.Status == PickingOrderStatus.InArbeit)
                          && o.ProductionOrder != null && !o.ProductionOrder.IsDone && !o.ProductionOrder.IsCancelled
                          && (o.ProductionOrder.PickingStatus == null || !o.ProductionOrder.PickingStatus.IsDonePicking));
        return q.OrderBy(o => o.PickingPriority.HasValue ? 0 : 1).ThenBy(o => o.PickingPriority).ThenBy(o => o.Id).ToListAsync();
    }

    public Task<PickingOrder?> GetMitZeilenAsync(int id) =>
        _db.PickingOrders.Include(o => o.Zeilen).Include(o => o.PickingArea).Include(o => o.ProductionOrder)
            .FirstOrDefaultAsync(o => o.Id == id);

    public void Add(PickingOrder order) => _db.PickingOrders.Add(order);
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
    public void VerwerfeAenderungen() => _db.ChangeTracker.Clear();

    public async Task<T> InSerializableTransactionAsync<T>(Func<Task<T>> action)
    {
        if (!_db.Database.IsRelational()) return await action();
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var result = await action();
        await tx.CommitAsync();
        return result;
    }
}
