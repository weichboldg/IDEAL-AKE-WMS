using System.Globalization;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>v1.88.0: Kommissionierauftrag je FA fuer Umbuchungs-Tests (ersetzt die frueheren PickingItem-Fixtures).</summary>
public static class KommissionierTestHelper
{
    public static PickingOrder Auftrag(ApplicationDbContext ctx, int faId)
    {
        var auftrag = ctx.PickingOrders.Local.FirstOrDefault(o => o.ProductionOrderId == faId)
                      ?? ctx.PickingOrders.FirstOrDefault(o => o.ProductionOrderId == faId);
        if (auftrag != null) return auftrag;
        var area = ctx.PickingAreas.Local.FirstOrDefault() ?? ctx.PickingAreas.FirstOrDefault();
        if (area == null)
        {
            area = new PickingArea { Name = "Lager", Kuerzel = "L", IsActive = true, IsDefault = true, CreatedBy = "t", CreatedByWindows = "t" };
            ctx.PickingAreas.Add(area);
        }
        var lfd = ctx.PickingOrders.Count() + 1;
        auftrag = new PickingOrder
        {
            Nummer = $"KO-2026-{lfd:D5}", Jahr = 2026, LaufendeNummer = lfd, ProductionOrderId = faId, PickingArea = area,
            Status = PickingOrderStatus.Offen, CreatedBy = "t", CreatedByWindows = "t",
        };
        ctx.PickingOrders.Add(auftrag);
        ctx.SaveChanges();
        return auftrag;
    }

    public static PickingOrderLine AddZeile(ApplicationDbContext ctx, int faId, string articleNumber, decimal menge, int? sourceId,
        PickingOrderLineStatus status = PickingOrderLineStatus.Gepickt, string? position = null)
    {
        var auftrag = Auftrag(ctx, faId);
        var zeile = new PickingOrderLine
        {
            PickingOrderId = auftrag.Id, ProductionOrderId = faId, ArticleNumber = articleNumber, BomPosition = position,
            SollMenge = menge, IstMenge = status == PickingOrderLineStatus.Gepickt ? menge : null, SourceStorageLocationId = sourceId,
            Status = status, CreatedBy = "t", CreatedByWindows = "t",
        };
        ctx.PickingOrderLines.Add(zeile);
        ctx.SaveChanges();
        return zeile;
    }

    /// <summary>Alle gepickten Zeilen des FA-Auftrags umbuchen (Ist = gespeicherte Ist-Menge).</summary>
    public static async Task<int> UmbuchenAsync(ApplicationDbContext ctx, PickingTransferService svc, int faId, int targetId,
        string user = "TestUser", string win = "TEST\\u", bool force = false)
    {
        var auftrag = Auftrag(ctx, faId);
        var items = ctx.PickingOrderLines.Where(z => z.PickingOrderId == auftrag.Id && z.Status == PickingOrderLineStatus.Gepickt).ToList()
            .Select(z => new PickingSelectionItem
            {
                PickingItemId = z.Id, SourceStorageLocationId = z.SourceStorageLocationId,
                IstMenge = z.IstMenge?.ToString(CultureInfo.InvariantCulture),
            }).ToList();
        return (await svc.TransferOrderLinesAsync(auftrag.Id, targetId, force, items, null, user, win)).TransferredCount;
    }
}
