using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

public class FaWorkStepDetectionServiceTests
{
    private static FaWorkStepDetectionService CreateService(ApplicationDbContext ctx, FakeSyncLogger? syncLogger = null)
        => new(ctx, NullLogger<FaWorkStepDetectionService>.Instance, syncLogger ?? new FakeSyncLogger());

    private static WorkStep NewWorkStep(string code, string? searchString, bool isActive = true) => new()
    {
        Code = code,
        Name = code,
        SearchString = searchString,
        IsActive = isActive,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
    };

    private static ProductionOrder NewOrder(string orderNumber, string? articleNumber, bool isDone = false) => new()
    {
        OrderNumber = orderNumber,
        ArticleNumber = articleNumber,
        IsDone = isDone,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
    };

    private static CachedBomHeader NewBomHeader(string artikelnummer, params string[] bezeichnungen) => new()
    {
        Artikelnummer = artikelnummer,
        Source = "SAGE",
        ContentHash = "hash",
        CachedAt = DateTime.UtcNow,
        ItemCount = bezeichnungen.Length,
        Items = bezeichnungen
            .Select((b, i) => new CachedBomItem { Bezeichnung1 = b, SortOrder = i })
            .ToList(),
    };

    [Fact]
    public async Task Detect_AddsFaWorkStep_WhenTermMatchesBezeichnung()
    {
        using var ctx = TestDbContextFactory.Create();
        var step = NewWorkStep("VL", "luefter");
        var order = NewOrder("FA-1", "ART-1");
        ctx.WorkSteps.Add(step);
        ctx.ProductionOrders.Add(order);
        ctx.CachedBomHeaders.Add(NewBomHeader("ART-1", "Axialluefter 230V"));
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).DetectAsync(dryRun: false);

        result.Inserted.Should().Be(1);
        var row = await ctx.FaWorkSteps.SingleAsync();
        row.ProductionOrderId.Should().Be(order.Id);
        row.WorkStepId.Should().Be(step.Id);
        row.Source.Should().Be(FaWorkStepSources.Sync);
        row.IsRemoved.Should().BeFalse();
    }

    [Fact]
    public async Task Detect_MatchesSecondTerm_OfCommaList()
    {
        using var ctx = TestDbContextFactory.Create();
        var step = NewWorkStep("VL", "Luefter,Ventilator");
        var order = NewOrder("FA-1", "ART-1");
        ctx.WorkSteps.Add(step);
        ctx.ProductionOrders.Add(order);
        ctx.CachedBomHeaders.Add(NewBomHeader("ART-1", "Ventilator XY"));
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).DetectAsync(dryRun: false);

        result.Inserted.Should().Be(1);
        var row = await ctx.FaWorkSteps.SingleAsync();
        row.ProductionOrderId.Should().Be(order.Id);
        row.WorkStepId.Should().Be(step.Id);
    }

    [Fact]
    public async Task Detect_DoesNotReAdd_WhenRowIsRemoved()
    {
        using var ctx = TestDbContextFactory.Create();
        var step = NewWorkStep("VL", "luefter");
        var order = NewOrder("FA-1", "ART-1");
        ctx.WorkSteps.Add(step);
        ctx.ProductionOrders.Add(order);
        ctx.CachedBomHeaders.Add(NewBomHeader("ART-1", "Axialluefter 230V"));
        await ctx.SaveChangesAsync();

        ctx.FaWorkSteps.Add(new FaWorkStep
        {
            ProductionOrderId = order.Id,
            WorkStepId = step.Id,
            Source = FaWorkStepSources.Manual,
            IsRemoved = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        });
        await ctx.SaveChangesAsync();

        var fakeLogger = new FakeSyncLogger();
        var result = await CreateService(ctx, fakeLogger).DetectAsync(dryRun: false);

        result.Inserted.Should().Be(0);
        var row = await ctx.FaWorkSteps.SingleAsync();
        row.IsRemoved.Should().BeTrue("vorhandene IsRemoved-Zeile sperrt Re-Add und bleibt unveraendert");
        row.Source.Should().Be(FaWorkStepSources.Manual);
        fakeLogger.Runs[0].FinalCounts!["uebersprungen"].Should().Be(1);
    }

    [Fact]
    public async Task Detect_SkipsClosedOrders_AndOrdersWithoutBomCache()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.WorkSteps.Add(NewWorkStep("VL", "luefter"));
        // Geschlossener FA mit passendem BOM-Cache
        ctx.ProductionOrders.Add(NewOrder("FA-1", "ART-1", isDone: true));
        ctx.CachedBomHeaders.Add(NewBomHeader("ART-1", "Axialluefter 230V"));
        // Offener FA ohne BOM-Cache-Eintrag
        ctx.ProductionOrders.Add(NewOrder("FA-2", "ART-2"));
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).DetectAsync(dryRun: false);

        result.Inserted.Should().Be(0);
        (await ctx.FaWorkSteps.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Detect_DryRun_WritesNothing()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.WorkSteps.Add(NewWorkStep("VL", "luefter"));
        ctx.ProductionOrders.Add(NewOrder("FA-1", "ART-1"));
        ctx.CachedBomHeaders.Add(NewBomHeader("ART-1", "Axialluefter 230V"));
        await ctx.SaveChangesAsync();

        var fakeLogger = new FakeSyncLogger();
        var result = await CreateService(ctx, fakeLogger).DetectAsync(dryRun: true);

        result.Inserted.Should().Be(1, "DryRun zaehlt, schreibt aber nicht");
        (await ctx.FaWorkSteps.AnyAsync()).Should().BeFalse();
        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(1);
    }
}
