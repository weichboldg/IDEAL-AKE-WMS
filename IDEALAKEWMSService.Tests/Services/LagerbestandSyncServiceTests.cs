using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

public class LagerbestandSyncServiceTests
{
    private const string SyncUser = "system:sync";

    private static (LagerbestandSyncService service, FakeSageBestandReader reader,
                    IdealAkeWms.Data.ApplicationDbContext ctx, SyncLogRepository syncLogs)
        Build()
    {
        var ctx = TestDbContextFactory.Create();
        var reader = new FakeSageBestandReader();
        var syncLogs = new SyncLogRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var service = new LagerbestandSyncService(ctx, reader, stockRepo, syncLogs, NullLogger<LagerbestandSyncService>.Instance);
        return (service, reader, ctx, syncLogs);
    }

    private static void SeedArticle(IdealAkeWms.Data.ApplicationDbContext ctx, int id, string number)
    {
        ctx.Articles.Add(new Article
        {
            Id = id, ArticleNumber = number,
            Description = "Test", Unit = "Stk",
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
    }

    private static void SeedSageLocation(IdealAkeWms.Data.ApplicationDbContext ctx, int id, string code, bool isActive = true)
    {
        ctx.StorageLocations.Add(new StorageLocation
        {
            Id = id, Code = code, BarcodeValue = code,
            Source = StorageLocationSource.Sage, IsActive = isActive,
            IsPickingTransport = false,
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
    }

    [Fact]
    public async Task Run_EmptyWms_SagePositive_InsertsSageEinbuchung()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-1", 5m) };

        var result = await svc.RunAsync(dryRun: false);

        result.CorrectionsPlus.Should().Be(1);
        result.CorrectionsMinus.Should().Be(0);
        result.NoChange.Should().Be(0);
        result.Skipped.Should().Be(0);
        result.Tuples.Should().Be(1);

        var movements = ctx.StockMovements.ToList();
        movements.Should().ContainSingle();
        movements[0].MovementType.Should().Be(MovementType.SageEinbuchung);
        movements[0].Quantity.Should().Be(5m);
        movements[0].WindowsUser.Should().Be(SyncUser);
        movements[0].Note.Should().Contain("Diff=+5");

        var summary = (await syncLogs.GetRecentAsync("Lagerbestand", null, 10)).FirstOrDefault();
        summary.Should().NotBeNull();
        summary!.Level.Should().Be(SyncLogLevel.Info);
    }

    [Fact]
    public async Task Run_WmsHigherThanSage_InsertsSageAusbuchung()
    {
        var (svc, reader, ctx, _) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1,
            Quantity = 10m, MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.Now.AddDays(-1),
            WindowsUser = "tester",
            CreatedAt = DateTime.Now.AddDays(-1),
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-1", 7m) };

        var result = await svc.RunAsync(dryRun: false);

        result.CorrectionsMinus.Should().Be(1);
        result.CorrectionsPlus.Should().Be(0);
        var corrections = ctx.StockMovements
            .Where(m => m.MovementType == MovementType.SageAusbuchung).ToList();
        corrections.Should().ContainSingle();
        corrections[0].Quantity.Should().Be(3m);
        corrections[0].Note.Should().Contain("Diff=-3");
    }

    [Fact]
    public async Task Run_WmsEqualsSage_NoCorrection()
    {
        var (svc, reader, ctx, _) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1, Quantity = 5m,
            MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.Now,
            WindowsUser = "tester", CreatedAt = DateTime.Now,
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-1", 5m) };

        var result = await svc.RunAsync(dryRun: false);

        result.NoChange.Should().Be(1);
        result.CorrectionsPlus.Should().Be(0);
        result.CorrectionsMinus.Should().Be(0);
        ctx.StockMovements.Where(m => m.MovementType >= MovementType.SageEinbuchung)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Run_DecimalDiff_PreservesFraction()
    {
        var (svc, reader, ctx, _) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1, Quantity = 5.7m,
            MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.Now, WindowsUser = "tester",
            CreatedAt = DateTime.Now,
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-1", 6.0m) };

        var result = await svc.RunAsync(dryRun: false);

        result.CorrectionsPlus.Should().Be(1);
        var c = ctx.StockMovements.Single(m => m.MovementType == MovementType.SageEinbuchung);
        c.Quantity.Should().Be(0.3m);
    }

    [Fact]
    public async Task Run_SageBestandNull_TreatsAsZero()
    {
        var (svc, reader, ctx, _) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1, Quantity = 4m,
            MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.Now, WindowsUser = "tester",
            CreatedAt = DateTime.Now,
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-1", null) };

        var result = await svc.RunAsync(dryRun: false);

        result.CorrectionsMinus.Should().Be(1);
        var c = ctx.StockMovements.Single(m => m.MovementType == MovementType.SageAusbuchung);
        c.Quantity.Should().Be(4m);
    }
}
