using System.Linq.Expressions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
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
            CreatedByUserId = currentUserId,
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
        WarehouseRequisitionStatus[] statuses, int? workplaceId, int page, int pageSize)
    {
        var q = _context.WarehouseRequisitions
            .Include(r => r.ProductionWorkplace)
            .Include(r => r.Items)
            .Where(r => statuses.Contains(r.Status));
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
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, bool> itemIsFinalShortages,
        int closedByUserId, string user, string winUser, byte[] rowVersion)
    {
        var r = await _context.WarehouseRequisitions
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException($"Requisition {id} not found");
        _context.Entry(r).Property(x => x.RowVersion).OriginalValue = rowVersion;
        foreach (var item in r.Items)
        {
            item.QuantityPicked = itemQuantitiesPicked.TryGetValue(item.Id, out var q) ? q : 0m;
            if (itemNotes.TryGetValue(item.Id, out var note))
                item.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            if (itemIsFinalShortages.TryGetValue(item.Id, out var final))
                item.IsFinalShortage = final;
            item.ModifiedAt = DateTime.Now;
            item.ModifiedBy = user;
            item.ModifiedByWindows = winUser;
        }
        r.Status = DeriveStatus(r);
        r.ClosedAt = DateTime.Now;
        r.ClosedByUserId = closedByUserId;
        r.ModifiedAt = DateTime.Now;
        r.ModifiedBy = user;
        r.ModifiedByWindows = winUser;
        await _context.SaveChangesAsync();
    }

    private static WarehouseRequisitionStatus DeriveStatus(WarehouseRequisition req)
    {
        bool isFullyDelivered = req.Items.All(i =>
            (i.QuantityPicked ?? 0) >= i.QuantityRequested);
        bool hasOpenShortage = req.Items.Any(i =>
            (i.QuantityPicked ?? 0) < i.QuantityRequested && !i.IsFinalShortage);

        return (isFullyDelivered || !hasOpenShortage)
            ? WarehouseRequisitionStatus.Closed
            : WarehouseRequisitionStatus.PartiallyDelivered;
    }

    public async Task SaveNotesAsync(int id, IReadOnlyDictionary<int, string?> itemNotes,
        string user, string winUser)
    {
        if (itemNotes.Count == 0) return;
        var rows = await _context.WarehouseRequisitionItems
            .Where(i => i.WarehouseRequisitionId == id && itemNotes.Keys.Contains(i.Id))
            .ToListAsync();

        var now = DateTime.Now;
        var changed = false;
        foreach (var row in rows)
        {
            if (!itemNotes.TryGetValue(row.Id, out var note)) continue;
            var normalized = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            if (row.Note == normalized) continue;
            row.Note = normalized;
            row.ModifiedAt = now;
            row.ModifiedBy = user;
            row.ModifiedByWindows = winUser;
            changed = true;
        }
        if (changed) await _context.SaveChangesAsync();
    }

    public async Task SaveProgressAsync(int id,
        IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, bool> itemIsFinalShortages,
        string user, string winUser)
    {
        var allKeys = itemQuantitiesPicked.Keys
            .Concat(itemNotes.Keys)
            .Concat(itemIsFinalShortages.Keys)
            .Distinct()
            .ToList();
        if (allKeys.Count == 0) return;

        var rows = await _context.WarehouseRequisitionItems
            .Where(i => i.WarehouseRequisitionId == id && allKeys.Contains(i.Id))
            .ToListAsync();

        var now = DateTime.Now;
        bool anyChanged = false;
        foreach (var row in rows)
        {
            bool rowChanged = false;
            if (itemQuantitiesPicked.TryGetValue(row.Id, out var qty))
            {
                if (row.QuantityPicked != qty)
                {
                    row.QuantityPicked = qty;
                    rowChanged = true;
                }
            }
            if (itemNotes.TryGetValue(row.Id, out var note))
            {
                var normalized = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
                if (row.Note != normalized)
                {
                    row.Note = normalized;
                    rowChanged = true;
                }
            }
            if (itemIsFinalShortages.TryGetValue(row.Id, out var final))
            {
                if (row.IsFinalShortage != final)
                {
                    row.IsFinalShortage = final;
                    rowChanged = true;
                }
            }
            if (rowChanged)
            {
                row.ModifiedAt = now;
                row.ModifiedBy = user;
                row.ModifiedByWindows = winUser;
                anyChanged = true;
            }
        }
        if (anyChanged) await _context.SaveChangesAsync();
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

    public async Task<(IReadOnlyList<MissingPartRow> Items, int TotalCount)>
        GetMissingPartsAsync(int? workplaceFilter,
                             IReadOnlyDictionary<string, string>? columnFilters,
                             DateTime? closedFrom, DateTime? closedUntil,
                             int page, int pageSize)
    {
        var q = _context.WarehouseRequisitionItems
            .Include(i => i.WarehouseRequisition)
                .ThenInclude(r => r.ProductionWorkplace)
            .Where(i => i.IsFinalShortage
                && (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
                    || i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered));

        if (workplaceFilter.HasValue)
            q = q.Where(i => i.WarehouseRequisition.ProductionWorkplaceId == workplaceFilter.Value);
        if (closedFrom.HasValue)
            q = q.Where(i => i.WarehouseRequisition.ClosedAt >= closedFrom.Value);
        if (closedUntil.HasValue)
            q = q.Where(i => i.WarehouseRequisition.ClosedAt < closedUntil.Value);

        if (columnFilters != null)
        {
            if (columnFilters.TryGetValue("ArticleNumber", out var an) && !string.IsNullOrWhiteSpace(an))
                q = ApplyMissingPartsTextFilter(q, an, isArticleNumber: true);
            if (columnFilters.TryGetValue("ArticleDescription", out var ad) && !string.IsNullOrWhiteSpace(ad))
                q = ApplyMissingPartsTextFilter(q, ad, isArticleNumber: false, isDescription: true);
            if (columnFilters.TryGetValue("WorkplaceName", out var wn) && !string.IsNullOrWhiteSpace(wn))
                q = ApplyMissingPartsTextFilter(q, wn, isArticleNumber: false, isDescription: false, isWorkplace: true);
        }

        var total = await q.CountAsync();
        var rows = await q.OrderByDescending(i => i.WarehouseRequisition.ClosedAt ?? DateTime.MinValue)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new MissingPartRow(
                i.WarehouseRequisitionId,
                i.Id,
                i.Position,
                i.WarehouseRequisition.ProductionWorkplace.Name,
                i.ArticleNumber,
                i.ArticleDescription,
                i.QuantityRequested,
                i.QuantityPicked ?? 0m,
                i.QuantityRequested - (i.QuantityPicked ?? 0m),
                i.Unit,
                i.Note,
                i.WarehouseRequisition.CreatedBy,
                i.WarehouseRequisition.ClosedAt))
            .ToListAsync();

        return (rows, total);
    }

    private static IQueryable<WarehouseRequisitionItem> ApplyMissingPartsTextFilter(
        IQueryable<WarehouseRequisitionItem> q,
        string filterValue,
        bool isArticleNumber,
        bool isDescription = false,
        bool isWorkplace = false)
    {
        var tokens = filterValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0) return q;
        var positives = tokens.Where(t => !t.StartsWith("!")).ToList();
        var negatives = tokens.Where(t => t.StartsWith("!")).Select(t => t.Substring(1)).ToList();

        Expression<Func<WarehouseRequisitionItem, string>> selector =
            isArticleNumber
                ? i => i.ArticleNumber
                : isDescription
                    ? i => i.ArticleDescription
                    : i => i.WarehouseRequisition.ProductionWorkplace.Name;

        if (positives.Count > 0)
        {
            q = q.Where(BuildOrContains(selector, positives));
        }
        foreach (var n in negatives)
        {
            var notExpr = BuildNotContains(selector, n);
            q = q.Where(notExpr);
        }
        return q;
    }

    private static Expression<Func<WarehouseRequisitionItem, bool>> BuildOrContains(
        Expression<Func<WarehouseRequisitionItem, string>> selector,
        IReadOnlyList<string> values)
    {
        var param = selector.Parameters[0];
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        Expression? body = null;
        foreach (var v in values)
        {
            var call = Expression.Call(selector.Body, containsMethod, Expression.Constant(v));
            body = body == null ? (Expression)call : Expression.OrElse(body, call);
        }
        return Expression.Lambda<Func<WarehouseRequisitionItem, bool>>(body!, param);
    }

    private static Expression<Func<WarehouseRequisitionItem, bool>> BuildNotContains(
        Expression<Func<WarehouseRequisitionItem, string>> selector,
        string value)
    {
        var param = selector.Parameters[0];
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        var call = Expression.Call(selector.Body, containsMethod, Expression.Constant(value));
        return Expression.Lambda<Func<WarehouseRequisitionItem, bool>>(Expression.Not(call), param);
    }

    public async Task<(int ItemCount, int RequisitionCount)>
        GetFinalShortagesCountForUserAsync(int userId)
    {
        var userWorkplaceIds = await _context.ProductionWorkplaceUsers
            .Where(u => u.UserId == userId)
            .Select(u => u.ProductionWorkplaceId)
            .ToListAsync();
        if (userWorkplaceIds.Count == 0) return (0, 0);

        var q = _context.WarehouseRequisitionItems
            .Where(i => i.IsFinalShortage
                && (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
                    || i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered)
                && userWorkplaceIds.Contains(i.WarehouseRequisition.ProductionWorkplaceId));

        int itemCount = await q.CountAsync();
        int reqCount = await q.Select(i => i.WarehouseRequisitionId).Distinct().CountAsync();
        return (itemCount, reqCount);
    }
}
