using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;            // TestDbContextFactory liegt im Web-Test-Projekt
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

public class LagerplatzSyncServiceTests
{
    private const string SyncUser_For_Tests = "system:sync";

    private static (LagerplatzSyncService service, FakeSageLagerplatzReader reader, IdealAkeWms.Data.ApplicationDbContext ctx, SyncLogRepository syncLogs)
        Build()
    {
        var ctx = TestDbContextFactory.Create();
        var reader = new FakeSageLagerplatzReader();
        var syncLogs = new SyncLogRepository(ctx);
        var service = new LagerplatzSyncService(ctx, reader, syncLogs, NullLogger<LagerplatzSyncService>.Instance);
        return (service, reader, ctx, syncLogs);
    }

    [Fact]
    public async Task Run_EmptyDb_ThreeSagePlaetze_InsertsThree()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        reader.Records = new()
        {
            new("HALLE-1", "A-01-01", "Regal A1"),
            new("HALLE-1", "A-01-02", "Regal A2"),
            new("HALLE-2", "B-01-01", "Lager Aussen"),
        };

        var result = await svc.RunAsync();

        result.Inserted.Should().Be(3);
        result.Updated.Should().Be(0);
        result.Conflicts.Should().Be(0);

        var stored = ctx.StorageLocations.OrderBy(s => s.Code).ToList();
        stored.Should().HaveCount(3);
        stored[0].Code.Should().Be("A-01-01");
        stored[0].Zone.Should().Be("HALLE-1");
        stored[0].Description.Should().Be("Regal A1");
        stored[0].Source.Should().Be(StorageLocationSource.Sage);
        stored[0].IsActive.Should().BeTrue();
        stored[0].BarcodeValue.Should().Be("A-01-01");
        stored[0].Capacity.Should().BeNull();
        stored[0].IsPickingTransport.Should().BeFalse();

        var summary = (await syncLogs.GetRecentAsync("Lagerplatz", null, 10)).FirstOrDefault();
        summary.Should().NotBeNull();
        summary!.Level.Should().Be(SyncLogLevel.Info);
        summary.Message.Should().Contain("3 neu");
    }

    [Fact]
    public async Task Run_ExistingSageRecord_DescriptionDiff_UpdatesAndStampsModified()
    {
        var (svc, reader, ctx, _) = Build();
        ctx.StorageLocations.Add(new StorageLocation
        {
            Code = "A-01-01", Zone = "HALLE-1", Description = "Alte Bezeichnung",
            BarcodeValue = "A-01-01", Source = StorageLocationSource.Sage, IsActive = true,
            CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = SyncUser_For_Tests, CreatedByWindows = "x"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("HALLE-1", "A-01-01", "Neue Bezeichnung") };

        var result = await svc.RunAsync();

        result.Updated.Should().Be(1);
        result.Inserted.Should().Be(0);
        var sl = ctx.StorageLocations.Single();
        sl.Description.Should().Be("Neue Bezeichnung");
        sl.ModifiedAt.Should().NotBeNull();
        sl.ModifiedBy.Should().Be(SyncUser_For_Tests);
    }

    [Fact]
    public async Task Run_ExistingSageRecord_NoDiff_DoesNotUpdate()
    {
        var (svc, reader, ctx, _) = Build();
        var original = new StorageLocation
        {
            Code = "A-01-01", Zone = "HALLE-1", Description = "Regal A1",
            BarcodeValue = "A-01-01", Source = StorageLocationSource.Sage, IsActive = true,
            CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = SyncUser_For_Tests, CreatedByWindows = "x"
        };
        ctx.StorageLocations.Add(original);
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("HALLE-1", "A-01-01", "Regal A1") };

        var result = await svc.RunAsync();

        result.Updated.Should().Be(0);
        var sl = ctx.StorageLocations.Single();
        sl.ModifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task Run_ExistingManualRecord_SameCodeFromSage_ConflictWithoutWrite()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        ctx.StorageLocations.Add(new StorageLocation
        {
            Code = "ABC", Zone = "MANUAL-ZONE", Description = "Manuell angelegt",
            BarcodeValue = "ABC", Source = StorageLocationSource.Manual, IsActive = true,
            CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = "tester", CreatedByWindows = "tester"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("HALLE-1", "ABC", "Sage-Bezeichnung") };

        var result = await svc.RunAsync();

        result.Conflicts.Should().Be(1);
        result.Updated.Should().Be(0);

        var sl = ctx.StorageLocations.Single();
        sl.Source.Should().Be(StorageLocationSource.Manual);
        sl.Zone.Should().Be("MANUAL-ZONE");
        sl.Description.Should().Be("Manuell angelegt");

        var warnings = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Warning, 10);
        warnings.Should().ContainSingle();
        warnings[0].Reference.Should().Be("ABC");
        warnings[0].Message.Should().Contain("manuell");
    }

    [Fact]
    public async Task Run_SageRecord_NoLongerInSage_SoftDeactivates()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        ctx.StorageLocations.Add(new StorageLocation
        {
            Code = "GONE-1", Zone = "HALLE-X", Description = "war mal in Sage",
            BarcodeValue = "GONE-1", Source = StorageLocationSource.Sage, IsActive = true,
            CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = SyncUser_For_Tests, CreatedByWindows = "x"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new(); // leer

        var result = await svc.RunAsync();

        result.Deactivated.Should().Be(1);
        var sl = ctx.StorageLocations.Single();
        sl.IsActive.Should().BeFalse();
        sl.ModifiedAt.Should().NotBeNull();

        var infos = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Info, 10);
        infos.Should().ContainSingle(x => x.Reference == "GONE-1");
    }

    [Fact]
    public async Task Run_DeactivatedSageRecord_ReappearsInSage_ReactivatesAndCountsUpdate()
    {
        var (svc, reader, ctx, _) = Build();
        ctx.StorageLocations.Add(new StorageLocation
        {
            Code = "BACK-1", Zone = "HALLE-1", Description = "war mal weg",
            BarcodeValue = "BACK-1", Source = StorageLocationSource.Sage, IsActive = false,
            CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = SyncUser_For_Tests, CreatedByWindows = "x"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("HALLE-1", "BACK-1", "war mal weg") };

        var result = await svc.RunAsync();

        result.Updated.Should().Be(1);
        var sl = ctx.StorageLocations.Single();
        sl.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Run_SageDuplicateCode_SkipsAllAndLogsWarning()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        reader.Records = new()
        {
            new("HALLE-A", "DUP", "Erste Zeile"),
            new("HALLE-B", "DUP", "Zweite Zeile"),
            new("HALLE-1", "OK",  "Eindeutiger Eintrag"),
        };

        var result = await svc.RunAsync();

        result.Inserted.Should().Be(1); // nur OK
        ctx.StorageLocations.Should().HaveCount(1);
        ctx.StorageLocations.Single().Code.Should().Be("OK");

        var warnings = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Warning, 10);
        warnings.Should().ContainSingle(x => x.Reference == "DUP");
        warnings[0].Message.Should().Contain("mehrfach");
    }

    [Fact]
    public async Task Run_SageCodeTooLong_SkipsAndLogsWarning()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        var longCode = new string('X', 13); // > 12 (MaxCodeLength = 12 to match StorageLocation [StringLength(12)])
        reader.Records = new() { new("HALLE-1", longCode, "irgendwas") };

        var result = await svc.RunAsync();

        result.Inserted.Should().Be(0);
        result.Skipped.Should().Be(1);
        ctx.StorageLocations.Should().BeEmpty();

        var warnings = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Warning, 10);
        warnings.Should().Contain(x => x.Reference == longCode && x.Message.Contains("zu lang"));
    }

    [Fact]
    public async Task Run_SageDescriptionTooLong_TruncatesAndLogsInfo()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        var longDesc = new string('Y', 250); // > 200
        reader.Records = new() { new("HALLE-1", "TRUNC-1", longDesc) };

        var result = await svc.RunAsync();

        result.Inserted.Should().Be(1);
        var sl = ctx.StorageLocations.Single();
        sl.Description!.Length.Should().Be(200);

        var infos = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Info, 10);
        infos.Should().Contain(x => x.Reference == "TRUNC-1" && x.Message.Contains("gekuerzt"));
    }

    [Fact]
    public async Task Run_SageReaderThrows_LogsError_NoCrash()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        reader.ThrowOnRead = new InvalidOperationException("Sage offline");

        var result = await svc.RunAsync();

        result.Errors.Should().Be(1);
        ctx.StorageLocations.Should().BeEmpty();

        var errors = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Error, 10);
        errors.Should().ContainSingle();
        errors[0].Message.Should().Contain("Sage offline");
    }

    [Fact]
    public async Task Run_NewSageRecord_SetsIstBuchbarFalse()
    {
        var (svc, reader, ctx, _) = Build();
        reader.Records = new() { new("HALLE-1", "NEU-1", "Neuer Sage-Platz") };

        await svc.RunAsync();

        var loc = ctx.StorageLocations.Single();
        loc.IstBuchbar.Should().BeFalse();
    }

    [Fact]
    public async Task Run_ExistingSageRecord_PreservesIstBuchbarOnUpdate()
    {
        var (svc, reader, ctx, _) = Build();
        ctx.StorageLocations.Add(new StorageLocation
        {
            Code = "EXIST-1", Zone = "HALLE-1", Description = "Alte Bezeichnung",
            BarcodeValue = "EXIST-1", Source = StorageLocationSource.Sage, IsActive = true,
            IstBuchbar = true,   // User hat es freigeschaltet
            CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = "system:sync", CreatedByWindows = "x"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("HALLE-1", "EXIST-1", "Neue Bezeichnung") };   // Description-Diff

        await svc.RunAsync();

        var loc = ctx.StorageLocations.Single();
        loc.Description.Should().Be("Neue Bezeichnung");   // Update wurde gemacht
        loc.IstBuchbar.Should().BeTrue();                   // User-Toggle bleibt
    }

    [Fact]
    public async Task Run_ConflictPath_DoesNotTouchManualIstBuchbar()
    {
        var (svc, reader, ctx, _) = Build();
        ctx.StorageLocations.Add(new StorageLocation
        {
            Code = "CONFLICT-1", Zone = "MANUAL-ZONE", Description = "Manuell",
            BarcodeValue = "CONFLICT-1", Source = StorageLocationSource.Manual, IsActive = true,
            IstBuchbar = true,   // Manual-Default
            CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = "tester", CreatedByWindows = "tester"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("HALLE-1", "CONFLICT-1", "Sage-Bezeichnung") };

        await svc.RunAsync();

        var loc = ctx.StorageLocations.Single();
        loc.IstBuchbar.Should().BeTrue();
        loc.Source.Should().Be(StorageLocationSource.Manual);
    }
}
