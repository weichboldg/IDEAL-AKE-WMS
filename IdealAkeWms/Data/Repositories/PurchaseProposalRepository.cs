using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>v1.99.20: ein offener WMS-Bedarf (Lagerbestellposition bzw. Bedarfsmeldung), Zuordnung ueber die Artikelnummer (kein FK).</summary>
public sealed record OffenerBedarf(string ArticleNumber, string? Unit, decimal Menge);

/// <summary>
/// v1.99.20 (Spec 2026-11-08-einkauf-bestellvorschlaege): Datensammlung der Bestellvorschlaege, nur lesend. Gerechnet wird im
/// <c>PurchaseProposalCalculator</c>; Bestand (<c>IStockMovementRepository</c>) und offene Bestellungen (<c>ISagePurchaseOrderLineRepository</c>)
/// kommen aus ihren vorhandenen Repositories.
/// </summary>
public interface IPurchaseProposalRepository
{
    /// <summary>Nachbestellartikel (F1): <c>ReorderLevel &gt; 0</c> (= Sage-Meldebestand &gt; 0), mit <c>SageDetail</c>, ohne Tracking.</summary>
    Task<List<Article>> GetNachbestellartikelAsync();

    /// <summary>Hauptlieferant je ArticleId (F4) der Nachbestellartikel, eine Abfrage (kein N+1).</summary>
    Task<Dictionary<int, ArticleSupplierRow>> GetHauptlieferantenAsync();

    /// <summary>E3 (v1.99.21, Rueckfrage 13 a): juengste letzte Bestellung je ArticleId ueber ALLE Lieferanten der Nachbestellartikel
    /// (<c>ArticleSupplier.LetzteBestellmenge</c> gesetzt; neuestes Datum, bei Gleichstand Kreditorname), eine Abfrage.</summary>
    Task<Dictionary<int, ArticleSupplierRow>> GetLetzteBestellungenAsync();

    /// <summary>E3: true, sobald der Lauf „Artikel-Lieferanten“ irgendeine letzte Bestellung abgelegt hat. Sonst gilt fuer alle Zeilen
    /// Rueckfrage 13 b (kein Fallback „keine Bestellung in 3 Jahren“, das waere ohne gelesene Daten falsch) und die Liste zeigt einen Banner.</summary>
    Task<bool> HatLetzteBestellungenAsync();

    /// <summary>Offene Lagerbestellungen: Submitted → angefordert; PartiallyDelivered → nur Positionen „wird nachgeliefert“ mit Rest &gt; 0.</summary>
    Task<List<OffenerBedarf>> GetOffeneLagerbestellungenAsync();

    /// <summary>Offene Bedarfsmeldungen (Status Offen).</summary>
    Task<List<OffenerBedarf>> GetOffeneBedarfsmeldungenAsync();
}

public class PurchaseProposalRepository : IPurchaseProposalRepository
{
    private readonly ApplicationDbContext _db;

    public PurchaseProposalRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Article>> GetNachbestellartikelAsync() =>
        _db.Articles.AsNoTracking().Include(a => a.SageDetail).Where(a => a.ReorderLevel > 0).ToListAsync();

    public async Task<Dictionary<int, ArticleSupplierRow>> GetHauptlieferantenAsync()
    {
        var rows = await (from s in _db.ArticleSuppliers.AsNoTracking()
                          join a in _db.Articles.AsNoTracking() on s.ArticleId equals a.Id
                          join c in _db.Creditors.AsNoTracking() on s.CreditorId equals c.Id
                          where s.IstHauptlieferant && a.ReorderLevel > 0
                          select new
                          {
                              s.ArticleId,
                              Row = new ArticleSupplierRow(c.Id, c.Name, c.SageKto, c.IsActive, s.SageBestellnummer, s.Mindestbestellmenge,
                                  s.Einkaufsmengeneinheit, s.Wiederbeschaffungszeit, s.IstHauptlieferant, s.UmrechnungsfaktorEK,
                                  s.LetzteBestellmenge, s.LetzteBestellungAm, s.LetzteBestellungBeleg),
                          }).ToListAsync();
        // Mehrere Hauptlieferanten je Artikel kommen laut Abgleich nicht vor; falls doch, gewinnt der erste nach Name (stabil).
        return rows.GroupBy(r => r.ArticleId).ToDictionary(g => g.Key, g => g.Select(r => r.Row).OrderBy(r => r.Name).First());
    }

    public async Task<Dictionary<int, ArticleSupplierRow>> GetLetzteBestellungenAsync()
    {
        var rows = await (from s in _db.ArticleSuppliers.AsNoTracking()
                          join a in _db.Articles.AsNoTracking() on s.ArticleId equals a.Id
                          join c in _db.Creditors.AsNoTracking() on s.CreditorId equals c.Id
                          where s.LetzteBestellmenge != null && a.ReorderLevel > 0
                          select new
                          {
                              s.ArticleId,
                              Row = new ArticleSupplierRow(c.Id, c.Name, c.SageKto, c.IsActive, s.SageBestellnummer, s.Mindestbestellmenge,
                                  s.Einkaufsmengeneinheit, s.Wiederbeschaffungszeit, s.IstHauptlieferant, s.UmrechnungsfaktorEK,
                                  s.LetzteBestellmenge, s.LetzteBestellungAm, s.LetzteBestellungBeleg),
                          }).ToListAsync();
        return rows.GroupBy(r => r.ArticleId).ToDictionary(g => g.Key,
            g => g.Select(r => r.Row).OrderByDescending(r => r.LetzteBestellungAm).ThenBy(r => r.Name).First());
    }

    public Task<bool> HatLetzteBestellungenAsync() => _db.ArticleSuppliers.AnyAsync(s => s.LetzteBestellungAm != null);

    public async Task<List<OffenerBedarf>> GetOffeneLagerbestellungenAsync()
    {
        var rows = await _db.WarehouseRequisitionItems.AsNoTracking()
            .Where(i => i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Submitted
                     || (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered && i.ShortageStatus == ShortageStatus.WillBeRestocked))
            .Select(i => new
            {
                i.ArticleNumber, i.Unit,
                Menge = i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Submitted ? i.QuantityRequested : i.QuantityRequested - (i.QuantityPicked ?? 0m),
            })
            .ToListAsync();
        return rows.Where(r => r.Menge > 0).Select(r => new OffenerBedarf(r.ArticleNumber, r.Unit, r.Menge)).ToList();
    }

    public Task<List<OffenerBedarf>> GetOffeneBedarfsmeldungenAsync() =>
        _db.PartRequisitions.AsNoTracking().Where(p => p.Status == PartRequisitionStatus.Offen)
            .Select(p => new OffenerBedarf(p.ArticleNumber, p.Unit, p.Quantity)).ToListAsync();
}
