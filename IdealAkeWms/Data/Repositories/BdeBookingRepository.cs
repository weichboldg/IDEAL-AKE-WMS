using System.Linq.Expressions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
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

    public Task<int> CountSubFasWithOpenBookingForOrderNumberAsync(string orderNumber) =>
        _ctx.BdeBookings
            .Where(b => b.EndedAt == null && !b.IsCancelled
                        && b.WorkOperation != null
                        && b.WorkOperation.ProductionOrder.OrderNumber == orderNumber)
            .Select(b => b.WorkOperation!.ProductionOrderId)
            .Distinct()
            .CountAsync();

    public Task<List<BdeBooking>> GetHistoryAsync(int skip, int take, int? operatorId, int? workplaceId, DateTime? from, DateTime? to, IReadOnlyDictionary<string, string>? columnFilters = null)
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

        q = ApplyHistoryColumnFilters(q, columnFilters);

        return q.OrderByDescending(b => b.StartedAt).Skip(skip).Take(take).ToListAsync();
    }

    public Task<int> GetHistoryCountAsync(int? operatorId, int? workplaceId, DateTime? from, DateTime? to, IReadOnlyDictionary<string, string>? columnFilters = null)
    {
        var q = _ctx.BdeBookings.AsNoTracking().Where(b => !b.IsCancelled);
        if (operatorId.HasValue) q = q.Where(b => b.BdeOperatorId == operatorId.Value);
        if (workplaceId.HasValue) q = q.Where(b => b.ProductionWorkplaceId == workplaceId.Value);
        if (from.HasValue) q = q.Where(b => b.StartedAt >= from.Value);
        if (to.HasValue) q = q.Where(b => b.StartedAt <= to.Value);
        q = ApplyHistoryColumnFilters(q, columnFilters);
        return q.CountAsync();
    }

    /// <summary>
    /// Wendet die Server-Side-Spaltenfilter (colf_*) identisch auf Liste UND Count an —
    /// muss von <see cref="GetHistoryAsync"/> und <see cref="GetHistoryCountAsync"/> aufgerufen
    /// werden, sonst zaehlt die Pagination Phantom-Seiten. Datums-/berechnete Spalten
    /// (started-at, ended-at, good-qty, scrap-qty) werden hier bewusst IGNORIERT — die
    /// filtert der Controller in C# gegen das gerenderte Format.
    /// </summary>
    private static IQueryable<BdeBooking> ApplyHistoryColumnFilters(
        IQueryable<BdeBooking> q,
        IReadOnlyDictionary<string, string>? columnFilters)
    {
        if (columnFilters == null || columnFilters.Count == 0) return q;

        foreach (var (key, raw) in columnFilters)
        {
            var parsed = ColumnFilterSyntax.Parse(raw, ColumnFilterType.Text);
            if (parsed.IsEmpty) continue;

            Expression<Func<BdeBooking, bool>>? pred = key switch
            {
                "operator" => ColumnFilterSql.Predicate<BdeBooking>(parsed, b => b.BdeOperator.FirstName + " " + b.BdeOperator.LastName),
                "workplace" => ColumnFilterSql.Predicate<BdeBooking>(parsed, b => b.ProductionWorkplace.Name),
                "target" => ColumnFilterSql.Predicate<BdeBooking>(parsed, b => b.WorkOperation != null
                    ? b.WorkOperation.ProductionOrder.OrderNumber + "/" + b.WorkOperation.OperationNumber
                    : (b.BdeActivity != null ? b.BdeActivity.Name : string.Empty)),
                _ => null
            };
            if (pred != null) { q = q.Where(pred); continue; }

            switch (key)
            {
                case "booking-type":
                {
                    var match = ColumnFilterSyntax.MatchingValues(Enum.GetValues<BdeBookingType>(), v => v.ToString(), parsed);
                    q = parsed.Negate ? q.Where(b => !match.Contains(b.BookingType)) : q.Where(b => match.Contains(b.BookingType));
                    break;
                }
                case "status":
                {
                    var match = ColumnFilterSyntax.MatchingValues(Enum.GetValues<BdeBookingStatus>(), v => v.ToString(), parsed);
                    q = parsed.Negate ? q.Where(b => !match.Contains(b.Status)) : q.Where(b => match.Contains(b.Status));
                    break;
                }
                // started-at / ended-at / good-qty / scrap-qty: C#-Filter im Controller (Typ laut Register)
            }
        }

        return q;
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
