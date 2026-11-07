using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// v1.84.0 (Spec 2026-11-08-wareneingang-aus-sage-bestellungen E2/E3): Lesezugriff auf den Cache offener Sage-Bestellpositionen.
/// Einzige Lese-Schnittstelle fuer offene Sage-Bestellungen im WMS (auch fuer das spaetere Einkaufs-Epic). Kein Caching-Decorator
/// (der Cache ist die Tabelle). Schreibt nur der Dienst-Abgleich.
/// </summary>
public interface ISagePurchaseOrderLineRepository
{
    /// <summary>Alle Positionen (ca. 1.000 am AKE-Test), ohne Tracking.</summary>
    Task<List<SagePurchaseOrderLine>> GetAllAsync();
    Task<List<SagePurchaseOrderLine>> GetByBelIdAsync(int belId);
    /// <summary>Distinct <c>BelId</c> zu einer Belegnummer (exakt, getrimmt) – mehrere = sichtbare Meldung beim Aufrufer.</summary>
    Task<List<int>> GetBelIdsByBelegnummerAsync(string belegnummer);
    /// <summary>Zeitpunkt des letzten erfolgreichen Abgleichs (max <c>SyncedAt</c>); null = Cache leer.</summary>
    Task<DateTime?> GetStandAsync();
}

public class SagePurchaseOrderLineRepository : ISagePurchaseOrderLineRepository
{
    private readonly ApplicationDbContext _db;

    public SagePurchaseOrderLineRepository(ApplicationDbContext db) => _db = db;

    public Task<List<SagePurchaseOrderLine>> GetAllAsync() => _db.SagePurchaseOrderLines.AsNoTracking().ToListAsync();

    public Task<List<SagePurchaseOrderLine>> GetByBelIdAsync(int belId) =>
        _db.SagePurchaseOrderLines.AsNoTracking().Where(l => l.BelId == belId).OrderBy(l => l.BelPosId).ToListAsync();

    public Task<List<int>> GetBelIdsByBelegnummerAsync(string belegnummer)
    {
        var key = belegnummer.Trim();
        return _db.SagePurchaseOrderLines.AsNoTracking().Where(l => l.Belegnummer == key).Select(l => l.BelId).Distinct().ToListAsync();
    }

    public async Task<DateTime?> GetStandAsync() =>
        await _db.SagePurchaseOrderLines.AsNoTracking().Select(l => (DateTime?)l.SyncedAt).MaxAsync();
}
