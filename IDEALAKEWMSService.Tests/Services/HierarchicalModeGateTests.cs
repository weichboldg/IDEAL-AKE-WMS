using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>BOM-Bridge Design H: bei Master true fuehrt keiner der vier Einstiege einen Durchlauf aus —
/// kein DB-Zugriff (die leere Konfiguration hat keine Connection-Strings und wuerde sonst werfen).</summary>
public class HierarchicalModeGateTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    [Fact]
    public async Task CoatingDetection_SkipsWithoutDbAccess_WhenHierarchical()
    {
        var log = new FakeSyncLogger();
        var svc = new CoatingDetectionService(EmptyConfig(), new StubHierarchicalModeReader(true),
            NullLogger<CoatingDetectionService>.Instance, log);

        var result = await svc.DetectAndUpdateCoatingFlagsAsync(false, null, CancellationToken.None);

        result.Inserted.Should().Be(0);
        log.Runs.Should().ContainSingle();
        log.Runs[0].FinishedSuccess.Should().BeTrue();
        log.Runs[0].FinalCounts!["uebersprungen_hierarchisch"].Should().Be(1);
    }

    [Fact]
    public async Task FaWorkStepDetection_SkipsWithoutTouchingDb_WhenHierarchical()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.WorkSteps.Add(new WorkStep { Code = "VL", Name = "VL", SearchString = "luefter", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.ProductionOrders.Add(new ProductionOrder { OrderNumber = "FA-1", ArticleNumber = "ART-1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.CachedBomHeaders.Add(new CachedBomHeader { Artikelnummer = "ART-1", Source = "SAGE", ContentHash = "h", CachedAt = DateTime.UtcNow, ItemCount = 1,
            Items = new List<CachedBomItem> { new() { Bezeichnung1 = "Axialluefter", SortOrder = 0 } } });
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();
        var svc = new FaWorkStepDetectionService(ctx, new StubHierarchicalModeReader(true), NullLogger<FaWorkStepDetectionService>.Instance, log);

        var result = await svc.DetectAsync(dryRun: false);

        result.Inserted.Should().Be(0);
        (await ctx.FaWorkSteps.CountAsync()).Should().Be(0);   // haette sonst 1 angelegt
        log.Runs[0].FinishedSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BomCacheSync_BothEntries_SkipWithoutDbAccess_WhenHierarchical()
    {
        var log = new FakeSyncLogger();
        var svc = new BomCacheSyncService(EmptyConfig(), new StubHierarchicalModeReader(true), NullLogger<BomCacheSyncService>.Instance, log);

        var full = await svc.SyncBomCacheAsync(dryRun: false, ct: CancellationToken.None);
        var specific = await svc.SyncSpecificArticleNumbersAsync(new List<string> { "ART-1" }, dryRun: false, ct: CancellationToken.None);

        full.Inserted.Should().Be(0);
        specific.Inserted.Should().Be(0);
        log.Runs.Should().ContainSingle();           // nur SyncBomCacheAsync oeffnet einen Run
        log.Runs[0].FinishedSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Flat_StillReachesDb_AndFailsOnMissingConnection()
    {
        // Regression: Master false -> Verhalten wie vor der Bridge (Lifecycle-Test-Erwartung).
        var svc = new BomCacheSyncService(EmptyConfig(), new StubHierarchicalModeReader(false), NullLogger<BomCacheSyncService>.Instance, new FakeSyncLogger());
        var act = async () => await svc.SyncBomCacheAsync(dryRun: false, ct: CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }
}
