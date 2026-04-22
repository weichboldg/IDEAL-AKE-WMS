using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class BdeApiControllerTests
{
    private readonly Mock<IBdeOperatorRepository> _ops = new();
    private readonly Mock<IBdeActivityRepository> _activities = new();
    private readonly Mock<IBdeBookingRepository> _bookings = new();
    private readonly Mock<IWorkOperationRepository> _workOps = new();
    private readonly Mock<IProductionWorkplaceRepository> _workplaces = new();
    private readonly Mock<IAppSettingRepository> _settings = new();

    private BdeApiController CreateController()
    {
        var ctx = TestDbContextFactory.Create();
        return new(_ops.Object, _activities.Object, _bookings.Object, _workOps.Object,
            _workplaces.Object, _settings.Object, ctx);
    }

    private static BdeOperator CreateOperator(int id, string personnelNumber, string firstName, string lastName)
    {
        return new BdeOperator
        {
            Id = id,
            PersonnelNumber = personnelNumber,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
    }

    [Fact]
    public async Task GetOperator_ReturnsOperator_WhenExists()
    {
        var op = CreateOperator(7, "1234", "Max", "Mustermann");
        _ops.Setup(r => r.GetByPersonnelNumberAsync("1234")).ReturnsAsync(op);

        var result = await CreateController().GetOperator("1234");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { id = 7, displayName = "Max Mustermann", personnelNumber = "1234" });
    }

    [Fact]
    public async Task GetOperator_Returns404_WhenNotFound()
    {
        _ops.Setup(r => r.GetByPersonnelNumberAsync("unknown")).ReturnsAsync((BdeOperator?)null);

        var result = await CreateController().GetOperator("unknown");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetWorkOperation_StripsCommaFromFaNumber()
    {
        var po = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "FA-100",
            ArticleNumber = "ART-1",
            Description1 = "Desc",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        var wp = new ProductionWorkplace
        {
            Id = 5,
            Name = "Werkbank",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        var wo = new WorkOperation
        {
            Id = 42,
            OperationNumber = "10",
            Name = "Zuschneiden",
            ProductionOrderId = po.Id,
            ProductionOrder = po,
            ProductionWorkplaceId = wp.Id,
            ProductionWorkplace = wp,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };

        _workOps.Setup(r => r.GetByFaAndOperationAsync("FA-100", "10")).ReturnsAsync(wo);

        var result = await CreateController().GetWorkOperation("FA-100,xx", "10");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        _workOps.Verify(r => r.GetByFaAndOperationAsync("FA-100", "10"), Times.Once);
        ok.Value.Should().BeEquivalentTo(new
        {
            id = 42,
            operationNumber = "10",
            name = "Zuschneiden",
            orderNumber = "FA-100",
            articleNumber = "ART-1",
            description = "Desc",
            workplaceId = (int?)5,
            workplaceName = "Werkbank"
        });
    }

    [Fact]
    public async Task GetWorkOperation_Returns404_WhenNotFound()
    {
        _workOps.Setup(r => r.GetByFaAndOperationAsync("FA-999", "10")).ReturnsAsync((WorkOperation?)null);

        var result = await CreateController().GetWorkOperation("FA-999", "10");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetWorkOperation_ReturnsBadRequest_WhenInputMissing()
    {
        var result = await CreateController().GetWorkOperation("", "10");
        result.Should().BeOfType<BadRequestResult>();

        var result2 = await CreateController().GetWorkOperation("FA-100", "");
        result2.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task GetActivities_ReturnsList()
    {
        var list = new List<BdeActivity>
        {
            new() { Id = 1, Code = "RUEST", Name = "Ruesten", IsActive = true,
                CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "TEST\\user" },
            new() { Id = 2, Code = "WART", Name = "Wartung", IsActive = true,
                CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "TEST\\user" }
        };
        _activities.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(list);

        var result = await CreateController().GetActivities();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new[]
        {
            new { id = 1, code = "RUEST", name = "Ruesten" },
            new { id = 2, code = "WART", name = "Wartung" }
        });
    }

    [Fact]
    public async Task GetActiveBooking_ReturnsNull_WhenNoActive()
    {
        _bookings.Setup(r => r.GetActiveForOperatorAsync(7)).ReturnsAsync((BdeBooking?)null);

        var result = await CreateController().GetActiveBooking(7);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { booking = (object?)null, nurFaMode = false });
    }

    [Fact]
    public async Task GetActiveBooking_ReturnsBooking_WhenActive()
    {
        var booking = new BdeBooking
        {
            Id = 11,
            BdeOperatorId = 7,
            BookingType = BdeBookingType.Production,
            Status = BdeBookingStatus.Running,
            StartedAt = new DateTime(2026, 4, 14, 8, 0, 0),
            WorkOperationId = 42,
            BdeActivityId = null,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        _bookings.Setup(r => r.GetActiveForOperatorAsync(7)).ReturnsAsync(booking);

        var result = await CreateController().GetActiveBooking(7);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            booking = new
            {
                id = 11,
                bookingType = "Production",
                status = "Running",
                startedAt = new DateTime(2026, 4, 14, 8, 0, 0),
                workOperationId = (int?)42,
                bdeActivityId = (int?)null,
                nurFaMode = false
            }
        });
    }

    [Fact]
    public async Task GetLatestPaused_Returns404_WhenNotFound()
    {
        _bookings.Setup(r => r.GetLatestPausedForWorkOperationAsync(42)).ReturnsAsync((BdeBooking?)null);

        var result = await CreateController().GetLatestPaused(42);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetLatestPaused_ReturnsBooking_WithOperatorName()
    {
        var op = CreateOperator(3, "0001", "Anna", "Admin");
        var booking = new BdeBooking
        {
            Id = 99,
            BdeOperatorId = op.Id,
            BdeOperator = op,
            BookingType = BdeBookingType.Setup,
            Status = BdeBookingStatus.Paused,
            StartedAt = DateTime.Now,
            WorkOperationId = 42,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        _bookings.Setup(r => r.GetLatestPausedForWorkOperationAsync(42)).ReturnsAsync(booking);

        var result = await CreateController().GetLatestPaused(42);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { id = 99, bookingType = "Setup", operatorName = "Anna Admin" });
    }

    [Fact]
    public async Task GetCockpit_ExcludesInactiveWorkplaces()
    {
        // Only the active workplace is returned by GetBdeActiveAsync
        var wpActive = new ProductionWorkplace
        {
            Id = 1,
            Name = "Aktiv",
            BdeAktiv = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        _workplaces.Setup(r => r.GetBdeActiveAsync()).ReturnsAsync(new List<ProductionWorkplace> { wpActive });
        _bookings.Setup(r => r.GetActiveCockpitAsync()).ReturnsAsync(new List<BdeBooking>());

        var result = await CreateController().GetCockpit();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        json.Should().Contain("Aktiv");
        json.Should().NotContain("Inaktiv");
    }

    [Fact]
    public async Task GetAvailableOperations_ReturnsEmpty_WhenWorkplaceInactive()
    {
        var ctx = TestDbContextFactory.Create();
        var wpInactive = new ProductionWorkplace
        {
            Name = "Inaktiv",
            BdeAktiv = false,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wpInactive);
        await ctx.SaveChangesAsync();

        var controller = new BdeApiController(_ops.Object, _activities.Object, _bookings.Object,
            _workOps.Object, _workplaces.Object, _settings.Object, ctx);

        var result = await controller.GetAvailableOperations(wpInactive.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        json.Should().Contain("\"productive\":[]");
        json.Should().Contain("\"unplanned\":[]");
        json.Should().Contain("\"nurFaMode\":false");
    }

    [Fact]
    public async Task GetAvailableOperations_FiltersOutFinishedWorkOperations()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var secondWoId = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        // Erste WO wird als fertig gemeldet (IsFinal-Quantity)
        var finishedBooking = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: DateTime.Now.AddHours(-2), endedAt: DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(finishedBooking);
        await ctx.SaveChangesAsync();
        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity
        {
            BdeBookingId = finishedBooking.Id, BdeOperatorId = ids.OperatorId,
            GoodQuantity = 5, IsFinal = true, ReportedAt = DateTime.Now.AddHours(-1),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        // Seed WorkOperations into the mocked repository
        var wo1 = await ctx.WorkOperations.FindAsync(ids.WorkOperationId);
        var wo2 = await ctx.WorkOperations.FindAsync(secondWoId);
        _workOps.Setup(r => r.GetOpenByWorkplaceIdAsync(ids.WorkplaceId))
            .ReturnsAsync(new List<WorkOperation> { wo1!, wo2! });
        _bookings.Setup(r => r.GetActiveCockpitAsync()).ReturnsAsync(new List<BdeBooking>());
        _activities.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<BdeActivity>());
        _settings.Setup(r => r.GetValueAsync("BdeNurFaMeldung")).ReturnsAsync("false");

        var controller = new BdeApiController(_ops.Object, _activities.Object, _bookings.Object,
            _workOps.Object, _workplaces.Object, _settings.Object, ctx);

        var result = await controller.GetAvailableOperations(ids.WorkplaceId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        // Erste WorkOperation (fertig gemeldet) darf nicht im Response sein, zweite schon
        json.Should().NotContain($"\"id\":{ids.WorkOperationId}");
        json.Should().Contain($"\"id\":{secondWoId}");
    }

    [Fact]
    public async Task GetAvailableOperations_DoesNotFilterOnPartialQuantity()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        // Partial-Quantity (IsFinal=false) sollte NICHT ausgefiltert werden
        var booking = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: DateTime.Now.AddMinutes(-30));
        ctx.BdeBookings.Add(booking);
        await ctx.SaveChangesAsync();
        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity
        {
            BdeBookingId = booking.Id, BdeOperatorId = ids.OperatorId,
            GoodQuantity = 2, IsFinal = false, ReportedAt = DateTime.Now.AddMinutes(-10),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var wo = await ctx.WorkOperations.FindAsync(ids.WorkOperationId);
        _workOps.Setup(r => r.GetOpenByWorkplaceIdAsync(ids.WorkplaceId))
            .ReturnsAsync(new List<WorkOperation> { wo! });
        _bookings.Setup(r => r.GetActiveCockpitAsync()).ReturnsAsync(new List<BdeBooking>());
        _activities.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<BdeActivity>());
        _settings.Setup(r => r.GetValueAsync("BdeNurFaMeldung")).ReturnsAsync("false");

        var controller = new BdeApiController(_ops.Object, _activities.Object, _bookings.Object,
            _workOps.Object, _workplaces.Object, _settings.Object, ctx);

        var result = await controller.GetAvailableOperations(ids.WorkplaceId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        // Partial-Gemeldet: WO bleibt sichtbar
        json.Should().Contain($"\"id\":{ids.WorkOperationId}");
    }

    [Fact]
    public async Task GetAvailableOperations_FiltersOutWorkOperationsOperatorAlreadyBooked()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2Id = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        // Operator hat aktive Buchung auf WO1
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: DateTime.Now.AddMinutes(-30)));
        await ctx.SaveChangesAsync();

        var wo1 = await ctx.WorkOperations.FindAsync(ids.WorkOperationId);
        var wo2 = await ctx.WorkOperations.FindAsync(wo2Id);
        _workOps.Setup(r => r.GetOpenByWorkplaceIdAsync(ids.WorkplaceId))
            .ReturnsAsync(new List<WorkOperation> { wo1!, wo2! });
        _bookings.Setup(r => r.GetActiveCockpitAsync()).ReturnsAsync(new List<BdeBooking>());
        _activities.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<BdeActivity>());
        _settings.Setup(r => r.GetValueAsync("BdeNurFaMeldung")).ReturnsAsync("false");

        var controller = new BdeApiController(_ops.Object, _activities.Object, _bookings.Object,
            _workOps.Object, _workplaces.Object, _settings.Object, ctx);
        var result = await controller.GetAvailableOperations(ids.WorkplaceId, operatorId: ids.OperatorId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        // WO1 wo der Operator schon bucht → ausgeblendet
        json.Should().NotContain($"\"id\":{ids.WorkOperationId}");
        // WO2 → sichtbar
        json.Should().Contain($"\"id\":{wo2Id}");
    }

    [Fact]
    public async Task GetAvailableOperations_WithoutOperatorId_UnchangedBehavior()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        // Operator hat aktive Buchung auf WO, aber kein operatorId übergeben
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: DateTime.Now.AddMinutes(-30)));
        await ctx.SaveChangesAsync();

        var wo = await ctx.WorkOperations.FindAsync(ids.WorkOperationId);
        _workOps.Setup(r => r.GetOpenByWorkplaceIdAsync(ids.WorkplaceId))
            .ReturnsAsync(new List<WorkOperation> { wo! });
        _bookings.Setup(r => r.GetActiveCockpitAsync()).ReturnsAsync(new List<BdeBooking>());
        _activities.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<BdeActivity>());
        _settings.Setup(r => r.GetValueAsync("BdeNurFaMeldung")).ReturnsAsync("false");

        var controller = new BdeApiController(_ops.Object, _activities.Object, _bookings.Object,
            _workOps.Object, _workplaces.Object, _settings.Object, ctx);
        // Kein operatorId → bisheriges Verhalten, kein Crash
        var result = await controller.GetAvailableOperations(ids.WorkplaceId);

        result.Should().BeOfType<OkObjectResult>();
    }
}
