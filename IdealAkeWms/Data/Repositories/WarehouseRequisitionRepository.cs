using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class WarehouseRequisitionRepository : IWarehouseRequisitionRepository
{
    private readonly ApplicationDbContext _context;
    public WarehouseRequisitionRepository(ApplicationDbContext context) { _context = context; }

    public async Task<int> CreateDraftAsync(int productionWorkplaceId, int currentUserId, string currentUserName, string windowsUserName)
    {
        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = productionWorkplaceId,
            Status = WarehouseRequisitionStatus.Draft,
            CreatedAt = DateTime.Now,
            CreatedBy = currentUserName,
            CreatedByWindows = windowsUserName
        };
        _context.WarehouseRequisitions.Add(r);
        await _context.SaveChangesAsync();
        return r.Id;
    }

    public async Task<WarehouseRequisition?> GetByIdAsync(int id, bool includeItems = true)
    {
        var q = _context.WarehouseRequisitions
            .Include(r => r.ProductionWorkplace)
            .Include(r => r.OrderRecipientGroup).ThenInclude(g => g!.Recipients)
            .AsQueryable();
        if (includeItems) q = q.Include(r => r.Items);
        return await q.FirstOrDefaultAsync(r => r.Id == id);
    }

    // AuditableEntity hat nur CreatedBy (string), kein CreatedByUserId. userId-Parameter wird hier
    // nicht direkt im Where verwendet — der Controller filtert post-load via r.CreatedBy == displayName.
    // GetForUserAsync laedt Drafts (immer) + alle Eintraege der letzten N Tage; der Aufrufer schraenkt ein.
    public async Task<List<WarehouseRequisition>> GetForUserAsync(int userId, int historyDays = 30)
    {
        var cutoff = DateTime.Now.AddDays(-historyDays);
        return await _context.WarehouseRequisitions
            .Include(r => r.ProductionWorkplace)
            .Include(r => r.Items)
            .Where(r => r.Status == WarehouseRequisitionStatus.Draft
                || r.CreatedAt >= cutoff)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<WarehouseRequisition> Items, int TotalCount)> GetForWarehouseAsync(
        WarehouseRequisitionStatus? statusFilter, int? workplaceId, int page, int pageSize)
    {
        var q = _context.WarehouseRequisitions
            .Include(r => r.ProductionWorkplace)
            .Include(r => r.Items)
            .Where(r => r.Status != WarehouseRequisitionStatus.Draft);
        if (statusFilter.HasValue) q = q.Where(r => r.Status == statusFilter.Value);
        if (workplaceId.HasValue) q = q.Where(r => r.ProductionWorkplaceId == workplaceId.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.SubmittedAt ?? r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<List<WarehouseRequisition>> GetPendingSubmitEmailsAsync()
    {
        return await _context.WarehouseRequisitions
            .Include(r => r.OrderRecipientGroup).ThenInclude(g => g!.Recipients)
            .Include(r => r.Items)
            .Include(r => r.ProductionWorkplace)
            .Where(r => r.Status == WarehouseRequisitionStatus.Submitted
                && r.EmailSentAt == null
                && r.OrderRecipientGroupId != null)
            .ToListAsync();
    }

    public async Task<List<WarehouseRequisition>> GetPendingCancellationEmailsAsync()
    {
        return await _context.WarehouseRequisitions
            .Include(r => r.OrderRecipientGroup).ThenInclude(g => g!.Recipients)
            .Include(r => r.Items)
            .Include(r => r.ProductionWorkplace)
            .Where(r => r.Status == WarehouseRequisitionStatus.Cancelled
                && r.EmailSentAt != null
                && r.CancellationEmailSentAt == null)
            .ToListAsync();
    }

    public async Task AddItemAsync(int requisitionId, string articleNumber, string description, string? unit,
        decimal quantity, string user, string winUser)
    {
        var alreadyExists = await _context.WarehouseRequisitionItems
            .AnyAsync(i => i.WarehouseRequisitionId == requisitionId
                && i.ArticleNumber == articleNumber);
        if (alreadyExists)
        {
            throw new InvalidOperationException(
                $"Artikel '{articleNumber}' ist bereits in dieser Bestellung enthalten.");
        }

        var nextPos = await _context.WarehouseRequisitionItems
            .Where(i => i.WarehouseRequisitionId == requisitionId)
            .Select(i => (int?)i.Position)
            .MaxAsync() ?? 0;

        _context.WarehouseRequisitionItems.Add(new WarehouseRequisitionItem
        {
            WarehouseRequisitionId = requisitionId,
            ArticleNumber = articleNumber,
            ArticleDescription = description,
            Unit = unit,
            QuantityRequested = quantity,
            Position = nextPos + 1,
            CreatedAt = DateTime.Now,
            CreatedBy = user,
            CreatedByWindows = winUser
        });
        var r = await _context.WarehouseRequisitions.FindAsync(requisitionId);
        if (r != null)
        {
            r.ModifiedAt = DateTime.Now;
            r.ModifiedBy = user;
            r.ModifiedByWindows = winUser;
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpdateItemQuantityAsync(int itemId, decimal quantity, string user, string winUser)
    {
        var item = await _context.WarehouseRequisitionItems.FindAsync(itemId);
        if (item == null) return;
        item.QuantityRequested = quantity;
        item.ModifiedAt = DateTime.Now;
        item.ModifiedBy = user;
        item.ModifiedByWindows = winUser;
        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(int itemId)
    {
        var item = await _context.WarehouseRequisitionItems.FindAsync(itemId);
        if (item == null) return;
        _context.WarehouseRequisitionItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task SubmitAsync(int id, int recipientGroupId, int submittedByUserId, string user, string winUser, byte[] rowVersion)
    {
        var r = await _context.WarehouseRequisitions.FindAsync(id)
            ?? throw new InvalidOperationException($"Requisition {id} not found");
        _context.Entry(r).Property(x => x.RowVersion).OriginalValue = rowVersion;
        r.Status = WarehouseRequisitionStatus.Submitted;
        r.OrderRecipientGroupId = recipientGroupId;
        r.SubmittedAt = DateTime.Now;
        r.SubmittedByUserId = submittedByUserId;
        r.ModifiedAt = DateTime.Now;
        r.ModifiedBy = user;
        r.ModifiedByWindows = winUser;
        await _context.SaveChangesAsync();
    }

    public async Task CloseAsync(int id, IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
        int closedByUserId, string user, string winUser, byte[] rowVersion)
    {
        var r = await _context.WarehouseRequisitions
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException($"Requisition {id} not found");
        _context.Entry(r).Property(x => x.RowVersion).OriginalValue = rowVersion;
        foreach (var item in r.Items)
        {
            item.QuantityPicked = itemQuantitiesPicked.TryGetValue(item.Id, out var q) ? q : item.QuantityRequested;
            item.ModifiedAt = DateTime.Now;
            item.ModifiedBy = user;
            item.ModifiedByWindows = winUser;
        }
        r.Status = WarehouseRequisitionStatus.Closed;
        r.ClosedAt = DateTime.Now;
        r.ClosedByUserId = closedByUserId;
        r.ModifiedAt = DateTime.Now;
        r.ModifiedBy = user;
        r.ModifiedByWindows = winUser;
        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(int id, string? reason, int cancelledByUserId, string user, string winUser, byte[] rowVersion)
    {
        var r = await _context.WarehouseRequisitions.FindAsync(id)
            ?? throw new InvalidOperationException($"Requisition {id} not found");
        _context.Entry(r).Property(x => x.RowVersion).OriginalValue = rowVersion;
        r.Status = WarehouseRequisitionStatus.Cancelled;
        r.CancellationReason = reason;
        r.CancelledAt = DateTime.Now;
        r.CancelledByUserId = cancelledByUserId;
        r.ModifiedAt = DateTime.Now;
        r.ModifiedBy = user;
        r.ModifiedByWindows = winUser;
        await _context.SaveChangesAsync();
    }

    public async Task MarkEmailSentAsync(int id, DateTime sentAt)
    {
        var r = await _context.WarehouseRequisitions.FindAsync(id);
        if (r == null) return;
        r.EmailSentAt = sentAt;
        await _context.SaveChangesAsync();
    }

    public async Task MarkCancellationEmailSentAsync(int id, DateTime sentAt)
    {
        var r = await _context.WarehouseRequisitions.FindAsync(id);
        if (r == null) return;
        r.CancellationEmailSentAt = sentAt;
        await _context.SaveChangesAsync();
    }
}
