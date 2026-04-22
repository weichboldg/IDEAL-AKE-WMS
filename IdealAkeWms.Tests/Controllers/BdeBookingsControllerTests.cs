using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class BdeBookingsControllerTests
{
    private static BdeBookingsController CreateBookingsController(ApplicationDbContext ctx)
    {
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");

        var ops = new Mock<IBdeOperatorRepository>();
        ops.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<BdeOperator>());

        var workplaces = new Mock<IProductionWorkplaceRepository>();
        workplaces.Setup(r => r.GetBdeActiveAsync()).ReturnsAsync(new List<ProductionWorkplace>());

        // Use real repository backed by in-memory ctx so bookings are found
        var repo = new BdeBookingRepository(ctx);

        // Use real service — pure EF queries
        var timeSplitSvc = new BdeTimeSplitService(ctx);

        return new BdeBookingsController(repo, ops.Object, workplaces.Object, userSvc.Object, timeSplitSvc, ctx);
    }

    [Fact]
    public async Task Index_PopulatesEffectiveDurations()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var b = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: DateTime.Today.AddHours(8), endedAt: DateTime.Today.AddHours(10));
        ctx.BdeBookings.Add(b);
        await ctx.SaveChangesAsync();

        var controller = CreateBookingsController(ctx);

        var result = await controller.Index() as ViewResult;

        var vm = result!.Model as BdeBookingsIndexViewModel;
        vm.Should().NotBeNull();
        vm!.EffectiveDurations.Should().ContainKey(b.Id);
        vm.EffectiveDurations[b.Id].Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task Index_EmptyBookings_EmptyDictionary()
    {
        var ctx = TestDbContextFactory.Create();
        var controller = CreateBookingsController(ctx);

        var result = await controller.Index() as ViewResult;

        var vm = result!.Model as BdeBookingsIndexViewModel;
        vm.Should().NotBeNull();
        vm!.EffectiveDurations.Should().BeEmpty();
    }

    [Fact]
    public async Task Index_TerminalBookingShowsCumulativeDuration()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var parent = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Resumed,
            startedAt: DateTime.Today.AddHours(8), endedAt: DateTime.Today.AddHours(9));
        ctx.BdeBookings.Add(parent);
        await ctx.SaveChangesAsync();

        var child = new BdeBooking
        {
            BdeOperatorId = ids.OperatorId, WorkOperationId = ids.WorkOperationId,
            BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Finished,
            StartedAt = DateTime.Today.AddHours(9).AddMinutes(30),
            EndedAt = DateTime.Today.AddHours(11),
            ParentBookingId = parent.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.BdeBookings.Add(child);
        await ctx.SaveChangesAsync();

        var controller = CreateBookingsController(ctx);

        var result = await controller.Index() as ViewResult;
        var vm = result!.Model as BdeBookingsIndexViewModel;
        vm.Should().NotBeNull();

        // Parent (has child) → own time: 1h
        vm!.EffectiveDurations[parent.Id].Should().Be(TimeSpan.FromHours(1));
        // Child (terminal) → cumulative: 1h parent + 1h30 child = 2h30
        vm.EffectiveDurations[child.Id].Should().Be(TimeSpan.FromMinutes(150));
    }
}
