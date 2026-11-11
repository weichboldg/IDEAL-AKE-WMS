using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// v1.99.4 (Wareneingang Teil 2, F5): im WMS erfasste Menge einer Bestellposition plus die an Sage uebergebenen Teilmengen (Zeitpunkt der
/// Uebergabe). <see cref="Abzuziehen"/> ist die EINE Stelle der Rest-Regel: abgezogen wird alles, was zum Abgleich-Stand noch nicht in Sage
/// enthalten war - alles ausser Uebergeben mit SageWeAm &lt;= SyncedAt (sonst zoege das WMS eine Uebergabe zweimal ab).
/// </summary>
public sealed record ErfasstJeBelPos(decimal Gesamt, IReadOnlyList<(DateTime Am, decimal Menge)> Uebergeben)
{
    public static readonly ErfasstJeBelPos Leer = new(0m, Array.Empty<(DateTime, decimal)>());
    public decimal Abzuziehen(DateTime syncedAt) => Gesamt - Uebergeben.Where(u => u.Am <= syncedAt).Sum(u => u.Menge);
}

/// <summary>v1.84.0 (Spec 2026-11-08-wareneingang-aus-sage-bestellungen E3): Lesen der Wareneingaenge zu Bestellpositionen. Geschrieben
/// werden sie nur als Navigation an der <c>StockMovement</c> ueber <c>IStockMovementRepository.AddAsync</c> (Sage-Queue-Decorator).</summary>
public interface IPurchaseOrderReceiptRepository
{
    /// <summary>Im WMS erfasst je <c>BelPosId</c>, mit den an Sage uebergebenen Teilmengen (eine Abfrage fuer alle sichtbaren Zeilen, kein N+1).</summary>
    Task<Dictionary<int, ErfasstJeBelPos>> GetErfasstJeBelPosAsync(IReadOnlyCollection<int> belPosIds);
}

public class PurchaseOrderReceiptRepository : IPurchaseOrderReceiptRepository
{
    private readonly ApplicationDbContext _db;

    public PurchaseOrderReceiptRepository(ApplicationDbContext db) => _db = db;

    public async Task<Dictionary<int, ErfasstJeBelPos>> GetErfasstJeBelPosAsync(IReadOnlyCollection<int> belPosIds)
    {
        if (belPosIds.Count == 0) return new Dictionary<int, ErfasstJeBelPos>();
        var zeilen = await _db.PurchaseOrderReceipts.AsNoTracking().Where(r => belPosIds.Contains(r.BelPosId))
            .Select(r => new { r.BelPosId, r.Quantity, r.SageWeStatus, r.SageWeAm }).ToListAsync();
        return zeilen.GroupBy(r => r.BelPosId).ToDictionary(g => g.Key, g => new ErfasstJeBelPos(g.Sum(r => r.Quantity),
            g.Where(r => r.SageWeStatus == SageWareneingangStatus.Uebergeben && r.SageWeAm != null).Select(r => (r.SageWeAm!.Value, r.Quantity)).ToList()));
    }
}
