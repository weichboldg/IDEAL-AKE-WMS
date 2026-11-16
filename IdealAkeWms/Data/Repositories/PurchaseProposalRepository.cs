using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>v1.99.20: ein offener WMS-Bedarf (Lagerbestellposition bzw. Bedarfsmeldung), Zuordnung ueber die Artikelnummer (kein FK).
/// v1.99.22 (Hinweis im Wareneingang zu Bestellung): <paramref name="Id"/> = Lagerbestellung- bzw. Bedarfsmeldungs-Id (Link),
/// <paramref name="Bezug"/> = Werkbank bzw. FA-Nr., <paramref name="Besteller"/> = angelegt von.
/// v1.99.30 (F4): <paramref name="ProductionOrderId"/> = FA der Bedarfsmeldung (Pflicht-FK), fuer „max je FA“; Lagerbestellungen haben keinen FA.</summary>
public sealed record OffenerBedarf(string ArticleNumber, string? Unit, decimal Menge, int Id = 0, string? Bezug = null, string? Besteller = null,
    int? ProductionOrderId = null);

/// <summary>
/// v1.99.20 (Spec 2026-11-08-einkauf-bestellvorschlaege): Datensammlung der Bestellvorschlaege, nur lesend. Gerechnet wird im
/// <c>PurchaseProposalCalculator</c>; Bestand (<c>IStockMovementRepository</c>) und offene Bestellungen (<c>ISagePurchaseOrderLineRepository</c>)
/// kommen aus ihren vorhandenen Repositories.
/// </summary>
public interface IPurchaseProposalRepository
{
    /// <summary>Nachbestellartikel (F1): <c>ReorderLevel &gt; 0</c> (= Sage-Meldebestand &gt; 0), mit <c>SageDetail</c>, ohne Tracking.</summary>
    Task<List<Article>> GetNachbestellartikelAsync();

    /// <summary>Alle Artikel-Lieferanten je ArticleId der Nachbestellartikel, eine Abfrage (kein N+1). v1.99.30 (F8): statt nur der
    /// Hauptlieferanten, damit die Lieferant-Kaskade ohne Hauptlieferant den einzigen bzw. den der juengsten Bestellung waehlen kann.</summary>
    Task<Dictionary<int, List<ArticleSupplierRow>>> GetLieferantenAsync();

    /// <summary>E3 (v1.99.21, Rueckfrage 13 a): juengste letzte Bestellung je ArticleId ueber ALLE Lieferanten der Nachbestellartikel
    /// (<c>ArticleSupplier.LetzteBestellmenge</c> gesetzt; neuestes Datum, bei Gleichstand Kreditorname), eine Abfrage.
    /// v1.99.30 (F3, Antwort 4): zweite Quelle <c>ArticleLastOrdersUnassigned</c> (Lieferant ohne Artikel-Lieferant-Zeile,
    /// <see cref="ArticleSupplierRow.OhneArtikelLieferant"/>); sie gilt nur, wenn sie JUENGER ist als alle zugeordneten.</summary>
    Task<Dictionary<int, ArticleSupplierRow>> GetLetzteBestellungenAsync();

    /// <summary>E3: true, sobald der Lauf „Letzte Bestellung (Einkauf)“ irgendeine letzte Bestellung abgelegt hat. Sonst gilt fuer alle Zeilen
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

    public async Task<Dictionary<int, List<ArticleSupplierRow>>> GetLieferantenAsync()
    {
        var rows = await (from s in _db.ArticleSuppliers.AsNoTracking()
                          join a in _db.Articles.AsNoTracking() on s.ArticleId equals a.Id
                          join c in _db.Creditors.AsNoTracking() on s.CreditorId equals c.Id
                          where a.ReorderLevel > 0
                          select new
                          {
                              s.ArticleId,
                              Row = new ArticleSupplierRow(c.Id, c.Name, c.SageKto, c.IsActive, s.SageBestellnummer, s.Mindestbestellmenge,
                                  s.Einkaufsmengeneinheit, s.Wiederbeschaffungszeit, s.IstHauptlieferant, s.UmrechnungsfaktorEK,
                                  s.LetzteBestellmenge, s.LetzteBestellungAm, s.LetzteBestellungBeleg),
                          }).ToListAsync();
        return rows.GroupBy(r => r.ArticleId).ToDictionary(g => g.Key, g => g.Select(r => r.Row).OrderBy(r => r.Name).ToList());
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
        var ohne = await (from u in _db.ArticleLastOrdersUnassigned.AsNoTracking()
                          join a in _db.Articles.AsNoTracking() on u.ArticleId equals a.Id
                          where a.ReorderLevel > 0
                          select new { u.ArticleId, u.Kto, u.Menge, u.Am, u.Beleg }).ToListAsync();
        // Name ueber Creditors.SageKto (Gross/Klein egal), sonst die Kto selbst. Eine Abfrage fuer alle Konten.
        var konten = ohne.Select(u => u.Kto).Distinct().ToList();
        var kreditoren = konten.Count == 0 ? new Dictionary<string, (int Id, string Name, bool IsActive)>()
            : (await _db.Creditors.AsNoTracking().Where(c => c.SageKto != null && konten.Contains(c.SageKto)).Select(c => new { c.Id, c.Name, c.SageKto, c.IsActive }).ToListAsync())
                .GroupBy(c => c.SageKto!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (g.First().Id, g.First().Name, g.First().IsActive), StringComparer.OrdinalIgnoreCase);
        var alle = rows.Select(r => (r.ArticleId, r.Row)).Concat(ohne.Select(u =>
        {
            var k = kreditoren.TryGetValue(u.Kto.Trim(), out var c) ? c : (Id: 0, Name: u.Kto.Trim(), IsActive: false);
            return (u.ArticleId, Row: new ArticleSupplierRow(k.Id, k.Name, u.Kto.Trim(), k.IsActive, null, null, null, null, false, null,
                u.Menge, u.Am, u.Beleg, OhneArtikelLieferant: true));
        }));
        // Juengste gewinnt; bei gleichem Datum die zugeordnete (Antwort 4: ohne Zuordnung nur, wenn juenger als alle zugeordneten).
        return alle.GroupBy(r => r.ArticleId).ToDictionary(g => g.Key,
            g => g.Select(r => r.Row).OrderByDescending(r => r.LetzteBestellungAm).ThenBy(r => r.OhneArtikelLieferant).ThenBy(r => r.Name).First());
    }

    public async Task<bool> HatLetzteBestellungenAsync() =>
        await _db.ArticleSuppliers.AnyAsync(s => s.LetzteBestellungAm != null) || await _db.ArticleLastOrdersUnassigned.AnyAsync();

    public async Task<List<OffenerBedarf>> GetOffeneLagerbestellungenAsync()
    {
        var rows = await _db.WarehouseRequisitionItems.AsNoTracking()
            .Where(i => i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Submitted
                     || (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered && i.ShortageStatus == ShortageStatus.WillBeRestocked))
            .Select(i => new
            {
                i.ArticleNumber, i.Unit, i.WarehouseRequisitionId, Werkbank = i.WarehouseRequisition.ProductionWorkplace.Name, i.WarehouseRequisition.CreatedBy,
                Menge = i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Submitted ? i.QuantityRequested : i.QuantityRequested - (i.QuantityPicked ?? 0m),
            })
            .ToListAsync();
        return rows.Where(r => r.Menge > 0)
            .Select(r => new OffenerBedarf(r.ArticleNumber, r.Unit, r.Menge, r.WarehouseRequisitionId, r.Werkbank, r.CreatedBy)).ToList();
    }

    public Task<List<OffenerBedarf>> GetOffeneBedarfsmeldungenAsync() =>
        _db.PartRequisitions.AsNoTracking().Where(p => p.Status == PartRequisitionStatus.Offen)
            .Select(p => new OffenerBedarf(p.ArticleNumber, p.Unit, p.Quantity, p.Id, p.ProductionOrder.OrderNumber, p.CreatedBy, p.ProductionOrderId)).ToListAsync();
}
