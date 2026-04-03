using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class PartRequisitionRepository : IPartRequisitionRepository
{
    private readonly ApplicationDbContext _context;

    public PartRequisitionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PartRequisition?> GetByIdAsync(int id)
    {
        return await _context.PartRequisitions
            .Include(r => r.ProductionOrder)
            .Include(r => r.OrderRecipientGroup)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task AddAsync(PartRequisition requisition)
    {
        _context.PartRequisitions.Add(requisition);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<PartRequisition> requisitions)
    {
        _context.PartRequisitions.AddRange(requisitions);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PartRequisition requisition)
    {
        _context.PartRequisitions.Update(requisition);
        await _context.SaveChangesAsync();
    }

    public async Task<List<PartRequisition>> GetByProductionOrderAsync(int productionOrderId)
    {
        return await _context.PartRequisitions
            .Where(r => r.ProductionOrderId == productionOrderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PartRequisition>> GetOpenByArticleNumberAsync(string articleNumber)
    {
        return await _context.PartRequisitions
            .Include(r => r.ProductionOrder)
            .Where(r => r.ArticleNumber == articleNumber && r.Status == PartRequisitionStatus.Offen)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasOpenRequisitionAsync(int productionOrderId, string articleNumber)
    {
        return await _context.PartRequisitions
            .AnyAsync(r => r.ProductionOrderId == productionOrderId
                        && r.ArticleNumber == articleNumber
                        && r.Status == PartRequisitionStatus.Offen);
    }

    public async Task<(List<PartRequisition> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, bool showAll = false, string? searchTerm = null)
    {
        var query = _context.PartRequisitions
            .Include(r => r.ProductionOrder)
            .AsQueryable();

        if (!showAll)
            query = query.Where(r => r.Status == PartRequisitionStatus.Offen);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(r =>
                r.ArticleNumber.Contains(term) ||
                (r.ArticleDescription != null && r.ArticleDescription.Contains(term)) ||
                r.ProductionOrder.OrderNumber.Contains(term) ||
                (r.ProductionOrder.Customer != null && r.ProductionOrder.Customer.Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<PartRequisition>> GetUnsentAsync()
    {
        return await _context.PartRequisitions
            .Include(r => r.ProductionOrder)
            .Where(r => r.EmailSentAt == null && r.Status == PartRequisitionStatus.Offen)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task FulfillAsync(int requisitionId, int stockMovementId, string modifiedBy, string modifiedByWindows)
    {
        var requisition = await _context.PartRequisitions.FindAsync(requisitionId);
        if (requisition == null || requisition.Status != PartRequisitionStatus.Offen) return;

        requisition.Status = PartRequisitionStatus.Erfuellt;
        requisition.FulfilledByStockMovementId = stockMovementId;
        requisition.FulfilledAt = DateTime.UtcNow;
        requisition.ModifiedAt = DateTime.UtcNow;
        requisition.ModifiedBy = modifiedBy;
        requisition.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(int requisitionId, string cancelledBy, string modifiedBy, string modifiedByWindows)
    {
        var requisition = await _context.PartRequisitions.FindAsync(requisitionId);
        if (requisition == null || requisition.Status != PartRequisitionStatus.Offen) return;

        requisition.Status = PartRequisitionStatus.Storniert;
        requisition.CancelledAt = DateTime.UtcNow;
        requisition.CancelledBy = cancelledBy;
        requisition.ModifiedAt = DateTime.UtcNow;
        requisition.ModifiedBy = modifiedBy;
        requisition.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }
}
