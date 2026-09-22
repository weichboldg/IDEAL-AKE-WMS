using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>
/// IDEAL: WorkOperation-Arbeitsgang-Erkennung aus der Struktur (2c-Umbau, v1.44.0, war
/// FaWorkStepStructureDetection/v1.41.0). Der Algorithmus ist InMemory-testbar (reine EF-LINQ). Die
/// datenabhaengigen und die Ausschlusslisten-AKs (AK 10, Liste liegt in ServiceSettings-DB) sind
/// zusaetzlich Manual-UAT (TS-77). Der Master-/Toggle-Gate (AK 18) sitzt im SyncWorker, nicht im Service.
/// </summary>
public class WorkOperationStructureDetectionServiceTests
{
    // Bei UNBEKANNTEN Token wird die Digest-Mail nur bei HasChanged==true versucht — und der Versand
    // liest die Mail-Konfiguration LIVE aus der SQL-Server-DB. Tests mit unbekannten Token stubben
    // daher HasChanged=false.
    private static readonly IUnknownArbeitsschrittTokenState NoDigest = BuildNoDigestState();

    private static IUnknownArbeitsschrittTokenState BuildNoDigestState()
    {
        var m = new Mock<IUnknownArbeitsschrittTokenState>();
        m.Setup(s => s.HasChanged(It.IsAny<IReadOnlyCollection<string>>())).Returns(false);
        return m.Object;
    }

    private static (WorkOperationStructureDetectionService Svc, FakeSyncLogger Log) Build(
        ApplicationDbContext ctx, IUnknownArbeitsschrittTokenState? state = null)
    {
        var log = new FakeSyncLogger();
        var svc = new WorkOperationStructureDetectionService(
            ctx, new ConfigurationBuilder().Build(), new Mock<IMailService>().Object,
            state ?? new UnknownArbeitsschrittTokenState(),
            NullLogger<WorkOperationStructureDetectionService>.Instance, log);
        return (svc, log);
    }

    private static FaHierarchyNode Node(int subFa, int? vaterFa, string? arbeitsschritte, int hauptFa = 9000)
        => new() { HauptFA = hauptFa, SubFA = subFa, VaterFA = vaterFa, Arbeitsschritte = arbeitsschritte, Sollmenge = 1 };

    private static ProductionWorkplace Workplace(string code, string name, string? nummer = null)
        => new() { ArbeitsschrittCode = code, Name = name, SageArbeitsplatznummer = nummer,
                   CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };

    private static ProductionOrder Order(string subFa, string hauptFa = "9000",
        bool isDone = false, bool isCancelled = false)
        => new() { OrderNumber = hauptFa, SubOrderNumber = subFa, Quantity = 1,
                   IsDone = isDone, IsCancelled = isCancelled,
                   CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };

    // --- AK 8 Grundfall ---
    [Fact]
    public async Task Detect_KnownToken_CreatesExactlyOneWorkOperation()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(subFa: 10, vaterFa: null, arbeitsschritte: "KA"));
        ctx.ProductionWorkplaces.Add(Workplace("KA", "Kanterei W1", "2300"));
        ctx.ProductionOrders.Add(Order("10"));
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx);
        var r = await svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(1);
        var wo = await ctx.WorkOperations.SingleAsync();
        wo.OperationNumber.Should().Be("KA");
        wo.Name.Should().Be("Kanterei W1");
        wo.ProductionWorkplaceId.Should().Be(ctx.ProductionWorkplaces.Single().Id);
        wo.IsReportable.Should().BeTrue();
        log.Runs.Single().FinalCounts!["neu"].Should().Be(1);
    }

    // --- Audit ---
    [Fact]
    public async Task Detect_CreatedRow_CarriesServiceNameAudit()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        ctx.ProductionWorkplaces.Add(Workplace("KA", "Kanterei W1"));
        ctx.ProductionOrders.Add(Order("10"));
        await ctx.SaveChangesAsync();

        await Build(ctx).Svc.DetectAsync(dryRun: false);

        var wo = await ctx.WorkOperations.SingleAsync();
        wo.CreatedBy.Should().Be("WorkOperationStructureDetection");
        wo.CreatedByWindows.Should().Be("WorkOperationStructureDetection");
    }

    // --- Token case-insensitiv + getrimmt ---
    [Fact]
    public async Task Detect_TokenCaseAndWhitespaceInsensitive_Matches()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "  ka   ls  "));
        ctx.ProductionWorkplaces.AddRange(Workplace("KA", "Kanterei W1"), Workplace("LS", "Stanzerei W1"));
        ctx.ProductionOrders.Add(Order("10"));
        await ctx.SaveChangesAsync();

        var r = await Build(ctx).Svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(2);
    }

    // --- AK 13 DirectChildren-Gegenprobe ---
    [Fact]
    public async Task Detect_GrandchildToken_LandsOnParentNotGrandparent()
    {
        using var ctx = TestDbContextFactory.Create();
        // X(10) -> Y(20) -> Z(0, Blatt). SÄ steht NUR auf Z; AV steht auf X.
        ctx.FaHierarchyNodes.AddRange(
            Node(subFa: 10, vaterFa: null, arbeitsschritte: "AV"),
            Node(subFa: 20, vaterFa: 10, arbeitsschritte: null),
            Node(subFa: 0, vaterFa: 20, arbeitsschritte: "SÄ"));
        ctx.ProductionWorkplaces.AddRange(Workplace("AV", "Vormontage"), Workplace("SÄ", "Schäumerei W1"));
        ctx.ProductionOrders.AddRange(Order("10"), Order("20"));
        await ctx.SaveChangesAsync();

        await Build(ctx).Svc.DetectAsync(dryRun: false);

        var byOrder = await ctx.WorkOperations.Include(w => w.ProductionOrder).ToListAsync();
        byOrder.Where(w => w.ProductionOrder.SubOrderNumber == "20").Select(w => w.OperationNumber)
            .Should().BeEquivalentTo(new[] { "SÄ" });
        byOrder.Where(w => w.ProductionOrder.SubOrderNumber == "10").Select(w => w.OperationNumber)
            .Should().BeEquivalentTo(new[] { "AV" });
    }

    // --- AK 12 Nur-hinzufuegen: (ProductionOrderId, OperationNumber) existiert -> uebersprungen ---
    [Fact]
    public async Task Detect_ExistingOperation_SkippedNoDuplicate()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        ctx.ProductionWorkplaces.Add(Workplace("KA", "Kanterei W1"));
        var order = Order("10");
        ctx.ProductionOrders.Add(order);
        await ctx.SaveChangesAsync();
        ctx.WorkOperations.Add(new WorkOperation
        {
            ProductionOrderId = order.Id, OperationNumber = "KA", Name = "Kanterei W1",
            Sequence = 0, IsReportable = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx);
        var r = await svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
        (await ctx.WorkOperations.CountAsync()).Should().Be(1);
        log.Runs.Single().FinalCounts!["uebersprungen"].Should().Be(1);
    }

    // --- AK 9 unbekanntes Kuerzel: melden, kein Insert ---
    [Fact]
    public async Task Detect_UnknownToken_ReportedNotInserted()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.AddRange(
            Node(10, null, "KA ZZZ"),   // ZZZ unbekannt
            Node(20, null, "ZZZ"));
        ctx.ProductionWorkplaces.Add(Workplace("KA", "Kanterei W1"));
        ctx.ProductionOrders.AddRange(Order("10"), Order("20"));
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx, NoDigest);
        var r = await svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(1); // nur KA auf FA 10
        var counts = log.Runs.Single().FinalCounts!;
        counts["arbeitsschritt_unbekannt"].Should().Be(1);           // ZZZ
        counts["arbeitsschritt_unbekannt_auftraege"].Should().Be(2); // FA 10 + FA 20
        log.Runs.Single().Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("ZZZ"));
    }

    // --- AK 11 Kuerzel-Mehrdeutigkeit: zwei Werkbaenke gleicher Code -> kein Insert, melden ---
    [Fact]
    public async Task Detect_AmbiguousCode_NotInsertedButReported()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        ctx.ProductionWorkplaces.AddRange(
            Workplace("KA", "Kanterei W1", "2300"),
            Workplace("KA", "Kanterei W2", "2301"));
        ctx.ProductionOrders.Add(Order("10"));
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx);
        var r = await svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
        (await ctx.WorkOperations.AnyAsync()).Should().BeFalse();
        var run = log.Runs.Single();
        run.FinalCounts!["arbeitsschritt_mehrdeutig"].Should().Be(1);
        run.FinalCounts!["arbeitsschritt_unbekannt"].Should().Be(0); // mehrdeutig != unbekannt
        run.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("Mehrdeutig"));
    }

    // --- AK 14 abgeschlossener Auftrag: IsDone ---
    [Fact]
    public async Task Detect_DoneOrder_GetsNoRow()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        ctx.ProductionWorkplaces.Add(Workplace("KA", "Kanterei W1"));
        ctx.ProductionOrders.Add(Order("10", isDone: true));
        await ctx.SaveChangesAsync();

        var r = await Build(ctx).Svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
    }

    // --- AK 14 IsDonePicking ---
    [Fact]
    public async Task Detect_DonePickingOrder_GetsNoRow()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        ctx.ProductionWorkplaces.Add(Workplace("KA", "Kanterei W1"));
        var order = Order("10");
        ctx.ProductionOrders.Add(order);
        await ctx.SaveChangesAsync();
        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus
        {
            ProductionOrderId = order.Id, IsDonePicking = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var r = await Build(ctx).Svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
    }

    // --- Fehlende Materialisierung: kein Fehler, keine Zeile ---
    [Fact]
    public async Task Detect_NodeWithoutMaterializedOrder_NoErrorNoRow()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA")); // kein passender ProductionOrder
        ctx.ProductionWorkplaces.Add(Workplace("KA", "Kanterei W1"));
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx);
        var r = await svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
        log.Runs.Single().FinishedSuccess.Should().BeTrue();
        (await ctx.WorkOperations.AnyAsync()).Should().BeFalse();
    }

    // --- DryRun zaehlt, schreibt nicht ---
    [Fact]
    public async Task Detect_DryRun_CountsButDoesNotWrite()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        ctx.ProductionWorkplaces.Add(Workplace("KA", "Kanterei W1"));
        ctx.ProductionOrders.Add(Order("10"));
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx);
        var r = await svc.DetectAsync(dryRun: true);

        r.Inserted.Should().Be(1);
        (await ctx.WorkOperations.AnyAsync()).Should().BeFalse();
        log.Runs.Single().FinalMessageSuffix.Should().Be("[DryRun]");
    }

    // --- SubFA==0 (Blatt) bekommt keine eigene Zeile ---
    [Fact]
    public async Task Detect_LeafNodeSubFaZero_IsNotItsOwnOrder()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(subFa: 0, vaterFa: 10, arbeitsschritte: "KA"));
        ctx.ProductionWorkplaces.Add(Workplace("KA", "Kanterei W1"));
        ctx.ProductionOrders.Add(Order("0"));
        await ctx.SaveChangesAsync();

        var r = await Build(ctx).Svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
    }

    // --- S1-Zustand (Sammelmail nur bei Mengenaenderung) — reine Logik ---
    [Fact]
    public void UnknownTokenState_ReportsChangeOnlyWhenSetChanges()
    {
        var state = new UnknownArbeitsschrittTokenState();
        state.HasChanged(new[] { "A", "B" }).Should().BeTrue();   // erstmalig
        state.HasChanged(new[] { "b", "a" }).Should().BeFalse();  // gleiche Menge (case-insensitiv)
        state.HasChanged(new[] { "A", "C" }).Should().BeTrue();   // Verschiebung bei gleicher Anzahl
        state.HasChanged(new[] { "A", "C" }).Should().BeFalse();
        state.HasChanged(System.Array.Empty<string>()).Should().BeTrue(); // geleert = Aenderung
    }
}
