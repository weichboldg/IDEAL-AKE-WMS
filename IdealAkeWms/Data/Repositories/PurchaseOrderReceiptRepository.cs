using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>v1.84.0 (Spec 2026-11-08-wareneingang-aus-sage-bestellungen E3): Lesen der Wareneingaenge zu Bestellpositionen. Geschrieben
/// werden sie nur als Navigation an der <c>StockMovement</c> ueber <c>IStockMovementRepository.AddAsync</c> (Sage-Queue-Decorator).</summary>
public interface IPurchaseOrderReceiptRepository
{
    /// <summary>Summe der im WMS erfassten Menge je <c>BelPosId</c> (eine Abfrage fuer alle sichtbaren Zeilen, kein N+1).</summary>
    Task<Dictionary<int, decimal>> GetErfasstJeBelPosAsync(IReadOnlyCollection<int> belPosIds);
}

public class PurchaseOrderReceiptRepository : IPurchaseOrderReceiptRepository
{
    private readonly ApplicationDbContext _db;

    public PurchaseOrderReceiptRepository(ApplicationDbContext db) => _db = db;

    public async Task<Dictionary<int, decimal>> GetErfasstJeBelPosAsync(IReadOnlyCollection<int> belPosIds)
    {
        if (belPosIds.Count == 0) return new Dictionary<int, decimal>();
        return await _db.PurchaseOrderReceipts.AsNoTracking().Where(r => belPosIds.Contains(r.BelPosId))
            .GroupBy(r => r.BelPosId).Select(g => new { g.Key, Summe = g.Sum(r => r.Quantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Summe);
    }
}
