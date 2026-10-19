using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// v1.64.0 (Spec 2026-10-19-bde-terminal-liste-scan-ohne-ag-prio, FA-5, AK 13-16, Befund Q4 aus v1.63.0): negative Mengen an allen
/// BDE-Mengeneingaengen M1-M6 werden abgewiesen, bevor etwas geschrieben wird.
/// </summary>
public class BdeNegativeMengenTests
{
    private const string Meldung = "Mengen dürfen nicht negativ sein.";

    private static BdeBookingService Svc(ApplicationDbContext ctx)
    {
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync("false");
        return new BdeBookingService(ctx, new FakeCurrentUserService(), settings.Object);
    }

    private static async Task<(BdeBookingTestSeed.Ids Ids, BdeBooking Booking)> RunningAsync(ApplicationDbContext ctx)
    {
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var b = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-30));
        ctx.BdeBookings.Add(b);
        await ctx.SaveChangesAsync();
        return (ids, b);
    }

    [Theory]
    [InlineData(-1.0, null)]
    [InlineData(null, -0.5)]
    public async Task Pause_Negativ_Abgewiesen_BuchungBleibtRunning_M1(double? good, double? scrap)
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, b) = await RunningAsync(ctx);

        var r = await Svc(ctx).PauseAsync(b.Id, (decimal?)good, (decimal?)scrap);

        r.Message.Should().Be(Meldung);
        (await ctx.BdeBookings.FindAsync(b.Id))!.Status.Should().Be(BdeBookingStatus.Running);
        (await ctx.BdeBookingQuantities.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Finish_Negativ_Abgewiesen_M2()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, b) = await RunningAsync(ctx);

        var r = await Svc(ctx).FinishAsync(b.Id, 5, -1);

        r.Message.Should().Be(Meldung);
        (await ctx.BdeBookings.FindAsync(b.Id))!.Status.Should().Be(BdeBookingStatus.Running);
        (await ctx.BdeBookingQuantities.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Teilmenge_Negativ_Abgewiesen_M3()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, b) = await RunningAsync(ctx);

        var r = await Svc(ctx).ReportPartialQuantityAsync(b.Id, -3, 0);

        r.Message.Should().Be(Meldung);
        (await ctx.BdeBookingQuantities.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Fertigmeldung_Negativ_Abgewiesen_KeineBuchung_M4()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        (await ctx.ProductionWorkplaces.FindAsync(ids.WorkplaceId))!.BdeNurAgFertigmeldung = true;
        await ctx.SaveChangesAsync();

        var r = await Svc(ctx).ReportCompletionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, -5, null);

        r.Message.Should().Be(Meldung);
        (await ctx.BdeBookings.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task GruppenAbschluss_EinEintragNegativ_GanzeGruppeAbgewiesen_M5()
    {
        using var ctx = TestDbContextFactory.Create();
        var (ids, b1) = await RunningAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);
        var b2 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-10),
            workOperationId: wo2);
        ctx.BdeBookings.Add(b2);
        await ctx.SaveChangesAsync();

        var r = await Svc(ctx).FinishGroupAsync(ids.OperatorId, new[]
        {
            new GroupFinishEntry(b1.Id, 3, 0, true), new GroupFinishEntry(b2.Id, -1, 0, true)
        });

        r.ErrorMessage.Should().Be(Meldung);
        (await ctx.BdeBookings.CountAsync(x => x.Status == BdeBookingStatus.Running)).Should().Be(2);
        (await ctx.BdeBookingQuantities.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task NullUndPositiv_WieBisher_AK16()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, b) = await RunningAsync(ctx);

        (await Svc(ctx).ReportPartialQuantityAsync(b.Id, 0, 2)).Outcome.Should().Be(BdeBookingOutcome.Success);
        (await Svc(ctx).FinishAsync(b.Id, 3, 0)).Outcome.Should().Be(BdeBookingOutcome.Success);
    }

    [Theory]
    [InlineData(-1, 0, false)]
    [InlineData(0, -0.01, false)]
    [InlineData(0, 0, true)]
    [InlineData(12.5, 1, true)]
    public void AdminKorrektur_RangeAufMengen_M6(double good, double scrap, bool gueltig)
    {
        var row = new BdeBookingQuantityEditRow { GoodQuantity = (decimal)good, ScrapQuantity = (decimal)scrap };
        var fehler = new List<ValidationResult>();

        Validator.TryValidateObject(row, new ValidationContext(row), fehler, validateAllProperties: true).Should().Be(gueltig);
        if (!gueltig) fehler.Single().ErrorMessage.Should().Be("Menge darf nicht negativ sein.");
    }
}
