using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class PickingRepository : IPickingRepository
{
    private readonly ApplicationDbContext _context;

    public PickingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PickingItem>> GetByProductionOrderAsync(int productionOrderId)
    {
        return await _context.PickingItems
            .Include(p => p.SourceStorageLocation)
            .Where(p => p.ProductionOrderId == productionOrderId)
            .OrderBy(p => p.BomPosition)
            .ToListAsync();
    }

    public async Task InitializePickingAsync(int productionOrderId, List<BomItem> bomItems,
        string createdBy, string createdByWindows)
    {
        var existing = await _context.PickingItems
            .AnyAsync(p => p.ProductionOrderId == productionOrderId);

        if (existing) return;

        var now = DateTime.Now;
        foreach (var bom in bomItems)
        {
            _context.PickingItems.Add(new PickingItem
            {
                ProductionOrderId = productionOrderId,
                BomArticleNumber = bom.Ressourcenummer ?? bom.Artikelnummer,
                BomPosition = bom.Position,
                Quantity = bom.Menge,
                CreatedAt = now,
                CreatedBy = createdBy,
                CreatedByWindows = createdByWindows
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<PickingItem?> GetByIdAsync(int pickingItemId)
    {
        return await _context.PickingItems.FindAsync(pickingItemId);
    }

    public async Task TogglePickedAsync(int pickingItemId, int? storageLocationId,
        string userName, string windowsUser, bool isBaugruppe = false)
    {
        var item = await _context.PickingItems.FindAsync(pickingItemId);
        if (item == null) return;

        item.IsPicked = !item.IsPicked;
        item.IsBaugruppe = isBaugruppe;
        if (item.IsPicked)
        {
            item.PickedAt = DateTime.Now;
            item.PickedBy = userName;
            item.PickedByWindows = windowsUser;
            item.SourceStorageLocationId = storageLocationId;
        }
        else
        {
            item.PickedAt = null;
            item.PickedBy = null;
            item.PickedByWindows = null;
            item.SourceStorageLocationId = null;
        }

        item.ModifiedAt = DateTime.Now;
        item.ModifiedBy = userName;
        item.ModifiedByWindows = windowsUser;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "Der Datensatz wurde zwischenzeitlich von einem anderen Benutzer geändert. Bitte Seite neu laden.");
        }
    }

    public async Task<List<PickingItem>> GetPickedNotTransferredAsync(int productionOrderId)
    {
        return await _context.PickingItems
            .Where(p => p.ProductionOrderId == productionOrderId &&
                        p.IsPicked && !p.IsTransferred)
            .ToListAsync();
    }

    public async Task MarkAsTransferredAsync(List<int> pickingItemIds, DateTime transferredAt)
    {
        var items = await _context.PickingItems
            .Where(p => pickingItemIds.Contains(p.Id))
            .ToListAsync();

        foreach (var item in items)
        {
            item.IsTransferred = true;
            item.TransferredAt = transferredAt;
        }

        await _context.SaveChangesAsync();
    }
}
