using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// BDE Stufe 1 (Spec 2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung, v1.63.0): Familien-Pruefung beim Buchen (FA-a9,
/// AK 15/21-24, Antwort 12), Start-Sperre und Fertigmeldung ohne Start/Stop (FA-c4..c7, AK 27-33).
/// Seed-Werkbank „A" (BdeBookingTestSeed) ist die Terminal-Werkbank.
/// </summary>
public class BdeBookingServiceStufe1Tests
{
    private const string Fremd = "Dieser Arbeitsgang gehört nicht zu dieser Werkbank.";

    private static BdeBookingService Svc(ApplicationDbContext ctx, bool nurFa = false, bool multiMa = false)
    {
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeNurFaMeldung)).ReturnsAsync(nurFa ? "true" : "false");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeMehrfachBuchungProArbeitsgang)).ReturnsAsync(multiMa ? "true" : "false");
        return new BdeBookingService(ctx, new FakeCurrentUserService(), settings.Object);
    }

    private static async Task<int> AddWorkplaceAsync(ApplicationDbContext ctx, string? code, bool bde = true, bool marked = false)
    {
        var wp = new ProductionWorkplace
        {
            Name = $"WB {code}", ArbeitsschrittCode = code, BdeAktiv = bde,
            SageMissingSince = marked ? new DateTime(2026, 10, 19) : null,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();
        return wp.Id;
    }

    private static async Task<int> AddWoAsync(ApplicationDbContext ctx, int poId, int? workplaceId, string op = "EG")
    {
        var wo = new WorkOperation
        {
            ProductionOrderId = poId, OperationNumber = op, Name = op, ProductionWorkplaceId = workplaceId, Sequence = 1,
            IsReportable = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.WorkOperations.Add(wo);
        await ctx.SaveChangesAsync();
        return wo.Id;
    }

    private static async Task SetAsync(ApplicationDbContext ctx, int workplaceId, Action<ProductionWorkplace> change)
    {
        change((await ctx.ProductionWorkplaces.FindAsync(workplaceId))!);
        await ctx.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- FA-a9

    [Fact]
    public async Task Start_ForeignAg_Rejected_NoBooking_RunningActivityStaysOpen_AK21()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var b = await AddWorkplaceAsync(ctx, null);           // AKE-artig: beide ohne Kuerzel
        var x = await AddWoAsync(ctx, ids.ProductionOrderId, b, "ST");
        var svc = Svc(ctx);
        (await svc.StartActivityAsync(ids.OperatorId, ids.ActivityId, ids.WorkplaceId, ids.TerminalId)).Outcome.Should().Be(BdeBookingOutcome.Success);

        foreach (var r in new[]
                 {
                     await svc.StartProductionAsync(ids.OperatorId, x, ids.WorkplaceId, ids.TerminalId),
                     await svc.StartSetupAsync(ids.OperatorId, x, ids.WorkplaceId, ids.TerminalId),
                 })
        {
            r.Outcome.Should().Be(BdeBookingOutcome.InvalidState);
            r.Message.Should().Be(Fremd);
        }

        (await ctx.BdeBookings.CountAsync()).Should().Be(1);
        (await ctx.BdeBookings.SingleAsync()).EndedAt.Should().BeNull(); // kein Auto-Close der Taetigkeit
    }

    [Fact]
    public async Task Resume_ForeignAg_Rejected_ParentStaysPaused_ActivityResumable_AK22()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var b = await AddWorkplaceAsync(ctx, "ST");
        var x = await AddWoAsync(ctx, ids.ProductionOrderId, b, "ST");
        var svc = Svc(ctx);
        var started = await svc.StartProductionAsync(ids.OperatorId, x, b, ids.TerminalId);
        await svc.PauseAsync(started.Booking!.Id);

        var r = await svc.ResumeAsync(started.Booking.Id, ids.OperatorId, BdeBookingType.Production, ids.WorkplaceId, ids.TerminalId);

        r.Message.Should().Be(Fremd);
        (await ctx.BdeBookings.FindAsync(started.Booking.Id))!.Status.Should().Be(BdeBookingStatus.Paused);
        (await ctx.BdeBookings.CountAsync()).Should().Be(1);
        // an der eigenen Werkbank (B) geht es
        (await svc.ResumeAsync(started.Booking.Id, ids.OperatorId, BdeBookingType.Production, b, ids.TerminalId)).Outcome
            .Should().Be(BdeBookingOutcome.Success);

        // pausierte Taetigkeit (ohne AG) bleibt an jeder Werkbank fortsetzbar
        await svc.FinishAsync((await ctx.BdeBookings.SingleAsync(x2 => x2.EndedAt == null)).Id, 1, 0);
        var act = await svc.StartActivityAsync(ids.OperatorId, ids.ActivityId, b, ids.TerminalId);
        await svc.PauseAsync(act.Booking!.Id);
        (await svc.ResumeAsync(act.Booking.Id, ids.OperatorId, BdeBookingType.Activity, ids.WorkplaceId, ids.TerminalId)).Outcome
            .Should().Be(BdeBookingOutcome.Success);
    }

    [Fact]
    public async Task Familie_AgOfSameCodeWorkplace_Accepted_BookingStoresTerminalWorkplace_AK15_AK24()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        await SetAsync(ctx, ids.WorkplaceId, w => w.ArbeitsschrittCode = "EG");
        var w19 = await AddWorkplaceAsync(ctx, " eg ", bde: false, marked: true); // Altbestand W1
        var y = await AddWoAsync(ctx, ids.ProductionOrderId, w19);
        var svc = Svc(ctx);

        var start = await svc.StartProductionAsync(ids.OperatorId, y, ids.WorkplaceId, ids.TerminalId);
        start.Outcome.Should().Be(BdeBookingOutcome.Success);
        start.Booking!.ProductionWorkplaceId.Should().Be(ids.WorkplaceId);

        await svc.PauseAsync(start.Booking.Id);
        (await svc.ResumeAsync(start.Booking.Id, ids.OperatorId, BdeBookingType.Production, ids.WorkplaceId, ids.TerminalId)).Outcome
            .Should().Be(BdeBookingOutcome.Success);
    }

    [Fact]
    public async Task UnknownWorkOperation_Rejected_AK24()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var r = await Svc(ctx).StartProductionAsync(ids.OperatorId, 999_999, ids.WorkplaceId, ids.TerminalId);

        r.Message.Should().Be(Fremd);
        (await ctx.BdeBookings.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task OwnWorkplaceWithoutCode_StillAccepted_Ake()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        (await Svc(ctx).StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId)).Outcome
            .Should().Be(BdeBookingOutcome.Success);
    }

    // Antwort 12: im NurFA-Modus zaehlt ProductionOrder.ProductionWorkplaceId statt der Kuerzel-Familie.
    [Fact]
    public async Task NurFa_ChecksOrderWorkplace_NotAgWorkplace()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var b = await AddWorkplaceAsync(ctx, null);
        var po = await ctx.ProductionOrders.FindAsync(ids.ProductionOrderId);
        po!.ProductionWorkplaceId = ids.WorkplaceId; // FA nach dem ersten Start auf A umgehaengt …
        await ctx.SaveChangesAsync();
        var agAtB = await AddWoAsync(ctx, ids.ProductionOrderId, b, "01"); // … „01" haengt noch an B
        var svc = Svc(ctx, nurFa: true);

        (await svc.StartProductionAsync(ids.OperatorId, agAtB, ids.WorkplaceId, ids.TerminalId)).Outcome.Should().Be(BdeBookingOutcome.Success);

        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        (await svc.StartProductionAsync(op2, agAtB, b, ids.TerminalId)).Message.Should().Be(Fremd); // FA gehoert zu A

        po.ProductionWorkplaceId = null;
        await ctx.SaveChangesAsync();
        var op3 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        (await svc.StartProductionAsync(op3, agAtB, ids.WorkplaceId, ids.TerminalId)).Message.Should().Be(Fremd); // FA ohne Werkbank
    }

    // ---------------------------------------------------------------- (c) nur AG-Fertigmeldung

    private static async Task<(BdeBookingTestSeed.Ids Ids, BdeBookingService Svc)> FertigmeldungAsync(
        ApplicationDbContext ctx, bool nurFa = false, bool multiMa = false)
    {
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        await SetAsync(ctx, ids.WorkplaceId, w => w.BdeNurAgFertigmeldung = true);
        return (ids, Svc(ctx, nurFa, multiMa));
    }

    [Fact]
    public async Task ReportCompletion_StoresFinishedProductionWithZeroDuration_AndFinalQuantity_AK27()
    {
        using var ctx = TestDbContextFactory.Create();
        var (ids, svc) = await FertigmeldungAsync(ctx);

        var r = await svc.ReportCompletionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 10, 1);

        r.Outcome.Should().Be(BdeBookingOutcome.Success);
        var b = await ctx.BdeBookings.Include(x => x.Quantities).SingleAsync();
        b.BookingType.Should().Be(BdeBookingType.Production);
        b.Status.Should().Be(BdeBookingStatus.Finished);
        b.EndedAt.Should().Be(b.StartedAt);
        b.ProductionWorkplaceId.Should().Be(ids.WorkplaceId);
        b.BdeOperatorId.Should().Be(ids.OperatorId);
        var q = b.Quantities.Single();
        q.IsFinal.Should().BeTrue();
        q.GoodQuantity.Should().Be(10);
        q.ScrapQuantity.Should().Be(1);
    }

    [Fact]
    public async Task ReportCompletion_SecondTime_AlreadyReported_AK28()
    {
        using var ctx = TestDbContextFactory.Create();
        var (ids, svc) = await FertigmeldungAsync(ctx);
        await svc.ReportCompletionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 10, 0);

        var r = await svc.ReportCompletionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 10, 0);

        r.Message.Should().Be("Arbeitsgang ist bereits fertig gemeldet.");
        (await ctx.BdeBookings.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, 0)]
    public async Task ReportCompletion_WithoutQuantity_Rejected_AK29(int? good, int? scrap)
    {
        using var ctx = TestDbContextFactory.Create();
        var (ids, svc) = await FertigmeldungAsync(ctx);

        var r = await svc.ReportCompletionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, good, scrap);

        r.Outcome.Should().Be(BdeBookingOutcome.InvalidState);
        (await ctx.BdeBookings.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ReportCompletion_PackedOrder_Rejected_AK29()
    {
        using var ctx = TestDbContextFactory.Create();
        var (ids, svc) = await FertigmeldungAsync(ctx);
        await BdeBookingTestSeed.SetSageStatusAsync(ctx, ids.ProductionOrderId, "verpackt");

        var r = await svc.ReportCompletionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 1, 0);

        r.Message.Should().Contain("keine BDE-Buchung mehr moeglich");
    }

    [Fact]
    public async Task ReportCompletion_ForeignAg_Rejected_NoBookingNoQuantity_AK23()
    {
        using var ctx = TestDbContextFactory.Create();
        var (ids, svc) = await FertigmeldungAsync(ctx);
        var b = await AddWorkplaceAsync(ctx, "ST");
        var x = await AddWoAsync(ctx, ids.ProductionOrderId, b, "ST");

        (await svc.ReportCompletionAsync(ids.OperatorId, x, ids.WorkplaceId, ids.TerminalId, 1, 0)).Message.Should().Be(Fremd);
        (await ctx.BdeBookings.AnyAsync()).Should().BeFalse();
        (await ctx.BdeBookingQuantities.AnyAsync()).Should().BeFalse();
    }

    [Theory]
    [InlineData(false, BdeBookingOutcome.CollisionOtherOperator, 1)]
    [InlineData(true, BdeBookingOutcome.Success, 2)]
    public async Task ReportCompletion_OtherOperatorRunning_FollowsStartRule_AK30(bool multiMa, BdeBookingOutcome expected, int bookings)
    {
        using var ctx = TestDbContextFactory.Create();
        var (ids, svc) = await FertigmeldungAsync(ctx, multiMa: multiMa);
        var other = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        var normal = await AddWorkplaceAsync(ctx, null);
        ctx.BdeBookings.Add(new BdeBooking
        {
            BdeOperatorId = other, WorkOperationId = ids.WorkOperationId, ProductionWorkplaceId = normal, BdeTerminalId = ids.TerminalId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-5),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var r = await svc.ReportCompletionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 10, 0);

        r.Outcome.Should().Be(expected);
        (await ctx.BdeBookings.CountAsync()).Should().Be(bookings);
    }

    [Fact]
    public async Task ReportCompletion_OwnRunningBooking_Rejected()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var svc = Svc(ctx);
        await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        await SetAsync(ctx, ids.WorkplaceId, w => w.BdeNurAgFertigmeldung = true);

        (await svc.ReportCompletionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 10, 0)).Message
            .Should().Be("Dieser Arbeitsgang läuft bei Ihnen — bitte dort beenden.");
    }

    [Fact]
    public async Task StartAndResume_BlockedAtFertigmeldungWorkplace_ActivityAllowed_AK31()
    {
        using var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var normalSvc = Svc(ctx);
        var started = await normalSvc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        await normalSvc.PauseAsync(started.Booking!.Id);
        await SetAsync(ctx, ids.WorkplaceId, w => w.BdeNurAgFertigmeldung = true);
        var svc = Svc(ctx);
        const string gesperrt = "An dieser Werkbank wird nur fertig gemeldet.";

        (await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId)).Message.Should().Be(gesperrt);
        (await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId)).Message.Should().Be(gesperrt);
        (await svc.ResumeAsync(started.Booking.Id, ids.OperatorId, BdeBookingType.Production, ids.WorkplaceId, ids.TerminalId)).Message
            .Should().Be(gesperrt);
        (await svc.StartActivityAsync(ids.OperatorId, ids.ActivityId, ids.WorkplaceId, ids.TerminalId)).Outcome.Should().Be(BdeBookingOutcome.Success);
    }

    [Fact]
    public async Task NurFaGlobal_OptionWithoutEffect_ReportCompletionRejected_StartAllowed_AK32()
    {
        using var ctx = TestDbContextFactory.Create();
        var (ids, svc) = await FertigmeldungAsync(ctx, nurFa: true);
        var po = await ctx.ProductionOrders.FindAsync(ids.ProductionOrderId);
        po!.ProductionWorkplaceId = ids.WorkplaceId;
        await ctx.SaveChangesAsync();

        (await svc.ReportCompletionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 1, 0)).Message
            .Should().Be("Diese Werkbank meldet nicht ohne Zeit fertig.");
        (await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId)).Outcome
            .Should().Be(BdeBookingOutcome.Success);
    }

    [Fact]
    public async Task ReportCompletion_ZeroDuration_DoesNotChangeParallelEffectiveDuration_AK33()
    {
        using var ctx = TestDbContextFactory.Create();
        var (ids, svc) = await FertigmeldungAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);
        // Parallele Taetigkeit um die Fertigmeldung herum (Zeitpunkt = jetzt)
        var t = DateTime.Now;
        var start = t.AddMinutes(-30) > DateTime.Today ? t.AddMinutes(-30) : DateTime.Today;
        var end = t.AddMinutes(30);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Activity, BdeBookingStatus.Finished,
            startedAt: start, endedAt: end));
        await ctx.SaveChangesAsync();
        await svc.ReportCompletionAsync(ids.OperatorId, wo2, ids.WorkplaceId, ids.TerminalId, 5, 0);

        var split = await new BdeTimeSplitService(ctx).ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        split.Should().ContainSingle(); // Fertigmeldung (Dauer 0) faellt heraus
        split[0].EffectiveDuration.Should().Be((end < DateTime.Today.AddDays(1) ? end : DateTime.Today.AddDays(1)) - start);
    }
}
