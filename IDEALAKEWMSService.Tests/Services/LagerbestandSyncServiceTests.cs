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

    [Fact]
    public async Task Run_UnknownArticle_SkipsAndLogsWarning()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        SeedSageLocation(ctx, id: 1, code: "L-1");
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("UNKNOWN-ARTICLE", "L-1", 5m) };

        var result = await svc.RunAsync(dryRun: false);

        result.Skipped.Should().Be(1);
        result.CorrectionsPlus.Should().Be(0);
        ctx.StockMovements.Should().BeEmpty();

        var warnings = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Warning, 10);
        warnings.Should().Contain(x => x.Message.Contains("UNKNOWN-ARTICLE"));
    }

    [Fact]
    public async Task Run_UnknownLocation_SkipsAndLogsWarning()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "UNKNOWN-LOC", 5m) };

        var result = await svc.RunAsync(dryRun: false);

        result.Skipped.Should().Be(1);
        ctx.StockMovements.Should().BeEmpty();
        var warnings = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Warning, 10);
        warnings.Should().Contain(x => x.Message.Contains("UNKNOWN-LOC"));
    }

    [Fact]
    public async Task Run_ManualLocation_SkipsAndLogsWarning()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        ctx.StorageLocations.Add(new StorageLocation
        {
            Id = 1, Code = "MAN-1", BarcodeValue = "MAN-1",
            Source = StorageLocationSource.Manual, IsActive = true,
            IsPickingTransport = false,
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "MAN-1", 5m) };

        var result = await svc.RunAsync(dryRun: false);

        result.Skipped.Should().Be(1);
        ctx.StockMovements.Should().BeEmpty();
        var warnings = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Warning, 10);
        warnings.Should().Contain(x => x.Message.Contains("Manual"));
    }

    [Fact]
    public async Task Run_InactiveSageLocation_SkipsAndLogsWarning()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1", isActive: false);
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-1", 5m) };

        var result = await svc.RunAsync(dryRun: false);

        result.Skipped.Should().Be(1);
        ctx.StockMovements.Should().BeEmpty();
        var warnings = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Warning, 10);
        warnings.Should().Contain(x => x.Message.Contains("deaktiviert"));
    }

    [Fact]
    public async Task Run_AggregatesMultiplePreMovements_BeforeComputingDelta()
    {
        var (svc, reader, ctx, _) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        SeedSageLocation(ctx, id: 2, code: "L-2");
        ctx.StockMovements.AddRange(
            new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 10m, MovementType = MovementType.Einbuchung,    Timestamp = DateTime.Now, WindowsUser = "x", CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" },
            new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 4m,  MovementType = MovementType.Ausbuchung,    Timestamp = DateTime.Now, WindowsUser = "x", CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" },
            new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 2m,  MovementType = MovementType.Umbuchung,
                                SourceStorageLocationId = 2,
                                Timestamp = DateTime.Now, WindowsUser = "x", CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" }
        );
        await ctx.SaveChangesAsync();
        // Effektiver Bestand auf L-1: 10 - 4 + 2 = 8
        reader.Records = new() { new("A-1", "L-1", 6m) };

        var result = await svc.RunAsync(dryRun: false);

        result.CorrectionsMinus.Should().Be(1);
        var c = ctx.StockMovements.Single(m => m.MovementType == MovementType.SageAusbuchung);
        c.Quantity.Should().Be(2m);   // 8 -> 6, Korrektur -2
    }

    [Fact]
    public async Task Run_CorrectionMovement_HasExpectedAuditFields()
    {
        var (svc, reader, ctx, _) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-1", 5m) };

        await svc.RunAsync(dryRun: false);

        var c = ctx.StockMovements.Single();
        c.WindowsUser.Should().Be(SyncUser);
        c.CreatedBy.Should().Be(SyncUser);
        c.UserId.Should().BeNull();
        c.ProductionOrder.Should().BeNull();
        c.Note.Should().NotBeNull();
        c.Note.Should().Contain("WMS=0");
        c.Note.Should().Contain("Sage=5");
        c.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Run_DryRun_DoesNotInsertButLogsCounts()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-1", 5m) };

        var result = await svc.RunAsync(dryRun: true);

        result.CorrectionsPlus.Should().Be(1);
        result.DryRun.Should().BeTrue();
        ctx.StockMovements.Should().BeEmpty();   // KEIN Insert

        var summary = (await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Info, 10)).First();
        summary.Message.Should().StartWith("[DryRun]");
    }

    [Fact]
    public async Task Run_SageReaderThrows_LogsErrorAndDoesNotCrash()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        reader.ThrowOnRead = new InvalidOperationException("Sage offline");

        var result = await svc.RunAsync(dryRun: false);

        result.Errors.Should().Be(1);
        ctx.StockMovements.Should().BeEmpty();

        var errors = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Error, 10);
        errors.Should().ContainSingle();
        errors[0].Message.Should().Contain("Sage offline");
    }

    [Fact]
    public async Task Run_SageDuplicateTuple_SkipsAllAndLogsWarning()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        SeedSageLocation(ctx, id: 2, code: "L-2");
        await ctx.SaveChangesAsync();
        // Sage liefert (A-1, L-1) zweimal — z.B. aus zwei verschiedenen Lagerorten mit gleichem Lagerplatz-Code
        reader.Records = new()
        {
            new("A-1", "L-1", 5m),
            new("A-1", "L-1", 7m),
            new("A-1", "L-2", 3m)   // dieser sollte normal verarbeitet werden
        };

        var result = await svc.RunAsync(dryRun: false);

        // Nur der eindeutige (A-1, L-2)-Tupel wird verarbeitet
        result.CorrectionsPlus.Should().Be(1);
        var corrections = ctx.StockMovements
            .Where(m => m.MovementType == MovementType.SageEinbuchung).ToList();
        corrections.Should().ContainSingle();
        corrections[0].StorageLocationId.Should().Be(2);

        var warnings = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Warning, 10);
        warnings.Should().Contain(x => x.Message.Contains("mehrfach"));
    }
}
