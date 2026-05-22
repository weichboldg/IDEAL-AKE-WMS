using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class BdeBookingRepository : IBdeBookingRepository
{
    private readonly ApplicationDbContext _ctx;

    public BdeBookingRepository(ApplicationDbContext ctx)
    {
        _ctx = ctx;
    }

    public Task<BdeBooking?> GetByIdAsync(int id) =>
        _ctx.BdeBookings
            .Include(b => b.BdeOperator)
            .Include(b => b.ProductionWorkplace)
            .Include(b => b.BdeTerminal)
            .Include(b => b.WorkOperation)
                .ThenInclude(w => w!.ProductionOrder)
            .Include(b => b.BdeActivity)
            .Include(b => b.Quantities)
            .FirstOrDefaultAsync(b => b.Id == id);

    public Task<BdeBooking?> GetActiveForOperatorAsync(int operatorId) =>
        _ctx.BdeBookings
            .Include(b => b.BdeOperator)
            .Include(b => b.ProductionWorkplace)
            .Include(b => b.WorkOperation)
                .ThenInclude(w => w!.ProductionOrder)
            .Include(b => b.BdeActivity)
            .FirstOrDefaultAsync(b => b.BdeOperatorId == operatorId && b.EndedAt == null && !b.IsCancelled);

    public Task<BdeBooking?> GetActiveForWorkOperationAsync(int workOperationId) =>
        _ctx.BdeBookings
            .Include(b => b.BdeOperator)
            .Include(b => b.ProductionWorkplace)
            .Include(b => b.WorkOperation)
                .ThenInclude(w => w!.ProductionOrder)
            .FirstOrDefaultAsync(b => b.WorkOperationId == workOperationId && b.EndedAt == null && !b.IsCancelled);

    public Task<BdeBooking?> GetLatestPausedForWorkOperationAsync(int workOperationId) =>
        _ctx.BdeBookings
            .Include(b => b.BdeOperator)
            .Include(b => b.ProductionWorkplace)
            .Include(b => b.WorkOperation)
                .ThenInclude(w => w!.ProductionOrder)
            .Where(b => b.WorkOperationId == workOperationId && b.Status == BdeBookingStatus.Paused && !b.IsCancelled)
            .OrderByDescending(b => b.StartedAt)
            .FirstOrDefaultAsync();

    public Task<List<BdeBooking>> GetActiveCockpitAsync() =>
        _ctx.BdeBookings
            .Include(b => b.BdeOperator)
            .Include(b => b.ProductionWorkplace)
            .Include(b => b.WorkOperation)
                .ThenInclude(w => w!.ProductionOrder)
            .Include(b => b.BdeActivity)
            .Where(b => b.EndedAt == null && !b.IsCancelled)
            .OrderBy(b => b.StartedAt)
            .ToListAsync();

    public Task<List<BdeBooking>> GetHistoryAsync(int skip, int take, int? operatorId, int? workplaceId, DateTime? from, DateTime? to)
    {
        var q = _ctx.BdeBookings
            .Include(b => b.BdeOperator)
            .Include(b => b.ProductionWorkplace)
            .Include(b => b.WorkOperation)
                .ThenInclude(w => w!.ProductionOrder)
            .Include(b => b.BdeActivity)
            .Include(b => b.Quantities)
            .Where(b => !b.IsCancelled);

        if (operatorId.HasValue)
            q = q.Where(b => b.BdeOperatorId == operatorId.Value);
        if (workplaceId.HasValue)
            q = q.Where(b => b.ProductionWorkplaceId == workplaceId.Value);
        if (from.HasValue)
            q = q.Where(b => b.StartedAt >= from.Value);
        if (to.HasValue)
            q = q.Where(b => b.StartedAt <= to.Value);

        return q.OrderByDescending(b => b.StartedAt).Skip(skip).Take(take).ToListAsync();
    }

    public Task<int> GetHistoryCountAsync(int? operatorId, int? workplaceId, DateTime? from, DateTime? to)
    {
        var q = _ctx.BdeBookings.AsNoTracking().Where(b => !b.IsCancelled);
        if (operatorId.HasValue) q = q.Where(b => b.BdeOperatorId == operatorId.Value);
        if (workplaceId.HasValue) q = q.Where(b => b.ProductionWorkplaceId == workplaceId.Value);
        if (from.HasValue) q = q.Where(b => b.StartedAt >= from.Value);
        if (to.HasValue) q = q.Where(b => b.StartedAt <= to.Value);
        return q.CountAsync();
    }

    public async Task AddAsync(BdeBooking booking)
    {
        _ctx.BdeBookings.Add(booking);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(BdeBooking booking)
    {
        _ctx.BdeBookings.Update(booking);
        await _ctx.SaveChangesAsync();
    }

    public async Task<decimal> GetTotalGoodAsync(int bookingId)
    {
        var rows = _ctx.BdeBookingQuantities.Where(q => q.BdeBookingId == bookingId);
        if (!await rows.AnyAsync()) return 0m;
        return await rows.SumAsync(q => q.GoodQuantity);
    }

    public async Task<decimal> GetTotalScrapAsync(int bookingId)
    {
        var rows = _ctx.BdeBookingQuantities.Where(q => q.BdeBookingId == bookingId);
        if (!await rows.AnyAsync()) return 0m;
        return await rows.SumAsync(q => q.ScrapQuantity);
    }
}
