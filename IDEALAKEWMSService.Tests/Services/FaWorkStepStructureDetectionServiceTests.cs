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
/// IDEAL: FA-Arbeitsgang-Erkennung aus der Struktur (Spec 2026-09-08-arbeitsgaenge-aus-arbeitsschritte,
/// v1.41.0). Der Algorithmus ist InMemory-testbar (reine EF-LINQ, kein Raw-SQL). Die datenabhaengigen
/// AKs am befuellten IDEAL-System (AK 3/5/8) sind zusaetzlich Manual-UAT (TS-76). AK 1/2 (Master-/
/// Toggle-Gate) sitzen im SyncWorker, nicht im Service — daher hier nicht abgedeckt.
/// </summary>
public class FaWorkStepStructureDetectionServiceTests
{
    // Bei UNBEKANNTEN Token wird die Digest-Mail nur bei HasChanged==true versucht — und der
    // Versand liest die Mail-Konfiguration LIVE aus der SQL-Server-DB (ServiceSettings). Tests mit
    // unbekannten Token stubben daher HasChanged=false (Muster aus FaMaterializationWorkplaceTests).
    private static readonly IUnknownWorkStepTokenState NoDigest = BuildNoDigestState();

    private static IUnknownWorkStepTokenState BuildNoDigestState()
    {
        var m = new Mock<IUnknownWorkStepTokenState>();
        m.Setup(s => s.HasChanged(It.IsAny<IReadOnlyCollection<string>>())).Returns(false);
        return m.Object;
    }

    private static (FaWorkStepStructureDetectionService Svc, FakeSyncLogger Log) Build(
        ApplicationDbContext ctx, IUnknownWorkStepTokenState? state = null)
    {
        var log = new FakeSyncLogger();
        var svc = new FaWorkStepStructureDetectionService(
            ctx, new ConfigurationBuilder().Build(), new Mock<IMailService>().Object,
            state ?? new UnknownWorkStepTokenState(),
            NullLogger<FaWorkStepStructureDetectionService>.Instance, log);
        return (svc, log);
    }

    private static FaHierarchyNode Node(int subFa, int? vaterFa, string? arbeitsschritte, int hauptFa = 9000)
        => new() { HauptFA = hauptFa, SubFA = subFa, VaterFA = vaterFa, Arbeitsschritte = arbeitsschritte, Sollmenge = 1 };

    private static WorkStep Step(string code, string name = "n", string? search = null, bool active = true)
        => new() { Code = code, Name = name, SearchString = search, IsActive = active,
                   CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };

    private static ProductionOrder Order(string subFa, string hauptFa = "9000",
        bool isDone = false, bool isCancelled = false)
        => new() { OrderNumber = hauptFa, SubOrderNumber = subFa, Quantity = 1,
                   IsDone = isDone, IsCancelled = isCancelled,
                   CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };

    // --- TS-76.1 Grundfall (AK 3) ---
    [Fact]
    public async Task Detect_KnownTokenOnOwnRow_CreatesExactlyOneStruktur()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(subFa: 10, vaterFa: null, arbeitsschritte: "KA"));
        ctx.WorkSteps.Add(Step("KA", "Kappen"));
        ctx.ProductionOrders.Add(Order("10"));
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx);
        var r = await svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(1);
        var rows = await ctx.FaWorkSteps.ToListAsync();
        rows.Should().ContainSingle();
        rows[0].Source.Should().Be(FaWorkStepSources.Struktur);
        log.Runs.Single().FinalCounts!["neu"].Should().Be(1);
    }

    // --- AK 10 Audit ---
    [Fact]
    public async Task Detect_CreatedRow_CarriesServiceNameAudit()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        ctx.WorkSteps.Add(Step("KA"));
        ctx.ProductionOrders.Add(Order("10"));
        await ctx.SaveChangesAsync();

        await Build(ctx).Svc.DetectAsync(dryRun: false);

        var row = await ctx.FaWorkSteps.SingleAsync();
        row.CreatedBy.Should().Be("FaWorkStepStructureDetection");
        row.CreatedByWindows.Should().Be("FaWorkStepStructureDetection");
    }

    // --- Code-Match case-insensitiv + getrimmt ---
    [Fact]
    public async Task Detect_TokenCaseAndWhitespaceInsensitive_Matches()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "  ka   ls  "));
        ctx.WorkSteps.AddRange(Step("KA"), Step("LS"));
        ctx.ProductionOrders.Add(Order("10"));
        await ctx.SaveChangesAsync();

        var r = await Build(ctx).Svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(2);
    }

    // --- TS-76.2 DirectChildren-Gegenprobe (AK 5) ---
    [Fact]
    public async Task Detect_GrandchildToken_LandsOnParentNotGrandparent()
    {
        using var ctx = TestDbContextFactory.Create();
        // X(10) -> Y(20) -> Z(0, Blatt). SÄ steht NUR auf Z; AV steht auf X.
        ctx.FaHierarchyNodes.AddRange(
            Node(subFa: 10, vaterFa: null, arbeitsschritte: "AV"),
            Node(subFa: 20, vaterFa: 10, arbeitsschritte: null),
            Node(subFa: 0, vaterFa: 20, arbeitsschritte: "SÄ"));
        ctx.WorkSteps.AddRange(Step("AV"), Step("SÄ"));
        ctx.ProductionOrders.AddRange(Order("10"), Order("20"));
        await ctx.SaveChangesAsync();

        await Build(ctx).Svc.DetectAsync(dryRun: false);

        var byOrder = await ctx.FaWorkSteps
            .Include(f => f.ProductionOrder).Include(f => f.WorkStep)
            .ToListAsync();
        // Y (SubOrderNumber 20) bekommt SÄ, X (10) bekommt NUR AV, nicht SÄ.
        byOrder.Where(f => f.ProductionOrder.SubOrderNumber == "20").Select(f => f.WorkStep.Code)
            .Should().BeEquivalentTo(new[] { "SÄ" });
        byOrder.Where(f => f.ProductionOrder.SubOrderNumber == "10").Select(f => f.WorkStep.Code)
            .Should().BeEquivalentTo(new[] { "AV" });
    }

    // --- TS-76.3 Nur-hinzufuegen: manuell entfernt (AK 4) ---
    [Fact]
    public async Task Detect_RemovedRowExists_NoReAdd()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        var step = Step("KA");
        var order = Order("10");
        ctx.WorkSteps.Add(step);
        ctx.ProductionOrders.Add(order);
        await ctx.SaveChangesAsync();
        ctx.FaWorkSteps.Add(new FaWorkStep
        {
            ProductionOrderId = order.Id, WorkStepId = step.Id, IsRemoved = true,
            Source = FaWorkStepSources.Manual,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var r = await Build(ctx).Svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
        (await ctx.FaWorkSteps.SingleAsync()).IsRemoved.Should().BeTrue();
    }

    // --- Nur-hinzufuegen: aktive Zeile existiert -> uebersprungen, keine Dublette ---
    [Fact]
    public async Task Detect_ActiveRowExists_SkippedNoDuplicate()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        var step = Step("KA");
        var order = Order("10");
        ctx.WorkSteps.Add(step);
        ctx.ProductionOrders.Add(order);
        await ctx.SaveChangesAsync();
        ctx.FaWorkSteps.Add(new FaWorkStep
        {
            ProductionOrderId = order.Id, WorkStepId = step.Id, Source = FaWorkStepSources.Struktur,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx);
        var r = await svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
        (await ctx.FaWorkSteps.CountAsync()).Should().Be(1);
        log.Runs.Single().FinalCounts!["uebersprungen"].Should().Be(1);
    }

    // --- TS-76.9 Match exakt ueber Code, NICHT ueber SearchString (AK 6) ---
    [Fact]
    public async Task Detect_SearchStringContainsToken_ButCodeDiffers_NoMatch()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        ctx.WorkSteps.Add(Step("XY", "irgendwas", search: "KA,foo,bar")); // Code != KA
        ctx.ProductionOrders.Add(Order("10"));
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx, NoDigest);
        var r = await svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
        (await ctx.FaWorkSteps.AnyAsync()).Should().BeFalse();
        log.Runs.Single().FinalCounts!["arbeitsschritt_unbekannt"].Should().Be(1);
    }

    // --- TS-76.4 Unbekanntes Token: melden, kein Insert (AK 7, AK 12) ---
    [Fact]
    public async Task Detect_UnknownToken_ReportedNotInserted()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.AddRange(
            Node(10, null, "KA ZZZ"),   // ZZZ unbekannt
            Node(20, null, "ZZZ"));
        ctx.WorkSteps.Add(Step("KA"));
        ctx.ProductionOrders.AddRange(Order("10"), Order("20"));
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx, NoDigest);
        var r = await svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(1); // nur KA auf FA 10
        var counts = log.Runs.Single().FinalCounts!;
        counts["arbeitsschritt_unbekannt"].Should().Be(1);          // ein distinktes unbekanntes Token (ZZZ)
        counts["arbeitsschritt_unbekannt_auftraege"].Should().Be(2); // auf FA 10 und FA 20
        log.Runs.Single().Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("ZZZ"));
    }

    // --- TS-76.5 Abgeschlossener Auftrag (AK 9): IsDone ---
    [Fact]
    public async Task Detect_DoneOrder_GetsNoRow()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        ctx.WorkSteps.Add(Step("KA"));
        ctx.ProductionOrders.Add(Order("10", isDone: true));
        await ctx.SaveChangesAsync();

        var r = await Build(ctx).Svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
    }

    // --- AK 9: IsDonePicking ---
    [Fact]
    public async Task Detect_DonePickingOrder_GetsNoRow()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        ctx.WorkSteps.Add(Step("KA"));
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

    // --- TS-76.6 Fehlende Materialisierung (AK 8): kein Fehler, keine Zeile ---
    [Fact]
    public async Task Detect_NodeWithoutMaterializedOrder_NoErrorNoRow()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA")); // kein passender ProductionOrder
        ctx.WorkSteps.Add(Step("KA"));
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx);
        var r = await svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
        log.Runs.Single().FinishedSuccess.Should().BeTrue();
        (await ctx.FaWorkSteps.AnyAsync()).Should().BeFalse();
    }

    // --- DryRun zaehlt, schreibt nicht ---
    [Fact]
    public async Task Detect_DryRun_CountsButDoesNotWrite()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(Node(10, null, "KA"));
        ctx.WorkSteps.Add(Step("KA"));
        ctx.ProductionOrders.Add(Order("10"));
        await ctx.SaveChangesAsync();

        var (svc, log) = Build(ctx);
        var r = await svc.DetectAsync(dryRun: true);

        r.Inserted.Should().Be(1);
        (await ctx.FaWorkSteps.AnyAsync()).Should().BeFalse();
        log.Runs.Single().FinalMessageSuffix.Should().Be("[DryRun]");
    }

    // --- SubFA==0 (Blatt) bekommt keine eigene Zeile ---
    [Fact]
    public async Task Detect_LeafNodeSubFaZero_IsNotItsOwnOrder()
    {
        using var ctx = TestDbContextFactory.Create();
        // Nur ein Blatt-Knoten mit Token; SubFA==0 -> nie eigener materialisierter Auftrag.
        ctx.FaHierarchyNodes.Add(Node(subFa: 0, vaterFa: 10, arbeitsschritte: "KA"));
        ctx.WorkSteps.Add(Step("KA"));
        ctx.ProductionOrders.Add(Order("0")); // selbst wenn es einen SubOrderNumber "0" gaebe
        await ctx.SaveChangesAsync();

        var r = await Build(ctx).Svc.DetectAsync(dryRun: false);

        r.Inserted.Should().Be(0);
    }

    // --- S1-Zustand (Sammelmail nur bei Mengenaenderung) — reine Logik ---
    [Fact]
    public void UnknownTokenState_ReportsChangeOnlyWhenSetChanges()
    {
        var state = new UnknownWorkStepTokenState();
        state.HasChanged(new[] { "A", "B" }).Should().BeTrue();   // erstmalig
        state.HasChanged(new[] { "b", "a" }).Should().BeFalse();  // gleiche Menge (case-insensitiv)
        state.HasChanged(new[] { "A", "C" }).Should().BeTrue();   // Verschiebung bei gleicher Anzahl
        state.HasChanged(new[] { "A", "C" }).Should().BeFalse();
        state.HasChanged(System.Array.Empty<string>()).Should().BeTrue(); // geleert = Aenderung
    }
}
