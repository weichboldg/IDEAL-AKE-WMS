using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>
/// End-to-End-InMemory-Tests des FA-Zusatzinfo-Syncs (v1.26.0, Spec §4/§6).
/// Reader ist gefaked (raw SQL = Manual-UAT), Entscheidungs- UND Schreibpfad
/// laufen ueber den echten EF-Service (kein Deko-Helper).
/// </summary>
public class FaZusatzinfoSyncServiceTests
{
    private const string SyncUser = "FaZusatzinfoSync";

    private static (FaZusatzinfoSyncService svc, FakeSageZusatzinfoReader reader,
                    IdealAkeWms.Data.ApplicationDbContext ctx, FakeSyncLogger fakeLogger,
                    Mock<ISyncErrorNotifier> notifier)
        Build()
    {
        var ctx = TestDbContextFactory.Create();
        var reader = new FakeSageZusatzinfoReader();
        var fakeLogger = new FakeSyncLogger();
        var notifier = new Mock<ISyncErrorNotifier>();
        var svc = new FaZusatzinfoSyncService(
            ctx, reader, notifier.Object, NullLogger<FaZusatzinfoSyncService>.Instance, fakeLogger);
        return (svc, reader, ctx, fakeLogger, notifier);
    }

    private static ProductionOrder SeedOrder(IdealAkeWms.Data.ApplicationDbContext ctx, string orderNumber,
        bool isDone = false, bool isCancelled = false)
    {
        var order = new ProductionOrder
        {
            OrderNumber = orderNumber,
            IsDone = isDone,
            IsCancelled = isCancelled,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionOrders.Add(order);
        ctx.SaveChanges();
        return order;
    }

    private static ProductionOrderExtraInfo SeedExtraInfo(
        IdealAkeWms.Data.ApplicationDbContext ctx, int productionOrderId,
        string? kaelte = "R290", string? ventil = "Danfoss", string? ausfuehrung = "E",
        string? maschine = "M1", string? status = "in Produktion")
    {
        var info = new ProductionOrderExtraInfo
        {
            ProductionOrderId = productionOrderId,
            Kaeltemittel = kaelte,
            Ventil = ventil,
            AusfuehrungEZ = ausfuehrung,
            Maschine = maschine,
            SageStatus = status,
            CreatedAt = DateTime.Now,
            CreatedBy = SyncUser,
            CreatedByWindows = SyncUser
        };
        ctx.ProductionOrderExtraInfos.Add(info);
        ctx.SaveChanges();
        return info;
    }

    private static ProductionOrderPickingStatus SeedPickingStatus(
        IdealAkeWms.Data.ApplicationDbContext ctx, int productionOrderId, bool isDonePicking)
    {
        var ps = new ProductionOrderPickingStatus
        {
            ProductionOrderId = productionOrderId,
            IsDonePicking = isDonePicking,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionOrderPickingStatuses.Add(ps);
        ctx.SaveChanges();
        return ps;
    }

    private static SageZusatzinfoRow Row(string wa, string? kaelte = "R290", string? ventil = "Danfoss",
        string? ausfuehrung = "E", string? maschine = "M1", string? status = "in Produktion")
        => new(wa, kaelte, ventil, ausfuehrung, maschine, status);

    // 1) Neu: WA mit FA-Treffer, kein Satellit -> Insert
    [Fact]
    public async Task Sync_NewWa_InsertsSatellite()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        reader.Rows = new() { Row("WA-1") };

        var result = await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        result.Inserted.Should().Be(1);
        result.Updated.Should().Be(0);
        var info = ctx.ProductionOrderExtraInfos.Single();
        info.ProductionOrderId.Should().Be(order.Id);
        info.Kaeltemittel.Should().Be("R290");
        info.Ventil.Should().Be("Danfoss");
        info.AusfuehrungEZ.Should().Be("E");
        info.Maschine.Should().Be("M1");
        info.SageStatus.Should().Be("in Produktion");
        info.CreatedBy.Should().Be(SyncUser);
        info.CreatedByWindows.Should().Be(SyncUser);
        info.ModifiedAt.Should().BeNull();

        fakeLogger.Runs[0].ServiceName.Should().Be("FaZusatzinfo");
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts!["gelesen"].Should().Be(1);
        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(1);
        fakeLogger.Runs[0].FinalCounts!["aktualisiert"].Should().Be(0);
        fakeLogger.Runs[0].FinalCounts!["uebersprungen"].Should().Be(0);
    }

    // 2) Aktualisiert: Satellit vorhanden, mindestens 1 Feld geaendert -> Update + Modified*
    [Fact]
    public async Task Sync_ChangedField_UpdatesSatellite_SetsModified()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedExtraInfo(ctx, order.Id, kaelte: "R134a");
        reader.Rows = new() { Row("WA-1", kaelte: "R290") };

        var result = await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        result.Updated.Should().Be(1);
        result.Inserted.Should().Be(0);
        var info = ctx.ProductionOrderExtraInfos.Single();
        info.Kaeltemittel.Should().Be("R290");
        info.ModifiedAt.Should().NotBeNull();
        info.ModifiedBy.Should().Be(SyncUser);
        info.ModifiedByWindows.Should().Be(SyncUser);
        fakeLogger.Runs[0].FinalCounts!["aktualisiert"].Should().Be(1);
        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(0);
    }

    // 3) Unveraendert: kein Write, kein Count
    [Fact]
    public async Task Sync_Unchanged_NoWrite_NoCount()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedExtraInfo(ctx, order.Id);
        reader.Rows = new() { Row("WA-1") };

        var result = await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        result.Inserted.Should().Be(0);
        result.Updated.Should().Be(0);
        ctx.ProductionOrderExtraInfos.Single().ModifiedAt.Should().BeNull();
        fakeLogger.Runs[0].FinalCounts!["gelesen"].Should().Be(1);
        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(0);
        fakeLogger.Runs[0].FinalCounts!["aktualisiert"].Should().Be(0);
    }

    // 4) Kein FA-Treffer: uebersprungen-Count, KEINE Warn-Zeile je WA (alte WAs sind normal)
    [Fact]
    public async Task Sync_WaWithoutFa_CountsSkipped_NoWarnPerWa()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        reader.Rows = new() { Row("WA-UNBEKANNT") };

        var result = await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        result.Inserted.Should().Be(0);
        ctx.ProductionOrderExtraInfos.Should().BeEmpty();
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts!["uebersprungen"].Should().Be(1);
        fakeLogger.Runs[0].Events.Should().NotContain(e => e.Level == "Warning");
    }

    // 5) IDEAL-Fall: mehrere ProductionOrders je WA-Nummer -> Upsert je gematchter Id
    //    (InMemory enforced den UNIQUE-Index auf OrderNumber nicht — genau richtig,
    //     um die IDEAL-Linie mit SubOrder-Zeilen zu simulieren.)
    [Fact]
    public async Task Sync_MultipleFasPerWa_UpsertsEachMatch()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var o1 = SeedOrder(ctx, "WA-1");
        var o2 = SeedOrder(ctx, "WA-1");
        reader.Rows = new() { Row("WA-1") };

        var result = await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        result.Inserted.Should().Be(2);
        ctx.ProductionOrderExtraInfos.Should().HaveCount(2);
        ctx.ProductionOrderExtraInfos.Select(i => i.ProductionOrderId)
            .Should().BeEquivalentTo(new[] { o1.Id, o2.Id });
        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(2);
    }

    // 6) DryRun: voller Plan mit echten Would-be-Counts, aber KEIN Write (Reconciler-Muster)
    [Fact]
    public async Task Sync_DryRun_RealCounts_NoWrites()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var oNew = SeedOrder(ctx, "WA-NEU");
        var oChanged = SeedOrder(ctx, "WA-GEAENDERT");
        SeedExtraInfo(ctx, oChanged.Id, kaelte: "R134a");
        reader.Rows = new() { Row("WA-NEU"), Row("WA-GEAENDERT", kaelte: "R290") };

        var result = await svc.SyncAsync(dryRun: true, autoDoneMaxPerRun: 100);

        result.Inserted.Should().Be(1);
        result.Updated.Should().Be(1);
        ctx.ProductionOrderExtraInfos.Should().HaveCount(1);            // kein Insert
        ctx.ProductionOrderExtraInfos.Single().Kaeltemittel.Should().Be("R134a"); // kein Update
        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(1);
        fakeLogger.Runs[0].FinalCounts!["aktualisiert"].Should().Be(1);
        fakeLogger.Runs[0].FinalMessageSuffix.Should().Be("[DryRun]");
    }

    // 7) View fehlt: Warn-Zeile + regulaeres Lauf-Ende (FinishSuccess), kein Throw
    [Fact]
    public async Task Sync_ViewMissing_WarnsAndFinishesSuccess()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        reader.ViewExists = false;

        var act = async () => await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        await act.Should().NotThrowAsync();
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
        fakeLogger.Runs[0].FinishedFailed.Should().BeFalse();
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("nicht vorhanden"));
        fakeLogger.Runs[0].FinalCounts!["gelesen"].Should().Be(0);
        fakeLogger.Runs[0].FinalCounts!["uebersprungen"].Should().Be(0);
    }

    // 8) Fehlerpfad: Reader wirft -> LogError + FinishFailed + rethrow
    [Fact]
    public async Task Sync_ReaderThrows_FinishesFailed_AndRethrows()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        reader.ThrowOnRead = new InvalidOperationException("Sage down");

        var act = async () => await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Sage down");
        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
        fakeLogger.Runs[0].FinalErrorMessage.Should().Be("Sage down");
        fakeLogger.Runs[0].Events.Should().Contain(e => e.Level == "Error");
    }

    // ===== Fold 2 (Spec §10.1-§10.4): Auto-Erledigt bei Sage-Status verpackt/abgeholt =====

    // §10.4-1) Status abgeholt + FA offen -> IsDonePicking=true, Audit, Count, Info-Zeile mit reference
    [Fact]
    public async Task AutoDone_StatusAbgeholt_OpenFa_SetsIsDonePicking()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        var ps = ctx.ProductionOrderPickingStatuses.Single();
        ps.IsDonePicking.Should().BeTrue();
        ps.ModifiedAt.Should().NotBeNull();
        ps.ModifiedBy.Should().Be(SyncUser);
        ps.ModifiedByWindows.Should().Be(SyncUser);
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Info"
            && e.Message.Contains("auf erledigt gesetzt")
            && e.Message.Contains("abgeholt")
            && e.Reference == "WA-1");
    }

    // §10.4-2) Status verpackt -> dito
    [Fact]
    public async Task AutoDone_StatusVerpackt_OpenFa_SetsIsDonePicking()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "verpackt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
    }

    // §10.4-3) FA bereits IsDonePicking -> kein Write, kein Count
    [Fact]
    public async Task AutoDone_AlreadyDonePicking_NoWrite_NoCount()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: true);
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        var ps = ctx.ProductionOrderPickingStatuses.Single();
        ps.ModifiedAt.Should().BeNull(); // kein Blind-Update
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
    }

    // §10.4-4) Sage-IsDone -> kein Write, kein Count (Sage-IsDone wird NIE beschrieben)
    [Fact]
    public async Task AutoDone_SageIsDone_NoWrite_NoCount()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1", isDone: true);
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeFalse();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
    }

    // §10.4-5) IsCancelled -> kein Write, kein Count (reaktivierte FA soll nicht dauerhaft versteckt zurueckkommen)
    [Fact]
    public async Task AutoDone_Cancelled_NoWrite_NoCount()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1", isCancelled: true);
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeFalse();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
    }

    // §10.4-6) Anderer Status -> nichts
    [Fact]
    public async Task AutoDone_OtherStatus_NoWrite()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "in Produktion") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeFalse();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
    }

    // §10.4-7) Rueckfall: bereits erledigt + Status jetzt "in Produktion" -> bleibt erledigt (Einweg)
    [Fact]
    public async Task AutoDone_StatusFellBack_StaysDone()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: true);
        SeedExtraInfo(ctx, order.Id, status: "abgeholt");
        reader.Rows = new() { Row("WA-1", status: "in Produktion") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeTrue();
        ctx.ProductionOrderExtraInfos.Single().SageStatus.Should().Be("in Produktion"); // Satellit folgt Sage
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
    }

    // §10.4-8) Ping-Pong: manuell wieder geoeffnet + Status weiterhin abgeholt -> wird erneut geschlossen
    [Fact]
    public async Task AutoDone_PingPong_ReclosesAfterManualReopen()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        var ps = SeedPickingStatus(ctx, order.Id, isDonePicking: false); // manuell wieder geoeffnet
        ps.ModifiedBy = "handbenutzer";
        ctx.SaveChanges();
        SeedExtraInfo(ctx, order.Id, status: "abgeholt");
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        var after = ctx.ProductionOrderPickingStatuses.Single();
        after.IsDonePicking.Should().BeTrue();
        after.ModifiedBy.Should().Be(SyncUser); // Sync hat erneut geschlossen
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
    }

    // §10.4-9) DryRun: Count gesetzt, DB unveraendert (weder Flag noch neue Zeile)
    [Fact]
    public async Task AutoDone_DryRun_Counts_NoWrites()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var withPs = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, withPs.Id, isDonePicking: false);
        var withoutPs = SeedOrder(ctx, "WA-2");
        reader.Rows = new() { Row("WA-1", status: "abgeholt"), Row("WA-2", status: "verpackt") };

        await svc.SyncAsync(dryRun: true, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Should().HaveCount(1);        // keine neue Zeile
        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeFalse(); // kein Flag
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(2); // echte Would-be-Zahl
        fakeLogger.Runs[0].FinalMessageSuffix.Should().Be("[DryRun]");
    }

    // §10.4-10) PickingStatus-Zeile fehlt (Altbestand) -> wird mit IsDonePicking=true angelegt
    [Fact]
    public async Task AutoDone_MissingPickingStatusRow_CreatesRow()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        var ps = ctx.ProductionOrderPickingStatuses.Single();
        ps.ProductionOrderId.Should().Be(order.Id);
        ps.IsDonePicking.Should().BeTrue();
        ps.CreatedBy.Should().Be(SyncUser);
        ps.CreatedByWindows.Should().Be(SyncUser);
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
    }

    // §10.4-11) Case/Trim: " Abgeholt " -> greift
    [Fact]
    public async Task AutoDone_CaseAndTrim_Matches()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: " Abgeholt ") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
    }

    // §10.4-12) Unveraendert-Zweig: ExtraInfo identisch + Status abgeholt + FA offen -> trotzdem erledigt
    //           (die if/else-if-Upsert-Kette hat kein continue — der Done-Check laeuft IMMER)
    [Fact]
    public async Task AutoDone_UnchangedUpsertBranch_StillSetsDone()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        SeedExtraInfo(ctx, order.Id, status: "abgeholt"); // identisch zur Row unten
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(0);
        fakeLogger.Runs[0].FinalCounts!["aktualisiert"].Should().Be(0); // Upsert unveraendert
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeTrue();
    }

    // §10.4-13) Mehrfach-Treffer-Mischfall: eine WA -> 2 FAs, eine bereits erledigt, eine offen
    [Fact]
    public async Task AutoDone_MultiFaPerWa_OnlyOpenOneIsSet()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var doneFa = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, doneFa.Id, isDonePicking: true);
        var openFa = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, openFa.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single(p => p.ProductionOrderId == openFa.Id)
            .IsDonePicking.Should().BeTrue();
        ctx.ProductionOrderPickingStatuses.Single(p => p.ProductionOrderId == doneFa.Id)
            .ModifiedAt.Should().BeNull();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
    }

    // §10.4-14) Cap: Kandidaten > Cap -> kein Write, Warn-Zeile, Fehlermail, Counts
    [Fact]
    public async Task AutoDone_OverCap_NoWrites_Warns_Notifies()
    {
        var (svc, reader, ctx, fakeLogger, notifier) = Build();
        for (var i = 1; i <= 3; i++)
        {
            var order = SeedOrder(ctx, $"WA-{i}");
            SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        }
        reader.Rows = new()
        {
            Row("WA-1", status: "abgeholt"),
            Row("WA-2", status: "abgeholt"),
            Row("WA-3", status: "verpackt"),
        };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 2); // Cap+1 Kandidaten

        ctx.ProductionOrderPickingStatuses.Where(p => p.IsDonePicking).Should().BeEmpty();
        ctx.ProductionOrderExtraInfos.Should().HaveCount(3); // Upsert-Teil laeuft normal weiter
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue(); // kein throw
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("Auto-Erledigt uebersprungen")
            && e.Message.Contains("3") && e.Message.Contains("2"));
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
        fakeLogger.Runs[0].FinalCounts!["erledigt-kandidaten"].Should().Be(3);
        notifier.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<Exception>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
