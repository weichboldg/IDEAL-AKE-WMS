using IdealAkeWms.Data;
using IdealAkeWms.Models;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// Seeds minimal entities (User, ProductionWorkplace, BdeOperator, BdeTerminal,
/// ProductionOrder, WorkOperation, BdeActivity) for BdeBooking-related tests.
/// </summary>
public static class BdeBookingTestSeed
{
    public class Ids
    {
        public int UserId { get; set; }
        public int WorkplaceId { get; set; }
        public int OperatorId { get; set; }
        public int TerminalId { get; set; }
        public int ProductionOrderId { get; set; }
        public int WorkOperationId { get; set; }
        public int ActivityId { get; set; }
    }

    public static async Task<Ids> SeedAsync(ApplicationDbContext ctx, string suffix = "")
    {
        var now = DateTime.Now;
        const string actor = "t";

        var user = new User { Name = $"terminaluser{suffix}", PasswordHash = "x", CreatedAt = now, CreatedBy = actor, CreatedByWindows = actor };
        var wp = new ProductionWorkplace { Name = $"Werkbank{suffix}", BdeAktiv = true, CreatedAt = now, CreatedBy = actor, CreatedByWindows = actor };
        ctx.Users.Add(user);
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();

        var op = new BdeOperator
        {
            PersonnelNumber = $"P{suffix}{Guid.NewGuid().ToString().Substring(0, 6)}",
            FirstName = "First", LastName = "Last", IsActive = true,
            CreatedAt = now, CreatedBy = actor, CreatedByWindows = actor
        };
        var terminal = new BdeTerminal
        {
            UserId = user.Id, DefaultProductionWorkplaceId = wp.Id,
            CreatedAt = now, CreatedBy = actor, CreatedByWindows = actor
        };
        var po = new ProductionOrder
        {
            OrderNumber = $"FA{suffix}{Guid.NewGuid().ToString().Substring(0, 6)}",
            Quantity = 10,
            CreatedAt = now, CreatedBy = actor, CreatedByWindows = actor
        };
        var activity = new BdeActivity
        {
            Code = $"A{suffix}{Guid.NewGuid().ToString().Substring(0, 4)}",
            Name = "Test Activity", IsActive = true,
            CreatedAt = now, CreatedBy = actor, CreatedByWindows = actor
        };
        ctx.BdeOperators.Add(op);
        ctx.BdeTerminals.Add(terminal);
        ctx.ProductionOrders.Add(po);
        ctx.BdeActivities.Add(activity);
        await ctx.SaveChangesAsync();

        var wo = new WorkOperation
        {
            ProductionOrderId = po.Id,
            OperationNumber = "10",
            Name = "Test Operation",
            ProductionWorkplaceId = wp.Id,
            Sequence = 10,
            IsReportable = true,
            CreatedAt = now, CreatedBy = actor, CreatedByWindows = actor
        };
        ctx.WorkOperations.Add(wo);
        await ctx.SaveChangesAsync();

        return new Ids
        {
            UserId = user.Id,
            WorkplaceId = wp.Id,
            OperatorId = op.Id,
            TerminalId = terminal.Id,
            ProductionOrderId = po.Id,
            WorkOperationId = wo.Id,
            ActivityId = activity.Id
        };
    }

    public static async Task<int> AddSecondWorkOperationAsync(ApplicationDbContext ctx, Ids ids)
    {
        var now = DateTime.Now;
        var wo = new WorkOperation
        {
            ProductionOrderId = ids.ProductionOrderId,
            OperationNumber = "20",
            Name = "Second Operation",
            ProductionWorkplaceId = ids.WorkplaceId,
            Sequence = 20,
            IsReportable = true,
            CreatedAt = now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.WorkOperations.Add(wo);
        await ctx.SaveChangesAsync();
        return wo.Id;
    }

    public static async Task<int> AddSecondOperatorAsync(ApplicationDbContext ctx)
    {
        var now = DateTime.Now;
        var op = new BdeOperator
        {
            PersonnelNumber = $"P2{Guid.NewGuid().ToString().Substring(0, 6)}",
            FirstName = "Other", LastName = "Person", IsActive = true,
            CreatedAt = now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.BdeOperators.Add(op);
        await ctx.SaveChangesAsync();
        return op.Id;
    }

    public static BdeBooking NewBooking(Ids ids, BdeBookingType type, BdeBookingStatus status, DateTime startedAt, DateTime? endedAt = null, bool cancelled = false, int? workOperationId = null, int? activityId = null)
    {
        var now = DateTime.Now;
        return new BdeBooking
        {
            BdeOperatorId = ids.OperatorId,
            ProductionWorkplaceId = ids.WorkplaceId,
            BdeTerminalId = ids.TerminalId,
            WorkOperationId = workOperationId ?? (type == BdeBookingType.Activity ? null : ids.WorkOperationId),
            BdeActivityId = activityId ?? (type == BdeBookingType.Activity ? ids.ActivityId : null),
            BookingType = type,
            Status = status,
            StartedAt = startedAt,
            EndedAt = endedAt,
            IsCancelled = cancelled,
            CreatedAt = now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
    }
}
