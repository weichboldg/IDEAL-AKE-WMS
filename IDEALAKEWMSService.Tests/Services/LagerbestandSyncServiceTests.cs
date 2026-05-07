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
}
