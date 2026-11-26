using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.81 (Entscheid 9, U-5 B+Admin): nicht fortsetzbare Pausen abschliessen – Kopf, Terminal-Aktion, Admin-Liste.</summary>
public class BdeHaengendePausenTests
{
    private static BdeBookingService Svc(Data.ApplicationDbContext ctx) =>
        new(ctx, new FakeCurrentUserService(), Mock.Of<IdealAkeWms.Data.Repositories.IAppSettingRepository>());

    [Fact]
    public async Task FortsetzenSperre_FaAbgeholt_Grund_SonstNull()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var svc = Svc(ctx);
        (await svc.FortsetzenSperreAsync(ids.WorkOperationId, ids.WorkplaceId)).Should().BeNull();

        await BdeBookingTestSeed.SetSageStatusAsync(ctx, ids.ProductionOrderId, "abgeholt");
        (await svc.FortsetzenSperreAsync(ids.WorkOperationId, ids.WorkplaceId)).Should().Contain("Keine BDE-Buchung mehr möglich").And.NotContain("—");
    }

    [Fact]
    public async Task ClosePaused_SetztBeendet_OhneMengen_MitAudit_NurHaengende()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var pause = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.AutoPaused, DateTime.Now.AddDays(-3), DateTime.Now.AddDays(-3).AddHours(2));
        var fortgesetzt = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused, DateTime.Now.AddHours(-3), DateTime.Now.AddHours(-2));
        var laeuft = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        ctx.BdeBookings.AddRange(pause, fortgesetzt, laeuft);
        await ctx.SaveChangesAsync();
        laeuft.ParentBookingId = fortgesetzt.Id;
        await ctx.SaveChangesAsync();
        var ende = pause.EndedAt;

        (await Svc(ctx).ClosePausedAsync(pause.Id)).Outcome.Should().Be(BdeBookingOutcome.Success);
        var b = ctx.BdeBookings.AsNoTracking().Single(x => x.Id == pause.Id);
        b.Status.Should().Be(BdeBookingStatus.Finished);
        b.EndedAt.Should().Be(ende, "die Zeit bis zur Pause bleibt, nichts kommt dazu");
        (b.ModifiedBy, b.ModifiedByWindows).Should().Be(("Test User", @"TEST\user"));
        b.ModifiedAt.Should().NotBeNull();
        ctx.BdeBookingQuantities.Count().Should().Be(0);

        (await Svc(ctx).ClosePausedAsync(fortgesetzt.Id)).Outcome.Should().Be(BdeBookingOutcome.InvalidState, "schon fortgesetzt");
        (await Svc(ctx).ClosePausedAsync(laeuft.Id)).Outcome.Should().Be(BdeBookingOutcome.InvalidState, "laeuft");
        (await BdeKopfBuchungen.HaengendeListeAsync(ctx, null)).Should().BeEmpty();
    }

    [Fact]
    public async Task HaengendeListe_AlleWerker_PausiertBisFiltert()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var alt = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.AutoPaused, new DateTime(2026, 7, 3, 8, 0, 0), new DateTime(2026, 7, 3, 9, 0, 0));
        var neu = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Activity, BdeBookingStatus.Paused, DateTime.Now.AddHours(-2), DateTime.Now.AddHours(-1));
        neu.BdeOperatorId = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        ctx.BdeBookings.AddRange(alt, neu);
        await ctx.SaveChangesAsync();

        (await BdeKopfBuchungen.HaengendeListeAsync(ctx, null)).Select(p => p.BookingId).Should().Equal(alt.Id, neu.Id);
        var bis = (await BdeKopfBuchungen.HaengendeListeAsync(ctx, new DateTime(2026, 7, 3))).Single();
        bis.Status.Should().Be("automatisch pausiert");
        BdeKopfBuchungen.HaengendeSpalten["pausiert-seit"](bis).Should().Be("03.07.2026 09:00");
    }

    [Fact]
    public void AdminAbschliessen_NurBdeAdmin_ListeAbSchichtleiter()
    {
        typeof(BdeBookingsController).GetMethod(nameof(BdeBookingsController.PauseAbschliessen))!
            .GetCustomAttributes(typeof(RequireBdeAdminAccessAttribute), true).Should().NotBeEmpty();
        typeof(BdeBookingsController).GetCustomAttributes(typeof(RequireBdeShiftleadAccessAttribute), true).Should().NotBeEmpty();
    }
}
