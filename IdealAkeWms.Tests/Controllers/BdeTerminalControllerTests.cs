using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class BdeTerminalControllerTests
{
    private static BdeTerminalController CreateTerminalController(ApplicationDbContext ctx, bool multiMa = false)
    {
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeMehrfachBuchungProArbeitsgang))
            .ReturnsAsync(multiMa ? "true" : "false");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeMehrfachBuchungProOperator))
            .ReturnsAsync("false");
        settings.Setup(s => s.GetValueAsync("BdeNurFaMeldung"))
            .ReturnsAsync("false");

        var bookingSvc = new BdeBookingService(ctx, userSvc.Object, settings.Object);
        var defaultWoSvc = new BdeDefaultWorkOperationService(ctx, settings.Object);

        var terminals = new Mock<IBdeTerminalRepository>();
        var workplaces = new Mock<IProductionWorkplaceRepository>();

        return new BdeTerminalController(terminals.Object, bookingSvc, userSvc.Object,
            workplaces.Object, defaultWoSvc, settings.Object, ctx);
    }

    [Fact]
    public async Task PausedBookings_ReturnsOnlyPausedForOperator()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused,
            startedAt: DateTime.Now.AddHours(-2), endedAt: DateTime.Now.AddHours(-1)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: DateTime.Now.AddMinutes(-15)));
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx);
        var result = await controller.PausedBookings(ids.OperatorId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        parsed.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task PausedBookings_Empty_ReturnsEmptyArray()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var controller = CreateTerminalController(ctx);

        var result = await controller.PausedBookings(ids.OperatorId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        parsed.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task CloseOthers_DelegatesToService_ReturnsCount()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = op2, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-30),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx);
        var result = await controller.CloseOthers(ids.WorkOperationId, ids.OperatorId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        json.Should().Contain("\"closedCount\":1");
    }

    [Fact]
    public async Task Finish_MultiMaEnabledAndFinalQty_IncludesOtherActiveBookings()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        var own = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(own);
        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = op2, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-30),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx, multiMa: true);

        var result = await controller.Finish(own.Id, goodQty: 5, scrapQty: 0);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"otherActiveBookings\"");
        json.Should().Contain($"\"operatorId\":{op2}");
    }

    [Fact]
    public async Task Finish_NoQuantity_EmptyOtherActiveBookings()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var own = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(own);
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx, multiMa: true);

        var result = await controller.Finish(own.Id, goodQty: null, scrapQty: null);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"otherActiveBookings\":[]");
    }
}
