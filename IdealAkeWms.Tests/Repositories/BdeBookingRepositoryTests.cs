using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class BdeBookingRepositoryTests
{
    [Fact]
    public async Task GetActiveForOperator_ReturnsRunning()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-10)));
        await ctx.SaveChangesAsync();

        var repo = new BdeBookingRepository(ctx);
        var b = await repo.GetActiveForOperatorAsync(ids.OperatorId);

        b.Should().NotBeNull();
        b!.Status.Should().Be(BdeBookingStatus.Running);
        b.EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveForOperator_IgnoresCancelled()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-10), cancelled: true));
        await ctx.SaveChangesAsync();

        var repo = new BdeBookingRepository(ctx);
        var b = await repo.GetActiveForOperatorAsync(ids.OperatorId);

        b.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveForOperator_IgnoresFinished()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var now = DateTime.Now;
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished, now.AddMinutes(-30), endedAt: now.AddMinutes(-5)));
        await ctx.SaveChangesAsync();

        var repo = new BdeBookingRepository(ctx);
        var b = await repo.GetActiveForOperatorAsync(ids.OperatorId);

        b.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveForWorkOperation_ReturnsRunning_WithOperatorIncluded()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-5)));
        await ctx.SaveChangesAsync();

        var repo = new BdeBookingRepository(ctx);
        var b = await repo.GetActiveForWorkOperationAsync(ids.WorkOperationId);

        b.Should().NotBeNull();
        b!.BdeOperator.Should().NotBeNull();
        b.BdeOperator.FirstName.Should().Be("First");
        b.ProductionWorkplace.Should().NotBeNull();
        b.WorkOperation.Should().NotBeNull();
        b.WorkOperation!.ProductionOrder.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLatestPausedForWorkOperation_ReturnsMostRecent()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var now = DateTime.Now;
        // older paused
        var older = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused, now.AddHours(-4), endedAt: now.AddHours(-3));
        // newer paused
        var newer = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused, now.AddHours(-1), endedAt: now.AddMinutes(-30));
        ctx.BdeBookings.AddRange(older, newer);
        await ctx.SaveChangesAsync();

        var repo = new BdeBookingRepository(ctx);
        var b = await repo.GetLatestPausedForWorkOperationAsync(ids.WorkOperationId);

        b.Should().NotBeNull();
        b!.StartedAt.Should().BeCloseTo(newer.StartedAt, TimeSpan.FromSeconds(1));
        b.BdeOperator.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActiveCockpit_IncludesOperatorAndWorkOperation()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-15)));
        // finished should be excluded
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Setup, BdeBookingStatus.Finished, DateTime.Now.AddHours(-2), endedAt: DateTime.Now.AddHours(-1)));
        await ctx.SaveChangesAsync();

        var repo = new BdeBookingRepository(ctx);
        var list = await repo.GetActiveCockpitAsync();

        list.Should().HaveCount(1);
        list[0].BdeOperator.Should().NotBeNull();
        list[0].ProductionWorkplace.Should().NotBeNull();
        list[0].WorkOperation.Should().NotBeNull();
        list[0].WorkOperation!.ProductionOrder.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTotalGood_SumsAllQuantities()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var booking = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-30));
        ctx.BdeBookings.Add(booking);
        await ctx.SaveChangesAsync();

        var now = DateTime.Now;
        ctx.BdeBookingQuantities.AddRange(
            new BdeBookingQuantity { BdeBookingId = booking.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 3m, ScrapQuantity = 1m, IsFinal = false, ReportedAt = now.AddMinutes(-20), CreatedAt = now, CreatedBy = "t", CreatedByWindows = "t" },
            new BdeBookingQuantity { BdeBookingId = booking.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 5m, ScrapQuantity = 2m, IsFinal = false, ReportedAt = now.AddMinutes(-10), CreatedAt = now, CreatedBy = "t", CreatedByWindows = "t" },
            new BdeBookingQuantity { BdeBookingId = booking.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 4m, ScrapQuantity = 0m, IsFinal = true, ReportedAt = now, CreatedAt = now, CreatedBy = "t", CreatedByWindows = "t" }
        );
        await ctx.SaveChangesAsync();

        var repo = new BdeBookingRepository(ctx);
        var good = await repo.GetTotalGoodAsync(booking.Id);
        var scrap = await repo.GetTotalScrapAsync(booking.Id);

        good.Should().Be(12m);
        scrap.Should().Be(3m);
    }

    [Fact]
    public async Task GetTotalGood_ReturnsZero_WhenNoRows()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var booking = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-5));
        ctx.BdeBookings.Add(booking);
        await ctx.SaveChangesAsync();

        var repo = new BdeBookingRepository(ctx);
        (await repo.GetTotalGoodAsync(booking.Id)).Should().Be(0m);
        (await repo.GetTotalScrapAsync(booking.Id)).Should().Be(0m);
    }

    private static async Task<BdeBookingTestSeed.Ids> SeedThreeOperatorBookingsAsync(IdealAkeWms.Data.ApplicationDbContext ctx)
    {
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var now = DateTime.Now;

        var lastNames = new[] { "Maier", "Huber", "Schulz" };
        foreach (var lastName in lastNames)
        {
            var op = new BdeOperator
            {
                PersonnelNumber = $"P-{lastName}",
                FirstName = "Test",
                LastName = lastName,
                IsActive = true,
                CreatedAt = now, CreatedBy = "t", CreatedByWindows = "t"
            };
            ctx.BdeOperators.Add(op);
            await ctx.SaveChangesAsync();

            var booking = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished, now.AddHours(-2), endedAt: now.AddHours(-1));
            booking.BdeOperatorId = op.Id;
            ctx.BdeBookings.Add(booking);
            await ctx.SaveChangesAsync();
        }

        return ids;
    }

    [Fact]
    public async Task GetHistory_ColumnFilter_Operator()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedThreeOperatorBookingsAsync(ctx);
        var repo = new BdeBookingRepository(ctx);
        var filters = new Dictionary<string, string> { ["operator"] = "maier" };

        var list = await repo.GetHistoryAsync(0, 50, null, null, null, null, filters);
        var count = await repo.GetHistoryCountAsync(null, null, null, null, filters);

        list.Should().HaveCount(1);
        list[0].BdeOperator.LastName.Should().Be("Maier");
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetHistory_ColumnFilter_Negate()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedThreeOperatorBookingsAsync(ctx);
        var repo = new BdeBookingRepository(ctx);
        var filters = new Dictionary<string, string> { ["operator"] = "!maier" };

        var list = await repo.GetHistoryAsync(0, 50, null, null, null, null, filters);
        var count = await repo.GetHistoryCountAsync(null, null, null, null, filters);

        list.Should().HaveCount(2);
        list.Select(b => b.BdeOperator.LastName).Should().BeEquivalentTo("Huber", "Schulz");
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetHistory_ColumnFilter_Or()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedThreeOperatorBookingsAsync(ctx);
        var repo = new BdeBookingRepository(ctx);
        var filters = new Dictionary<string, string> { ["operator"] = "maier,huber" };

        var list = await repo.GetHistoryAsync(0, 50, null, null, null, null, filters);
        var count = await repo.GetHistoryCountAsync(null, null, null, null, filters);

        list.Should().HaveCount(2);
        list.Select(b => b.BdeOperator.LastName).Should().BeEquivalentTo("Maier", "Huber");
        count.Should().Be(2);
    }
}
