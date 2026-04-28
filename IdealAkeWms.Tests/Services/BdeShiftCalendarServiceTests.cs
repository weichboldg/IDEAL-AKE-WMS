using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class BdeShiftCalendarServiceTests
{
    private static (ApplicationDbContext ctx, BdeShiftCalendarService svc) Setup(bool masterEnabled = true)
    {
        var ctx = TestDbContextFactory.Create();
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv))
                .ReturnsAsync(masterEnabled ? "true" : "false");
        return (ctx, new BdeShiftCalendarService(ctx, settings.Object));
    }

    private static int SeedWorkplace(ApplicationDbContext ctx, bool useCustom = false)
    {
        var wp = new ProductionWorkplace
        {
            Name = "WB", BdeAktiv = true, BdeUseCustomShiftPlan = useCustom,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();
        return wp.Id;
    }

    private static BdeShift NewShift(DayOfWeek day, int startHour, int endHour, int? workplaceId = null, string? name = null) => new()
    {
        DayOfWeek = day, StartTime = TimeSpan.FromHours(startHour), EndTime = TimeSpan.FromHours(endHour),
        ProductionWorkplaceId = workplaceId, Name = name,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
    };

    private static DateTime Monday(int hour, int minute = 0)
    {
        var today = DateTime.Today;
        var diff = ((int)today.DayOfWeek + 6) % 7; // Mo=0
        return today.AddDays(-diff).AddHours(hour).AddMinutes(minute);
    }

    [Fact]
    public async Task MasterToggleOff_ReturnsNull()
    {
        var (ctx, svc) = Setup(masterEnabled: false);
        var wp = SeedWorkplace(ctx);
        ctx.BdeShifts.Add(NewShift(Monday(0).DayOfWeek, 6, 14));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, Monday(8));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Sunday_NoShifts_ReturnsNull()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        ctx.BdeShifts.Add(NewShift(DayOfWeek.Monday, 6, 14));
        await ctx.SaveChangesAsync();

        var sunday = Monday(8).AddDays(-1); // Sonntag
        var result = await svc.GetShiftEndForBookingAsync(wp, sunday);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Holiday_ReturnsNull()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        var monday = Monday(8);
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14));
        ctx.Holidays.Add(new Holiday { Date = monday.Date, Source = HolidaySource.Manual,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Within_EarlyShift_ReturnsShiftEnd()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        var monday = Monday(8, 30);
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().Be(monday.Date.AddHours(14));
    }

    [Fact]
    public async Task BetweenShifts_ReturnsNextShiftEnd()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        var monday = Monday(14, 30);
        ctx.BdeShifts.AddRange(NewShift(monday.DayOfWeek, 6, 14), NewShift(monday.DayOfWeek, 14, 22));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().Be(monday.Date.AddHours(22));
    }

    [Fact]
    public async Task BeforeFirstShift_ReturnsFirstShiftEnd()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        var monday = Monday(4);
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().Be(monday.Date.AddHours(14));
    }

    [Fact]
    public async Task AfterLastShift_ReturnsNull()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        var monday = Monday(23);
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().BeNull();
    }

    [Fact]
    public async Task WorkbenchOverride_PrefersOwnPlan()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx, useCustom: true);
        var monday = Monday(8);
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14, workplaceId: null));        // Default: 06–14
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 8, 16, workplaceId: wp));          // Werkbank: 08–16
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().Be(monday.Date.AddHours(16));
    }

    [Fact]
    public async Task WorkbenchOverrideEmpty_ReturnsNull()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx, useCustom: true);
        var monday = Monday(8);
        // Default-Plan vorhanden, aber Override-Toggle erzwingt eigenen (leeren) Plan
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14, workplaceId: null));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().BeNull();
    }
}
