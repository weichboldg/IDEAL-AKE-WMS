using System.Linq.Expressions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class WarehouseRequisitionRepository : IWarehouseRequisitionRepository
{
    private readonly ApplicationDbContext _context;
    public WarehouseRequisitionRepository(ApplicationDbContext context) { _context = context; }

    public async Task<int> CreateDraftAsync(int productionWorkplaceId, WarehouseRequisitionType type, int currentUserId, string currentUserName, string windowsUserName)
    {
        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = productionWorkplaceId,
            Status = WarehouseRequisitionStatus.Draft,
            Type = type,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.Now,
            CreatedBy = currentUserName,
            CreatedByWindows = windowsUserName
        };
        _context.WarehouseRequisitions.Add(r);
        await _context.SaveChangesAsync();
        return r.Id;
    }

    public async Task<WarehouseRequisition?> GetOpenDraftForUserAndTypeAsync(int userId, WarehouseRequisitionType type)
    {
        return await _context.WarehouseRequisitions
            .Where(r => r.Status == WarehouseRequisitionStatus.Draft
                && r.Type == type
                && r.CreatedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
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

    public async Task<WarehouseRequisition?> GetByItemIdAsync(int itemId)
    {
        return await _context.WarehouseRequisitionItems
            .Where(i => i.Id == itemId)
            .Select(i => i.WarehouseRequisition)
            .FirstOrDefaultAsync();
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
        WarehouseRequisitionStatus[] statuses, int? workplaceId, WarehouseRequisitionType? type, int page, int pageSize)
    {
        var q = _context.WarehouseRequisitions
            .Include(r => r.ProductionWorkplace)
            .Include(r => r.Items)
            .Where(r => statuses.Contains(r.Status));
        if (workplaceId.HasValue) q = q.Where(r => r.ProductionWorkplaceId == workplaceId.Value);
        if (type.HasValue) q = q.Where(r => r.Type == type.Value);

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
        // DUMMY-Schluessel vom Duplikat-Guard ausnehmen: mehrere DUMMY-Positionen mit
        // unterschiedlichen Bezeichnungen in einer Bestellung sind erlaubt (Teil-7).
        if (articleNumber != Article.DummyArticleNumber)
        {
            var alreadyExists = await _context.WarehouseRequisitionItems
                .AnyAsync(i => i.WarehouseRequisitionId == requisitionId
                    && i.ArticleNumber == articleNumber);
            if (alreadyExists)
            {
                throw new InvalidOperationException(
                    $"Artikel '{articleNumber}' ist bereits in dieser Bestellung enthalten.");
            }
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

    public async Task<IReadOnlyList<(int Position, string ArticleNumber)>> CloseAsync(int id,
        IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, string?> itemNotesEinkauf,
        IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
        int closedByUserId, string user, string winUser, byte[] rowVersion)
    {
        var r = await _context.WarehouseRequisitions
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException($"Requisition {id} not found");
        _context.Entry(r).Property(x => x.RowVersion).OriginalValue = rowVersion;

        // Pflichtpruefung VOR jeder Aenderung (Hotfix v1.30.1): nicht Geliefertes muss bestaetigt sein —
        // Ist-Menge (auch 0) oder Fehlteil-Markierung. Nichts wird angenommen, nichts still als 0 gebucht.
        var incomplete = r.Items
            .Where(i => (itemQuantitiesPicked.TryGetValue(i.Id, out var qp) ? qp : null) == null
                && (itemShortageStatuses.TryGetValue(i.Id, out var st) ? st : i.ShortageStatus) == ShortageStatus.None)
            .OrderBy(i => i.Position)
            .Select(i => (i.Position, i.ArticleNumber))
            .ToList();
        if (incomplete.Count > 0)
            return incomplete;

        foreach (var item in r.Items)
        {
            item.QuantityPicked = itemQuantitiesPicked.TryGetValue(item.Id, out var q) ? q : null;
            if (itemNotes.TryGetValue(item.Id, out var note))
                item.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            if (itemNotesEinkauf.TryGetValue(item.Id, out var noteEk))
                item.NoteEinkauf = string.IsNullOrWhiteSpace(noteEk) ? null : noteEk.Trim();
            if (itemShortageStatuses.TryGetValue(item.Id, out var status))
                item.ShortageStatus = status;
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
        return Array.Empty<(int, string)>();
    }

    private static WarehouseRequisitionStatus DeriveStatus(WarehouseRequisition req)
    {
        bool isFullyDelivered = req.Items.All(i =>
            (i.QuantityPicked ?? 0) >= i.QuantityRequested);
        bool hasWaitingRestock = req.Items.Any(i =>
            i.ShortageStatus == ShortageStatus.WillBeRestocked);

        return (isFullyDelivered || !hasWaitingRestock)
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

    public async Task SaveCommentAsync(int id, string? comment, string user, string winUser)
    {
        var r = await _context.WarehouseRequisitions.FindAsync(id);
        if (r == null) return;
        var normalized = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (r.Comment == normalized) return;
        r.Comment = normalized;
        r.ModifiedAt = DateTime.Now;
        r.ModifiedBy = user;
        r.ModifiedByWindows = winUser;
        await _context.SaveChangesAsync();
    }

    public async Task SaveProgressAsync(int id,
        IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, string?> itemNotesEinkauf,
        IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
        string user, string winUser)
    {
        var allKeys = itemQuantitiesPicked.Keys
            .Concat(itemNotes.Keys)
            .Concat(itemNotesEinkauf.Keys)
            .Concat(itemShortageStatuses.Keys)
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
            if (itemNotesEinkauf.TryGetValue(row.Id, out var noteEk))
            {
                var normalized = string.IsNullOrWhiteSpace(noteEk) ? null : noteEk.Trim();
                if (row.NoteEinkauf != normalized)
                {
                    row.NoteEinkauf = normalized;
                    rowChanged = true;
                }
            }
            if (itemShortageStatuses.TryGetValue(row.Id, out var status))
            {
                if (row.ShortageStatus != status)
                {
                    row.ShortageStatus = status;
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
        GetMissingPartsAsync(ShortageStatus filterStatus,
                             WarehouseRequisitionType? type,
                             IReadOnlyCollection<int>? workplaceIds,
                             IReadOnlyDictionary<string, string>? columnFilters,
                             DateTime? closedFrom, DateTime? closedUntil,
                             int page, int pageSize)
    {
        var q = _context.WarehouseRequisitionItems
            .Include(i => i.WarehouseRequisition)
                .ThenInclude(r => r.ProductionWorkplace)
            .Where(i => i.ShortageStatus == filterStatus
                && (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
                    || i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered));

        if (type.HasValue)
            q = q.Where(i => i.WarehouseRequisition.Type == type.Value);
        // Spec 2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl: Werkbank-Menge in SQL VOR Count/Paging (ADR 0005).
        // null = kein Werkbank-Filter, leere Menge = keine Treffer.
        if (workplaceIds != null)
            q = q.Where(i => workplaceIds.Contains(i.WarehouseRequisition.ProductionWorkplaceId));
        if (closedFrom.HasValue)
            q = q.Where(i => i.WarehouseRequisition.ClosedAt >= closedFrom.Value);
        if (closedUntil.HasValue)
            q = q.Where(i => i.WarehouseRequisition.ClosedAt < closedUntil.Value);

        if (columnFilters != null)
        {
            foreach (var (key, raw) in columnFilters)
            {
                var parsed = ColumnFilterSyntax.Parse(raw, ColumnFilterType.Text);
                if (parsed.IsEmpty) continue;
                var pred = MissingPartsColumnPredicate(key, parsed);
                if (pred != null) q = q.Where(pred);
            }
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
                i.WarehouseRequisition.ClosedAt,
                i.ShortageStatus,
                i.NoteEinkauf))
            .ToListAsync();

        return (rows, total);
    }

    /// <summary>Fehlteile + Fehlteile-Lager: Spaltenfilter ueber den zentralen SQL-Bauer (Spaltenfilter-Suchsyntax, Etappe B).
    /// Antwort 5: das <c>!</c> am Anfang gilt fuer ALLE Teile wie ueberall (vorher je Wert). Artikelnummer trifft zusaetzlich den
    /// Matchcode aus dem Artikelstamm (korrelierte Subquery per <see cref="ColumnFilterSql.Splice{T}"/>).</summary>
    private Expression<Func<WarehouseRequisitionItem, bool>>? MissingPartsColumnPredicate(string key, ParsedColumnFilter f) => key switch
    {
        "ArticleNumber" => ColumnFilterSql.Combine<WarehouseRequisitionItem>(f, (part, value) => ColumnFilterSql.Splice(
            part.Op == ColumnFilterOp.Empty
                ? (Expression<Func<WarehouseRequisitionItem, bool>>)(i => ColumnFilterSql.Wert(i.ArticleNumber))
                : i => ColumnFilterSql.Wert(i.ArticleNumber) ||
                       _context.Set<Article>().Any(a => a.ArticleNumber == i.ArticleNumber && ColumnFilterSql.Wert(a.Matchcode)),
            value)),
        "ArticleDescription" => ColumnFilterSql.Predicate<WarehouseRequisitionItem>(f, i => i.ArticleDescription),
        "WorkplaceName" => ColumnFilterSql.Predicate<WarehouseRequisitionItem>(f, i => i.WarehouseRequisition.ProductionWorkplace.Name),
        "NoteLager" => ColumnFilterSql.Predicate<WarehouseRequisitionItem>(f, i => i.Note),
        "NoteEinkauf" => ColumnFilterSql.Predicate<WarehouseRequisitionItem>(f, i => i.NoteEinkauf),
        _ => null
    };

    public async Task<(int WaitingItemCount, int WaitingRequisitionCount,
                       int NoRestockItemCount, int NoRestockRequisitionCount)>
        GetShortageCountsForUserAsync(int userId, WarehouseRequisitionType? type)
    {
        var userWorkplaceIds = await _context.ProductionWorkplaceUsers
            .Where(u => u.UserId == userId)
            .Select(u => u.ProductionWorkplaceId)
            .ToListAsync();
        if (userWorkplaceIds.Count == 0) return (0, 0, 0, 0);

        var q = _context.WarehouseRequisitionItems
            .Where(i => (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
                         || i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered)
                && userWorkplaceIds.Contains(i.WarehouseRequisition.ProductionWorkplaceId));
        // v1.81.0 (Spec 2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas A): Karte zaehlt den aktiven Bestelltyp wie die Liste dahinter;
        // null = alle Typen (Semantik wie GetMissingPartsAsync).
        if (type.HasValue) q = q.Where(i => i.WarehouseRequisition.Type == type.Value);

        var waiting = q.Where(i => i.ShortageStatus == ShortageStatus.WillBeRestocked);
        var noRestock = q.Where(i => i.ShortageStatus == ShortageStatus.NoRestock);

        int waitingItems = await waiting.CountAsync();
        int waitingReqs = await waiting.Select(i => i.WarehouseRequisitionId).Distinct().CountAsync();
        int noRestockItems = await noRestock.CountAsync();
        int noRestockReqs = await noRestock.Select(i => i.WarehouseRequisitionId).Distinct().CountAsync();

        return (waitingItems, waitingReqs, noRestockItems, noRestockReqs);
    }
}
