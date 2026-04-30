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

public class OseonReportingControllerTests
{
    private static OseonReportingController CreateController(ApplicationDbContext ctx,
        Mock<IAppSettingRepository>? settings = null)
    {
        settings ??= new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync("OseonReportingHorizonDays")).ReturnsAsync("10");

        var orderRepo = new OseonProductionOrderRepository(ctx);
        var configRepo = new OseonOperationConfigRepository(ctx);
        var holidayRepo = new HolidayRepository(ctx);
        var workplaces = new ProductionWorkplaceRepository(ctx);

        var businessDays = new Mock<IBusinessDayService>();
        businessDays.Setup(x => x.AddBusinessDays(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<HashSet<DateTime>>()))
            .Returns<DateTime, int, HashSet<DateTime>>((d, n, _) =>
            {
                var current = d.Date;
                var sign = Math.Sign(n);
                var rem = Math.Abs(n);
                while (rem > 0)
                {
                    current = current.AddDays(sign);
                    if (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday) continue;
                    rem--;
                }
                return current;
            });

        return new OseonReportingController(orderRepo, configRepo, holidayRepo, workplaces, settings.Object, businessDays.Object);
    }

    private static OseonProductionOrder NewOrder(long oseonId, DateTime dueDate, int status = 60,
        string custOrder = "K-100", string faNumber = "FA-100", int? workplaceId = null)
        => new()
        {
            OseonId = oseonId, OseonOrderNumber = faNumber, CustomerOrderNumber = custOrder,
            OseonStatus = status, ProductionWorkplaceId = workplaceId, DueDate = dueDate,
            LastChangedInOseon = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };

    private static OseonWorkOperation NewWo(string position, string name, int status, DateTime? lastReport = null)
        => new()
        {
            PositionNumber = position, Name = name, OseonStatus = status,
            LastStatusReportInOseon = lastReport,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };

    private static OseonOperationConfig NewCfg(string name, int offset = 0)
        => new()
        {
            OperationName = name, DueDateOffsetDays = offset, IsOseonRelevant = true
        };

    [Fact]
    public async Task OperationsOverview_DefaultSliceIsToday()
    {
        var ctx = TestDbContextFactory.Create();
        var controller = CreateController(ctx);

        var result = await controller.OperationsOverview(null, null, null, null, null, null) as ViewResult;

        var vm = result!.Model as OseonReportingViewModel;
        vm!.Filter.Slice.Should().Be(OseonReportingSlice.Today);
    }

    [Fact]
    public async Task OperationsOverview_KpiCountsAreCorrect()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonOperationConfigs.Add(NewCfg("B", 0));

        var today = DateTime.Today;
        var orderOverdue = NewOrder(1, today.AddDays(-2));
        orderOverdue.WorkOperations = new List<OseonWorkOperation> { NewWo("10", "B", 60) };

        var orderTodayPlanned = NewOrder(2, today);
        orderTodayPlanned.WorkOperations = new List<OseonWorkOperation> { NewWo("10", "B", 60) };

        var orderTodayDone = NewOrder(3, today);
        orderTodayDone.WorkOperations = new List<OseonWorkOperation> { NewWo("10", "B", 90, today) };

        var orderFuture = NewOrder(4, today.AddDays(3));
        orderFuture.WorkOperations = new List<OseonWorkOperation> { NewWo("10", "B", 60) };

        ctx.OseonProductionOrders.AddRange(orderOverdue, orderTodayPlanned, orderTodayDone, orderFuture);
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var result = await controller.OperationsOverview(null, null, null, null, null, OseonReportingSlice.All) as ViewResult;
        var vm = result!.Model as OseonReportingViewModel;

        vm!.Kpis.OverdueCount.Should().Be(1);
        vm.Kpis.TodayPlannedCount.Should().Be(2);
        vm.Kpis.TodayDoneCount.Should().Be(1);
        vm.Kpis.FutureCount.Should().Be(1);
    }

    [Fact]
    public async Task OperationsOverview_TodayDoneRequiresLastStatusReportToday()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonOperationConfigs.Add(NewCfg("B", 0));

        var order = NewOrder(1, DateTime.Today);
        order.WorkOperations = new List<OseonWorkOperation> {
            NewWo("10", "B", 90, DateTime.Today.AddDays(-3))
        };
        ctx.OseonProductionOrders.Add(order);
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var result = await controller.OperationsOverview(null, null, null, null, null, OseonReportingSlice.All) as ViewResult;
        var vm = result!.Model as OseonReportingViewModel;

        vm!.Kpis.TodayDoneCount.Should().Be(0);
    }

    [Fact]
    public async Task OperationsOverview_OperationsWithoutConfig_BannerCounterSet()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, DateTime.Today);
        order.WorkOperations = new List<OseonWorkOperation> { NewWo("10", "UNKNOWN", 60) };
        ctx.OseonProductionOrders.Add(order);
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var result = await controller.OperationsOverview(null, null, null, null, null, null) as ViewResult;
        var vm = result!.Model as OseonReportingViewModel;

        vm!.OperationsWithoutConfigCount.Should().Be(1);
    }

    [Fact]
    public async Task OperationsOverview_HorizonOverrideIsClampedToValidRange()
    {
        var ctx = TestDbContextFactory.Create();
        var controller = CreateController(ctx);

        var result1 = await controller.OperationsOverview(null, null, null, null, -5, null) as ViewResult;
        var vm1 = result1!.Model as OseonReportingViewModel;
        vm1!.HorizonDaysEffective.Should().Be(1);

        var result2 = await controller.OperationsOverview(null, null, null, null, 999, null) as ViewResult;
        var vm2 = result2!.Model as OseonReportingViewModel;
        vm2!.HorizonDaysEffective.Should().Be(60);
    }
}
