using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class SageBookingQueueRepository : ISageBookingQueueRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Audit-Akteur fuer die Service-seitigen Statuswechsel (Konvention wie SyncUser).</summary>
    public const string ServiceActor = "system:sage-booking";

    public SageBookingQueueRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SageBookingQueueItem> EnqueueAsync(StockMovement movement)
    {
        // Idempotent + App-Layer-Guard (InMemory erzwingt den UNIQUE-Index nicht): existiert bereits
        // ein Eintrag zu dieser Buchung, keinen zweiten anlegen (Doppelbuchungs-Schutz).
        var existing = await _context.SageBookingQueueItems
            .FirstOrDefaultAsync(q => q.StockMovementId == movement.Id);
        if (existing != null)
            return existing;

        var item = new SageBookingQueueItem
        {
            StockMovementId = movement.Id,
            Status = SageBookingQueueStatus.Offen,
            AttemptCount = 0,
            CreatedAt = DateTime.Now,
            // Akteur der urspruenglichen Buchung uebernehmen (gleicher Vorgang, gleicher Nutzer).
            CreatedBy = string.IsNullOrEmpty(movement.CreatedBy) ? ServiceActor : movement.CreatedBy,
            CreatedByWindows = string.IsNullOrEmpty(movement.CreatedByWindows) ? ServiceActor : movement.CreatedByWindows
        };
        await _context.SageBookingQueueItems.AddAsync(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<List<SageBookingQueueItem>> GetOpenBatchAsync(int max)
    {
        return await _context.SageBookingQueueItems
            .Include(q => q.StockMovement).ThenInclude(m => m.Article)
            .Include(q => q.StockMovement).ThenInclude(m => m.StorageLocation)
            .Include(q => q.StockMovement).ThenInclude(m => m.SourceStorageLocation)
            .Where(q => q.Status == SageBookingQueueStatus.Offen)
            .OrderBy(q => q.Id)
            .Take(max)
            .ToListAsync();
    }

    public async Task<List<SageBookingQueueItem>> GetStuckSentAsync(DateTime olderThan, int max)
    {
        return await _context.SageBookingQueueItems
            .Include(q => q.StockMovement).ThenInclude(m => m.Article)
            .Include(q => q.StockMovement).ThenInclude(m => m.StorageLocation)
            .Include(q => q.StockMovement).ThenInclude(m => m.SourceStorageLocation)
            .Where(q => q.Status == SageBookingQueueStatus.Gesendet
                     && q.SentAt != null && q.SentAt < olderThan)
            .OrderBy(q => q.Id)
            .Take(max)
            .ToListAsync();
    }

    public async Task<SageBookingQueueItem?> GetByIdWithMovementAsync(int id)
    {
        return await _context.SageBookingQueueItems
            .Include(q => q.StockMovement).ThenInclude(m => m.Article)
            .Include(q => q.StockMovement).ThenInclude(m => m.StorageLocation)
            .Include(q => q.StockMovement).ThenInclude(m => m.SourceStorageLocation)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<List<SageBookingQueueItem>> GetForMonitoringAsync(SageBookingQueueStatus? status)
    {
        var query = _context.SageBookingQueueItems
            .Include(q => q.StockMovement).ThenInclude(m => m.Article)
            .Include(q => q.StockMovement).ThenInclude(m => m.StorageLocation)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(q => q.Status == status.Value);

        return await query.OrderByDescending(q => q.Id).ToListAsync();
    }

    public async Task MarkSentAsync(int id)
    {
        var item = await _context.SageBookingQueueItems.FindAsync(id);
        if (item is null) return;
        item.Status = SageBookingQueueStatus.Gesendet;
        item.SentAt = DateTime.Now;
        item.LastAttemptAt = DateTime.Now;
        item.AttemptCount++;
        Touch(item);
        await _context.SaveChangesAsync();
    }

    public async Task MarkConfirmedAsync(int id, string? sageResponseRaw)
    {
        var item = await _context.SageBookingQueueItems.FindAsync(id);
        if (item is null) return;
        item.Status = SageBookingQueueStatus.Bestaetigt;
        item.ConfirmedAt = DateTime.Now;
        item.SageResponseRaw = sageResponseRaw;
        item.LastError = null;
        Touch(item);
        await _context.SaveChangesAsync();
    }

    public async Task MarkFailedAsync(int id, string error, string? sageResponseRaw)
    {
        var item = await _context.SageBookingQueueItems.FindAsync(id);
        if (item is null) return;
        item.Status = SageBookingQueueStatus.Fehler;
        item.LastError = Truncate(error, 2000);
        item.SageResponseRaw = sageResponseRaw;
        item.LastAttemptAt = DateTime.Now;
        Touch(item);
        await _context.SaveChangesAsync();
    }

    public async Task RequeueAsync(int id, string actor)
    {
        var item = await _context.SageBookingQueueItems.FindAsync(id);
        if (item is null) return;
        item.Status = SageBookingQueueStatus.Offen;
        item.LastError = null;
        item.AttemptCount = 0;   // frische Versuche nach Retry-Cap / Recovery
        item.ModifiedAt = DateTime.Now;
        item.ModifiedBy = actor;
        item.ModifiedByWindows = actor;
        await _context.SaveChangesAsync();
    }

    public async Task<int> EnqueueMissingAsync(DateTime since, int max)
    {
        // Ein-/Ausbuchungen auf Sage-freigegebenen Plaetzen ab 'since' ohne Queue-Eintrag.
        var candidates = await _context.StockMovements
            .Where(m => (m.MovementType == MovementType.Einbuchung || m.MovementType == MovementType.Ausbuchung)
                     && m.Timestamp >= since
                     && m.StorageLocation.SageBuchungErlaubt
                     && !_context.SageBookingQueueItems.Any(q => q.StockMovementId == m.Id))
            .OrderBy(m => m.Id)
            .Take(max)
            .ToListAsync();

        int enqueued = 0;
        foreach (var m in candidates)
        {
            var item = new SageBookingQueueItem
            {
                StockMovementId = m.Id,
                Status = SageBookingQueueStatus.Offen,
                AttemptCount = 0,
                CreatedAt = DateTime.Now,
                CreatedBy = ServiceActor,
                CreatedByWindows = ServiceActor
            };
            _context.SageBookingQueueItems.Add(item);
            try
            {
                // Einzeln speichern: raced der Web-Decorator parallel denselben StockMovement ein,
                // scheitert nur DIESER Insert am UNIQUE-Index (kein Doppel-Enqueue) — der Rest laeuft weiter.
                await _context.SaveChangesAsync();
                enqueued++;
            }
            catch (DbUpdateException)
            {
                _context.Entry(item).State = EntityState.Detached;
            }
        }

        return enqueued;
    }

    private static void Touch(SageBookingQueueItem item)
    {
        item.ModifiedAt = DateTime.Now;
        item.ModifiedBy = ServiceActor;
        item.ModifiedByWindows = ServiceActor;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value.Substring(0, max);
}
