using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class BdeTerminalControllerTests
{
    private static BdeTerminalController CreateTerminalController(ApplicationDbContext ctx, bool multiMa = false, bool multiOp = false, bool groupFinish = false, bool nurFa = false)
    {
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeMehrfachBuchungProArbeitsgang))
            .ReturnsAsync(multiMa ? "true" : "false");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeMehrfachBuchungProOperator))
            .ReturnsAsync(multiOp ? "true" : "false");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeGleichzeitigerAbschlussBeiMehrfachStart))
            .ReturnsAsync(groupFinish ? "true" : "false");
        settings.Setup(s => s.GetValueAsync("BdeNurFaMeldung"))
            .ReturnsAsync(nurFa ? "true" : "false");

        var bookingSvc = new BdeBookingService(ctx, userSvc.Object, settings.Object);
        var defaultWoSvc = new BdeDefaultWorkOperationService(ctx, settings.Object);

        var terminals = new Mock<IBdeTerminalRepository>();
        var workplaces = new Mock<IProductionWorkplaceRepository>();

        return new BdeTerminalController(terminals.Object, bookingSvc, userSvc.Object,
            workplaces.Object, defaultWoSvc, settings.Object, ctx, NullLogger<BdeTerminalController>.Instance);
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
    public async Task PausedBookings_IncludesAutoPaused()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.AutoPaused,
            startedAt: DateTime.Now.AddHours(-3), endedAt: DateTime.Now.AddHours(-1)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused,
            startedAt: DateTime.Now.AddHours(-2), endedAt: DateTime.Now.AddMinutes(-30)));
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx);
        var result = await controller.PausedBookings(ids.OperatorId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        parsed.GetArrayLength().Should().Be(2);
        json.Should().Contain("AutoPaused");
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
    public async Task PausedBookings_ExcludesParentsAlreadyResumed()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        // Pausierte Parent-Buchung
        var parent = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused,
            startedAt: DateTime.Now.AddHours(-2), endedAt: DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(parent);
        await ctx.SaveChangesAsync();

        // Resumed Child-Buchung (mit ParentBookingId → parent)
        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = ids.OperatorId,
            WorkOperationId = ids.WorkOperationId,
            BdeTerminalId = ids.TerminalId,
            ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production,
            Status = BdeBookingStatus.Running,
            StartedAt = DateTime.Now.AddMinutes(-30),
            ParentBookingId = parent.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx);
        var result = await controller.PausedBookings(ids.OperatorId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        // Parent sollte NICHT in der paused-list auftauchen, weil bereits resumed
        parsed.GetArrayLength().Should().Be(0);
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

    // v1.63.0 (AK 30): Fertigmeldung bei erlaubter Mehrfachbuchung -> Antwort traegt die anderen Laufenden.
    [Fact]
    public async Task ReportCompletion_MultiMa_SavedAndIncludesOtherActiveBookings()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        (await ctx.ProductionWorkplaces.FindAsync(ids.WorkplaceId))!.BdeNurAgFertigmeldung = true;
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = op2, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-30),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx, multiMa: true);
        var result = await controller.ReportCompletion(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 10, 0);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"outcome\":\"Success\"");
        json.Should().Contain($"\"operatorId\":{op2}");
        json.Should().Contain($"\"workOperationId\":{ids.WorkOperationId}");
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

    [Fact]
    public async Task ActiveBookings_ReturnsOnlyRunningForOperator()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        // 2 active (running) bookings for this operator
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-30), workOperationId: wo2));
        // 1 paused + 1 finished — should NOT appear
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused,
            startedAt: DateTime.Now.AddHours(-3), endedAt: DateTime.Now.AddHours(-2)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: DateTime.Now.AddHours(-4), endedAt: DateTime.Now.AddHours(-3)));
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx);
        var result = await controller.ActiveBookings(ids.OperatorId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        parsed.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task ActiveBookings_IncludesTargetQuantity()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1)));
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx);
        var result = await controller.ActiveBookings(ids.OperatorId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        json.Should().Contain("\"targetQuantity\":10");
    }

    [Fact]
    public async Task Finish_GroupSettingOnAndMultipleActive_ReturnsGroupFinishRequired()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        var b1 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        var b2 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-30), workOperationId: wo2);
        ctx.BdeBookings.AddRange(b1, b2);
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx, multiOp: true, groupFinish: true);

        var result = await controller.Finish(b1.Id, goodQty: 5m, scrapQty: 0m);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"outcome\":\"GroupFinishRequired\"");
        json.Should().Contain($"\"bookingId\":{b1.Id}");
        json.Should().Contain($"\"bookingId\":{b2.Id}");
    }

    // QA v1.63.0 (FA-a9/Antwort 12): StartProductionForOrder prueft VOR dem Anlegen des Default-AG „01".
    [Theory]
    [InlineData(true, false)]  // NurFA an, FA an anderer/keiner Werkbank
    [InlineData(false, true)]  // Normal-Modus: Endpunkt wird nicht angeboten, auch FA der eigenen Werkbank
    public async Task StartProductionForOrder_NotOffered_Rejected_NoDefaultAgCreated(bool nurFa, bool orderAtTerminal)
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        if (orderAtTerminal)
        {
            (await ctx.ProductionOrders.FindAsync(ids.ProductionOrderId))!.ProductionWorkplaceId = ids.WorkplaceId;
            await ctx.SaveChangesAsync();
        }
        var before = ctx.WorkOperations.Count();

        var result = await CreateTerminalController(ctx, nurFa: nurFa)
            .StartProductionForOrder(ids.OperatorId, ids.ProductionOrderId, ids.WorkplaceId, ids.TerminalId);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"outcome\":\"InvalidState\"");
        ctx.WorkOperations.Count().Should().Be(before);
        ctx.BdeBookings.Count().Should().Be(0);
    }

    [Fact]
    public async Task StartProductionForOrder_NurFa_OrderOfTerminalWorkplace_Started()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        (await ctx.ProductionOrders.FindAsync(ids.ProductionOrderId))!.ProductionWorkplaceId = ids.WorkplaceId;
        (await ctx.ProductionWorkplaces.FindAsync(ids.WorkplaceId))!.BdeDefaultArbeitsgang = "PRODUKTION";
        await ctx.SaveChangesAsync();

        var result = await CreateTerminalController(ctx, nurFa: true)
            .StartProductionForOrder(ids.OperatorId, ids.ProductionOrderId, ids.WorkplaceId, ids.TerminalId);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"outcome\":\"Success\"");
        ctx.WorkOperations.Should().Contain(w => w.ProductionOrderId == ids.ProductionOrderId && w.OperationNumber == "01");
    }
}
