using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class BdeAutoPauseServiceTests
{
    private static (ApplicationDbContext ctx, BdeAutoPauseService svc, Mock<IAppSettingRepository> settings)
        Setup(bool masterEnabled = true)
    {
        var ctx = TestDbContextFactory.Create();
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv))
            .ReturnsAsync(masterEnabled ? "true" : "false");
        var calendar = new BdeShiftCalendarService(ctx, settings.Object);
        var svc = new BdeAutoPauseService(ctx, calendar, settings.Object, NullLogger<BdeAutoPauseService>.Instance, new FakeSyncLogger());
        return (ctx, svc, settings);
    }

    [Fact]
    public async Task Run_NoActiveBookings_ReturnsZero()
    {
        var (ctx, svc, _) = Setup();
        var result = await svc.RunAsync(CancellationToken.None);

        result.CheckedCount.Should().Be(0);
        result.PausedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_MasterToggleOff_NoOps()
    {
        var (ctx, svc, _) = Setup(masterEnabled: false);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-2)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.CheckedCount.Should().Be(0);
        result.PausedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_BookingPastShiftEnd_PausesWithShiftEndTimestamp()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        // Frühschicht 06–14 für gestern (deterministisch in der Vergangenheit)
        var day = DateTime.Today.AddDays(-1);
        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = day.DayOfWeek, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            ProductionWorkplaceId = null,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        // Buchung lief seit 08:00 gestern
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: day.AddHours(8)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(1);
        var pausedBooking = ctx.BdeBookings.First();
        pausedBooking.Status.Should().Be(BdeBookingStatus.AutoPaused);
        pausedBooking.EndedAt.Should().Be(day.AddHours(14));
        pausedBooking.ModifiedBy.Should().Be("BDE-AutoPause");
    }

    [Fact]
    public async Task Run_BookingWithinShift_NotPaused()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var today = DateTime.Today;

        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = today.DayOfWeek, StartTime = TimeSpan.FromHours(0), EndTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59)),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: DateTime.Now.AddMinutes(-30)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_BookingOutsideAnyShift_NotPaused()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var today = DateTime.Today;

        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = today.DayOfWeek, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        // Buchung um 23:00 — nach allen Schichten des Tages
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: today.AddHours(23)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_HolidayDay_SkipsAutoPause()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var today = DateTime.Today;

        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = today.DayOfWeek, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.Holidays.Add(new Holiday {
            Date = today, Description = "Heute Feiertag", Source = HolidaySource.Manual,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: today.AddHours(8)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_PausesActivityAndSetupAlongProduction()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        // Gestern (deterministisch in der Vergangenheit)
        var day = DateTime.Today.AddDays(-1);

        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = day.DayOfWeek, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, day.AddHours(8)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Activity,   BdeBookingStatus.Running, day.AddHours(8)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Setup,      BdeBookingStatus.Running, day.AddHours(8)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(3);
        ctx.BdeBookings.All(b => b.Status == BdeBookingStatus.AutoPaused).Should().BeTrue();
    }

    [Fact]
    public async Task Run_DoesNotTouchAlreadyEndedBookings()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var today = DateTime.Today;

        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = today.DayOfWeek, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: today.AddHours(7), endedAt: today.AddHours(13)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_OneBookingThrows_OthersStillProcessed_ErrorsCollected()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var day = DateTime.Today.AddDays(-1);

        var booking1 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: day.AddHours(8));
        var booking2 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: day.AddHours(9));
        ctx.BdeBookings.AddRange(booking1, booking2);
        await ctx.SaveChangesAsync();

        // Calendar wirft fuer Booking 1, liefert vergangenes Schichtende fuer Booking 2.
        var calendar = new Mock<IBdeShiftCalendarService>();
        calendar
            .Setup(c => c.GetShiftEndForBookingAsync(It.IsAny<int>(), It.Is<DateTime>(d => d == booking1.StartedAt)))
            .ThrowsAsync(new InvalidOperationException("synthetic failure"));
        calendar
            .Setup(c => c.GetShiftEndForBookingAsync(It.IsAny<int>(), It.Is<DateTime>(d => d == booking2.StartedAt)))
            .ReturnsAsync(day.AddHours(14));

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv)).ReturnsAsync("true");

        var svc = new BdeAutoPauseService(ctx, calendar.Object, settings.Object,
            NullLogger<BdeAutoPauseService>.Instance, new FakeSyncLogger());

        var result = await svc.RunAsync(CancellationToken.None);

        result.CheckedCount.Should().Be(2);
        result.PausedCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain($"Booking {booking1.Id}");

        var b1 = await ctx.BdeBookings.FindAsync(booking1.Id);
        var b2 = await ctx.BdeBookings.FindAsync(booking2.Id);
        b1!.Status.Should().Be(BdeBookingStatus.Running, "exception path: booking unchanged");
        b2!.Status.Should().Be(BdeBookingStatus.AutoPaused, "happy path: booking paused");
    }

    [Fact]
    public async Task RunAsync_writes_lifecycle_to_synclogger()
    {
        using var ctx = TestDbContextFactory.Create();
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv))
                .ReturnsAsync("true");
        var calendar = new BdeShiftCalendarService(ctx, settings.Object);
        var fakeLogger = new FakeSyncLogger();
        var svc = new BdeAutoPauseService(ctx, calendar, settings.Object,
            NullLogger<BdeAutoPauseService>.Instance, fakeLogger);

        await svc.RunAsync(CancellationToken.None);

        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("BdeAutoPause");
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_logs_booking_id_in_reference_on_pause()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var day = DateTime.Today.AddDays(-1);

        ctx.BdeShifts.Add(new BdeShift
        {
            DayOfWeek = day.DayOfWeek, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            ProductionWorkplaceId = null,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        var booking = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: day.AddHours(8));
        ctx.BdeBookings.Add(booking);
        await ctx.SaveChangesAsync();

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv))
                .ReturnsAsync("true");
        var calendar = new BdeShiftCalendarService(ctx, settings.Object);
        var fakeLogger = new FakeSyncLogger();
        var svc = new BdeAutoPauseService(ctx, calendar, settings.Object,
            NullLogger<BdeAutoPauseService>.Instance, fakeLogger);

        await svc.RunAsync(CancellationToken.None);

        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Info" && e.Reference == $"booking/{booking.Id}");
    }
}
