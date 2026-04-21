using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class BdeTimeSplitServiceTests
{
    private static DateTime Today(int hour = 0, int minute = 0)
        => DateTime.Today.AddHours(hour).AddMinutes(minute);

    [Fact]
    public async Task SingleBooking_NoOverlap_ReturnsFullDuration()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0)));
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        result.Should().HaveCount(1);
        result[0].EffectiveDuration.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task TwoBookings_FullOverlap_EqualQty_SplitsHalf()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        var b1 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0));
        var b2 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0), workOperationId: wo2);
        ctx.BdeBookings.AddRange(b1, b2);
        await ctx.SaveChangesAsync();

        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = b1.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 5, IsFinal = true, ReportedAt = Today(10, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = b2.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 5, IsFinal = true, ReportedAt = Today(10, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        result.Should().HaveCount(2);
        result.First(r => r.BookingId == b1.Id).EffectiveDuration.Should().Be(TimeSpan.FromHours(1));
        result.First(r => r.BookingId == b2.Id).EffectiveDuration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task TwoBookings_FullOverlap_UnequalQty_SplitsByQty()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        var b1 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0));
        var b2 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0), workOperationId: wo2);
        ctx.BdeBookings.AddRange(b1, b2);
        await ctx.SaveChangesAsync();

        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = b1.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 3, IsFinal = true, ReportedAt = Today(10, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = b2.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 2, IsFinal = true, ReportedAt = Today(10, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        // 2h total, split 3:2 → 1.2h : 0.8h = 72min : 48min
        result.First(r => r.BookingId == b1.Id).EffectiveDuration.Should().Be(TimeSpan.FromMinutes(72));
        result.First(r => r.BookingId == b2.Id).EffectiveDuration.Should().Be(TimeSpan.FromMinutes(48));
    }

    [Fact]
    public async Task ThreeSegments_SoloParallelSolo_CorrectSums()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        // A: 08:00-12:00, B: 10:00-14:00, Gutmenge je 5
        var bA = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(12, 0));
        var bB = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(10, 0), endedAt: Today(14, 0), workOperationId: wo2);
        ctx.BdeBookings.AddRange(bA, bB);
        await ctx.SaveChangesAsync();

        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = bA.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 5, IsFinal = true, ReportedAt = Today(12, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = bB.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 5, IsFinal = true, ReportedAt = Today(14, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        // A: solo 08-10 (2h) + parallel 10-12 (1h of 2h split 50/50) = 3h
        // B: parallel 10-12 (1h) + solo 12-14 (2h) = 3h
        result.First(r => r.BookingId == bA.Id).EffectiveDuration.Should().Be(TimeSpan.FromHours(3));
        result.First(r => r.BookingId == bB.Id).EffectiveDuration.Should().Be(TimeSpan.FromHours(3));
    }

    [Fact]
    public async Task NoBookings_ReturnsEmpty()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelledBookingsIgnored()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0), cancelled: true));
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FallbackToEqual_WhenNoSollmengeAndNoGutmenge()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        // Beide FAs haben Quantity 0 → Fallback auf gleichmäßig
        ctx.ProductionOrders.First().Quantity = 0;
        await ctx.SaveChangesAsync();

        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0), workOperationId: wo2));
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        result.Should().HaveCount(2);
        result.All(r => r.EffectiveDuration == TimeSpan.FromHours(1)).Should().BeTrue();
    }

    [Fact]
    public async Task ComputeEffectiveDurationAsync_SingleDay()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var b = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0));
        ctx.BdeBookings.Add(b);
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var duration = await svc.ComputeEffectiveDurationAsync(b.Id);

        duration.Should().Be(TimeSpan.FromHours(2));
    }
}
