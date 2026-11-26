using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.81 (Fix-Lauf, Entscheid 4 / F-22 / 3A): Gut 0 + Ausschuss 0 als Abschluss nur nach mindestens einer Teilmenge
/// am Arbeitsgang – dieselbe Regel in FinishAsync, FinishGroupAsync und ReportCompletionCoreAsync.</summary>
public class BdeMengenRegelTests
{
    private static BdeBookingService Svc(Data.ApplicationDbContext ctx) =>
        new(ctx, new FakeCurrentUserService(), Mock.Of<IdealAkeWms.Data.Repositories.IAppSettingRepository>());

    private static async Task<(BdeBookingTestSeed.Ids ids, BdeBooking b1, BdeBooking b2)> ZweiLaufendeAsync(Data.ApplicationDbContext ctx)
    {
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);
        var b1 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        var b2 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-30), workOperationId: wo2);
        ctx.BdeBookings.AddRange(b1, b2);
        await ctx.SaveChangesAsync();
        return (ids, b1, b2);
    }

    // ---- Gruppe (Task 2) ----

    [Fact]
    public async Task FinishGroup_ZeileOhneMenge_Abgewiesen_NichtsGeaendert()
    {
        var ctx = TestDbContextFactory.Create();
        var (ids, b1, b2) = await ZweiLaufendeAsync(ctx);

        var r = await Svc(ctx).FinishGroupAsync(ids.OperatorId, new List<GroupFinishEntry> { new(b1.Id, 5m, 0m, true), new(b2.Id, null, null, true) });

        r.IsSuccess.Should().BeFalse();
        r.ErrorMessage.Should().Contain("Gut- oder Ausschussmenge");
        ctx.BdeBookings.AsNoTracking().Count(b => b.Status == BdeBookingStatus.Finished).Should().Be(0);
        ctx.BdeBookingQuantities.Count().Should().Be(0);
    }

    [Fact]
    public async Task FinishGroup_00_OhneTeilmenge_Abgewiesen()
    {
        var ctx = TestDbContextFactory.Create();
        var (ids, b1, b2) = await ZweiLaufendeAsync(ctx);

        var r = await Svc(ctx).FinishGroupAsync(ids.OperatorId, new List<GroupFinishEntry> { new(b1.Id, 5m, 0m, true), new(b2.Id, 0m, 0m, true) });

        r.IsSuccess.Should().BeFalse();
        r.ErrorMessage.Should().Contain("Teilmeldung");
        ctx.BdeBookingQuantities.Count().Should().Be(0);
    }

    [Fact]
    public async Task FinishGroup_00_NachTeilmenge_ErzeugtIsFinal()
    {
        var ctx = TestDbContextFactory.Create();
        var (ids, b1, b2) = await ZweiLaufendeAsync(ctx);
        var svc = Svc(ctx);
        (await svc.ReportPartialQuantityAsync(b2.Id, 4m, 0m)).Outcome.Should().Be(BdeBookingOutcome.Success);

        var r = await svc.FinishGroupAsync(ids.OperatorId, new List<GroupFinishEntry> { new(b1.Id, 5m, 0m, true), new(b2.Id, 0m, 0m, true) });

        r.IsSuccess.Should().BeTrue();
        ctx.BdeBookingQuantities.Count(q => q.BdeBookingId == b2.Id && q.IsFinal).Should().Be(1);
    }

    // ---- FA-Sicht Fertig (Task 3) ----

    [Fact]
    public async Task Fertig_00_OhneTeilmenge_Abgewiesen()
    {
        var ctx = TestDbContextFactory.Create();
        var (_, b1, _) = await ZweiLaufendeAsync(ctx);

        var r = await Svc(ctx).FinishAsync(b1.Id, 0m, 0m);

        r.Outcome.Should().Be(BdeBookingOutcome.InvalidState);
        ctx.BdeBookings.AsNoTracking().Single(b => b.Id == b1.Id).Status.Should().Be(BdeBookingStatus.Running);
    }

    [Fact]
    public async Task Fertig_00_NachTeilmenge_ErzeugtIsFinal()
    {
        var ctx = TestDbContextFactory.Create();
        var (_, b1, _) = await ZweiLaufendeAsync(ctx);
        var svc = Svc(ctx);
        await svc.ReportPartialQuantityAsync(b1.Id, 10m, 0m);

        var r = await svc.FinishAsync(b1.Id, 0m, 0m);

        r.Outcome.Should().Be(BdeBookingOutcome.Success);
        ctx.BdeBookingQuantities.Count(q => q.BdeBookingId == b1.Id && q.IsFinal).Should().Be(1);
    }

    [Fact]
    public async Task Teilmenge_ErreichtSoll_ErfolgMitHinweis()
    {
        var ctx = TestDbContextFactory.Create();
        var (_, b1, _) = await ZweiLaufendeAsync(ctx);   // Soll 10
        var svc = Svc(ctx);

        (await svc.ReportPartialQuantityAsync(b1.Id, 4m, 0m)).Message.Should().BeNull();
        var r = await svc.ReportPartialQuantityAsync(b1.Id, 6m, 1m);

        r.Outcome.Should().Be(BdeBookingOutcome.Success);
        r.Message.Should().Be("Sollmenge erreicht – bitte Fertig melden.");
    }

    // ---- Kern ReportCompletionCoreAsync (Baugruppe/Teileliste, F-22) ----

    private static async Task<BdeBookingTestSeed.Ids> NurAgWerkbankAsync(Data.ApplicationDbContext ctx)
    {
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        (await ctx.ProductionWorkplaces.FindAsync(ids.WorkplaceId))!.BdeNurAgFertigmeldung = true;
        await ctx.SaveChangesAsync();
        return ids;
    }

    [Fact]
    public async Task Kern_00_OhneTeilmenge_Abgewiesen()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await NurAgWerkbankAsync(ctx);

        var r = await Svc(ctx).ReportCompletionCoreAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 0m, 0m);

        r.Outcome.Should().Be(BdeBookingOutcome.InvalidState);
        ctx.BdeBookings.Count().Should().Be(0);
    }

    [Fact]
    public async Task Kern_Teil5_Dann00_ErzeugtIsFinal()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await NurAgWerkbankAsync(ctx);
        var svc = Svc(ctx);
        (await svc.ReportCompletionCoreAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 5m, 0m, istFinal: false))
            .Outcome.Should().Be(BdeBookingOutcome.Success);

        var r = await svc.ReportCompletionCoreAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 0m, 0m);

        r.Outcome.Should().Be(BdeBookingOutcome.Success);
        ctx.BdeBookingQuantities.Count(q => q.IsFinal && q.BdeBooking!.WorkOperationId == ids.WorkOperationId).Should().Be(1);
    }

    [Fact]
    public async Task Kern_00_Teilbuchung_Abgewiesen()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await NurAgWerkbankAsync(ctx);
        var svc = Svc(ctx);
        await svc.ReportCompletionCoreAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 5m, 0m, istFinal: false);

        var r = await svc.ReportCompletionCoreAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 0m, 0m, istFinal: false);

        r.Outcome.Should().Be(BdeBookingOutcome.InvalidState, "0/0 nur als Abschluss, nie als Teilmenge");
    }
}
