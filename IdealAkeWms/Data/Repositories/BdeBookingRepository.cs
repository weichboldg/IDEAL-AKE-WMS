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
            var (tokens, negate) = ColumnFilterHelper.Parse(raw);
            if (tokens.Count == 0) continue;

            switch (key)
            {
                case "operator":
                    q = q.Where(BuildOrContains(b => b.BdeOperator.FirstName + " " + b.BdeOperator.LastName, tokens, negate));
                    break;
                case "workplace":
                    q = q.Where(BuildOrContains(b => b.ProductionWorkplace.Name, tokens, negate));
                    break;
                case "target":
                    q = q.Where(BuildOrContains(b => b.WorkOperation != null
                            ? b.WorkOperation.ProductionOrder.OrderNumber + "/" + b.WorkOperation.OperationNumber
                            : (b.BdeActivity != null ? b.BdeActivity.Name : string.Empty),
                        tokens, negate));
                    break;
                case "booking-type":
                {
                    var match = MatchingEnumValues<BdeBookingType>(tokens);
                    q = negate
                        ? q.Where(b => !match.Contains(b.BookingType))
                        : q.Where(b => match.Contains(b.BookingType));
                    break;
                }
                case "status":
                {
                    var match = MatchingEnumValues<BdeBookingStatus>(tokens);
                    q = negate
                        ? q.Where(b => !match.Contains(b.Status))
                        : q.Where(b => match.Contains(b.Status));
                    break;
                }
                // started-at / ended-at / good-qty / scrap-qty: C#-Filter im Controller
                default:
                    break;
            }
        }

        return q;
    }

    /// <summary>Enum-Werte deren gerenderter Name (ToString, lowercased) eines der Tokens enthaelt.</summary>
    private static List<TEnum> MatchingEnumValues<TEnum>(IReadOnlyList<string> tokens) where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>()
            .Where(v => tokens.Any(t => v.ToString().ToLowerInvariant().Contains(t)))
            .ToList();

    /// <summary>
    /// Baut eine OR-Kette von <c>selector.ToLower().Contains(token)</c>-Calls als Expression-Tree
    /// (Pattern aus WarehouseRequisitionRepository). KEIN EF.Functions.Like — der
    /// InMemory-Provider (Test-Setup) unterstuetzt das nicht. Bei <paramref name="negate"/>
    /// wird die gesamte OR-Kette negiert (Mini-Syntax "!a,b" = weder a noch b).
    /// </summary>
    private static Expression<Func<BdeBooking, bool>> BuildOrContains(
        Expression<Func<BdeBooking, string>> selector,
        IReadOnlyList<string> tokens,
        bool negate)
    {
        var param = selector.Parameters[0];
        var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        var lowered = Expression.Call(selector.Body, toLowerMethod);

        Expression? body = null;
        foreach (var t in tokens)
        {
            var call = Expression.Call(lowered, containsMethod, Expression.Constant(t));
            body = body == null ? (Expression)call : Expression.OrElse(body, call);
        }
        if (negate) body = Expression.Not(body!);
        return Expression.Lambda<Func<BdeBooking, bool>>(body!, param);
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
