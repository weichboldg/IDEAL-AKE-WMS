using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class BdeBookingServiceTests
{
    private static BdeBookingService NewService(out Data.ApplicationDbContext ctx)
    {
        ctx = TestDbContextFactory.Create();
        return new BdeBookingService(ctx, new FakeCurrentUserService());
    }

    [Fact]
    public async Task StartSetup_CreatesRunningBooking_WithTypeSetup()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var result = await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        result.Booking.Should().NotBeNull();
        result.Booking!.BookingType.Should().Be(BdeBookingType.Setup);
        result.Booking.Status.Should().Be(BdeBookingStatus.Running);
        result.Booking.EndedAt.Should().BeNull();
        result.Booking.WorkOperationId.Should().Be(ids.WorkOperationId);
    }

    [Fact]
    public async Task StartSetup_WhenOperatorHasOtherSetup_AutoClosesOld()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        var first = await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        first.Outcome.Should().Be(BdeBookingOutcome.Success);

        var second = await svc.StartSetupAsync(ids.OperatorId, wo2, ids.WorkplaceId, ids.TerminalId);
        second.Outcome.Should().Be(BdeBookingOutcome.Success);
        second.Booking!.WorkOperationId.Should().Be(wo2);

        var oldBooking = await ctx.BdeBookings.FindAsync(first.Booking!.Id);
        oldBooking!.EndedAt.Should().NotBeNull();
        oldBooking.Status.Should().Be(BdeBookingStatus.Finished);
    }

    [Fact]
    public async Task StartSetup_WhenOperatorHasOpenProduction_ReturnsQuantityRequired()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        var result = await svc.StartSetupAsync(ids.OperatorId, wo2, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.QuantityRequired);
        result.Booking!.WorkOperationId.Should().Be(ids.WorkOperationId);
    }

    [Fact]
    public async Task StartSetup_WhenWorkOperationAlreadyActiveElsewhere_ReturnsCollision()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        var result = await svc.StartSetupAsync(op2, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.CollisionOtherOperator);
        result.CollidingBooking.Should().NotBeNull();
        result.CollidingBooking!.BdeOperatorId.Should().Be(ids.OperatorId);
    }

    [Fact]
    public async Task StartProduction_FromSetupSameAG_AutoClosesSetupCreatesProduction()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var setup = await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        var prod = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        prod.Outcome.Should().Be(BdeBookingOutcome.Success);
        prod.Booking!.BookingType.Should().Be(BdeBookingType.Production);
        prod.Booking.ParentBookingId.Should().Be(setup.Booking!.Id);

        var closedSetup = await ctx.BdeBookings.FindAsync(setup.Booking!.Id);
        closedSetup!.Status.Should().Be(BdeBookingStatus.Finished);
        closedSetup.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StartProduction_FromSetupSameAGDifferentOperator_ReturnsCollision()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        var result = await svc.StartProductionAsync(op2, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.CollisionOtherOperator);
        result.CollidingBooking!.BookingType.Should().Be(BdeBookingType.Setup);
    }

    [Fact]
    public async Task AutoClose_AndNewStart_DoesNotViolateUniqueIndex()
    {
        // On InMemory DB the filtered unique index is not enforced, but we
        // still verify the service saves the close BEFORE the new insert by
        // asserting the final state is consistent (one active only).
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        var result = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        var activeCount = await ctx.BdeBookings
            .CountAsync(b => b.WorkOperationId == ids.WorkOperationId && b.EndedAt == null && !b.IsCancelled);
        activeCount.Should().Be(1);
    }

    [Fact]
    public async Task StartActivity_CreatesActivityBooking()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var result = await svc.StartActivityAsync(ids.OperatorId, ids.ActivityId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        result.Booking!.BookingType.Should().Be(BdeBookingType.Activity);
        result.Booking.BdeActivityId.Should().Be(ids.ActivityId);
        result.Booking.WorkOperationId.Should().BeNull();
        result.Booking.Status.Should().Be(BdeBookingStatus.Running);
    }

    [Fact]
    public async Task Pause_Production_WithPartialQuantity_CreatesQuantityEntry_SetsStatusPaused()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var start = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        var result = await svc.PauseAsync(start.Booking!.Id, goodQty: 5m, scrapQty: 1m);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        result.Booking!.Status.Should().Be(BdeBookingStatus.Paused);
        result.Booking.EndedAt.Should().NotBeNull();
        var qty = await ctx.BdeBookingQuantities.SingleAsync();
        qty.BdeBookingId.Should().Be(start.Booking!.Id);
        qty.GoodQuantity.Should().Be(5m);
        qty.ScrapQuantity.Should().Be(1m);
        qty.IsFinal.Should().BeFalse();
    }

    [Fact]
    public async Task Pause_Setup_SetsStatusPaused_NoQuantity()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var start = await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        var result = await svc.PauseAsync(start.Booking!.Id, goodQty: 5m, scrapQty: 1m);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        result.Booking!.Status.Should().Be(BdeBookingStatus.Paused);
        (await ctx.BdeBookingQuantities.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Resume_SameOperator_CreatesNewBookingWithParentId()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var start = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        await svc.PauseAsync(start.Booking!.Id, 3m, 0m);

        var resume = await svc.ResumeAsync(start.Booking!.Id, ids.OperatorId, BdeBookingType.Production, ids.WorkplaceId, ids.TerminalId);

        resume.Outcome.Should().Be(BdeBookingOutcome.Success);
        resume.Booking!.Id.Should().NotBe(start.Booking!.Id);
        resume.Booking.ParentBookingId.Should().Be(start.Booking!.Id);
        resume.Booking.Status.Should().Be(BdeBookingStatus.Running);
        resume.Booking.BdeOperatorId.Should().Be(ids.OperatorId);
    }

    [Fact]
    public async Task Resume_DifferentOperator_CreatesNewBookingWithParentIdAndNewOperator()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        var start = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        await svc.PauseAsync(start.Booking!.Id, 2m, 0m);

        var resume = await svc.ResumeAsync(start.Booking!.Id, op2, BdeBookingType.Production, ids.WorkplaceId, ids.TerminalId);

        resume.Outcome.Should().Be(BdeBookingOutcome.Success);
        resume.Booking!.BdeOperatorId.Should().Be(op2);
        resume.Booking.ParentBookingId.Should().Be(start.Booking!.Id);
    }

    [Fact]
    public async Task Resume_FromPausedSetup_AsProduction_TransitionsType()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var start = await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        await svc.PauseAsync(start.Booking!.Id);

        var resume = await svc.ResumeAsync(start.Booking!.Id, ids.OperatorId, BdeBookingType.Production, ids.WorkplaceId, ids.TerminalId);

        resume.Outcome.Should().Be(BdeBookingOutcome.Success);
        resume.Booking!.BookingType.Should().Be(BdeBookingType.Production);
        resume.Booking.ParentBookingId.Should().Be(start.Booking!.Id);
    }

    [Fact]
    public async Task Finish_Production_RequiresFinalQuantity_StoresIt()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var start = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        var noQty = await svc.FinishAsync(start.Booking!.Id);
        noQty.Outcome.Should().Be(BdeBookingOutcome.QuantityRequired);

        var withQty = await svc.FinishAsync(start.Booking!.Id, goodQty: 10m, scrapQty: 2m);
        withQty.Outcome.Should().Be(BdeBookingOutcome.Success);
        withQty.Booking!.Status.Should().Be(BdeBookingStatus.Finished);
        withQty.Booking.EndedAt.Should().NotBeNull();
        var finalQ = await ctx.BdeBookingQuantities.SingleAsync(q => q.BdeBookingId == start.Booking!.Id && q.IsFinal);
        finalQ.GoodQuantity.Should().Be(10m);
        finalQ.ScrapQuantity.Should().Be(2m);
    }

    [Fact]
    public async Task Finish_Setup_WithoutQuantity_Succeeds()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var start = await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        var result = await svc.FinishAsync(start.Booking!.Id);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        result.Booking!.Status.Should().Be(BdeBookingStatus.Finished);
        (await ctx.BdeBookingQuantities.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Finish_Activity_WithoutQuantity_Succeeds()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var start = await svc.StartActivityAsync(ids.OperatorId, ids.ActivityId, ids.WorkplaceId, ids.TerminalId);
        var result = await svc.FinishAsync(start.Booking!.Id);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        result.Booking!.Status.Should().Be(BdeBookingStatus.Finished);
        (await ctx.BdeBookingQuantities.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ReportPartialQuantity_AppendsNonFinalQuantity_KeepsStatusRunning()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var start = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        var result = await svc.ReportPartialQuantityAsync(start.Booking!.Id, 3m, 1m);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        var reloaded = await ctx.BdeBookings.FindAsync(start.Booking!.Id);
        reloaded!.Status.Should().Be(BdeBookingStatus.Running);
        reloaded.EndedAt.Should().BeNull();
        var qty = await ctx.BdeBookingQuantities.SingleAsync();
        qty.IsFinal.Should().BeFalse();
        qty.GoodQuantity.Should().Be(3m);
        qty.ScrapQuantity.Should().Be(1m);
    }

    [Fact]
    public async Task ReportPartialQuantity_OnFinishedBooking_ReturnsInvalidState()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var start = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        await svc.FinishAsync(start.Booking!.Id, 10m, 0m);

        var result = await svc.ReportPartialQuantityAsync(start.Booking!.Id, 1m, 0m);
        result.Outcome.Should().Be(BdeBookingOutcome.InvalidState);
    }

    [Fact]
    public async Task Collision_ShowsOtherOperatorAndWorkplace_InResult()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        var result = await svc.StartSetupAsync(op2, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.CollisionOtherOperator);
        result.CollidingBooking.Should().NotBeNull();
        result.CollidingBooking!.BdeOperator.Should().NotBeNull();
        result.CollidingBooking!.ProductionWorkplace.Should().NotBeNull();
        result.CollidingBooking.BdeOperatorId.Should().Be(ids.OperatorId);
        result.CollidingBooking.ProductionWorkplaceId.Should().Be(ids.WorkplaceId);
    }
}
