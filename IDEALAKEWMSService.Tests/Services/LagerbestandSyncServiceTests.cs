using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services;

public class LagerbestandSyncServiceTests
{
    private const string SyncUser = "system:sync";

    private static (LagerbestandSyncService service, FakeSageBestandReader reader,
                    IdealAkeWms.Data.ApplicationDbContext ctx, FakeSyncLogger fakeLogger)
        Build()
    {
        var (service, reader, ctx, fakeLogger, _) = BuildWithNotifier();
        return (service, reader, ctx, fakeLogger);
    }

    private static (LagerbestandSyncService service, FakeSageBestandReader reader,
                    IdealAkeWms.Data.ApplicationDbContext ctx, FakeSyncLogger fakeLogger,
                    Mock<ISyncErrorNotifier> notifier)
        BuildWithNotifier()
    {
        var ctx = TestDbContextFactory.Create();
        var reader = new FakeSageBestandReader();
        var fakeLogger = new FakeSyncLogger();
        var stockRepo = new StockMovementRepository(ctx);
        var config = new ConfigurationBuilder().Build();   // kein Connection-String -> Cap faellt auf Default 100
        var notifier = new Mock<ISyncErrorNotifier>();
        var service = new LagerbestandSyncService(
            ctx, reader, stockRepo, config, notifier.Object,
            NullLogger<LagerbestandSyncService>.Instance, fakeLogger);
        return (service, reader, ctx, fakeLogger, notifier);
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
        var (svc, reader, ctx, fakeLogger) = Build();
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

        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts!["einbuchungen"].Should().Be(1);
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
        var (svc, reader, ctx, fakeLogger) = Build();
        SeedSageLocation(ctx, id: 1, code: "L-1");
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("UNKNOWN-ARTICLE", "L-1", 5m) };

        var result = await svc.RunAsync(dryRun: false);

        result.Skipped.Should().Be(1);
        result.CorrectionsPlus.Should().Be(0);
        ctx.StockMovements.Should().BeEmpty();

        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("UNKNOWN-ARTICLE"));
    }

    [Fact]
    public async Task Run_UnknownLocation_SkipsAndLogsWarning()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "UNKNOWN-LOC", 5m) };

        var result = await svc.RunAsync(dryRun: false);

        result.Skipped.Should().Be(1);
        ctx.StockMovements.Should().BeEmpty();
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("UNKNOWN-LOC"));
    }

    [Fact]
    public async Task Run_ManualLocation_SkipsAndLogsWarning()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
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
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("Manual"));
    }

    [Fact]
    public async Task Run_InactiveSageLocation_SkipsAndLogsWarning()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1", isActive: false);
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-1", 5m) };

        var result = await svc.RunAsync(dryRun: false);

        result.Skipped.Should().Be(1);
        ctx.StockMovements.Should().BeEmpty();
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("deaktiviert"));
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
        // L-2 (Umbuchungs-Quellseite, WMS = -2) explizit im Sage-Read mit exakt -2 abbilden,
        // damit L-2 als "in Sage vorhanden + deckungsgleich" gilt: kein Delta, kein Nullsetzen.
        reader.Records = new() { new("A-1", "L-1", 6m), new("A-1", "L-2", -2m) };

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
        var (svc, reader, ctx, fakeLogger) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-1", 5m) };

        var result = await svc.RunAsync(dryRun: true);

        result.CorrectionsPlus.Should().Be(1);
        result.DryRun.Should().BeTrue();
        ctx.StockMovements.Should().BeEmpty();   // KEIN Insert

        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts!["einbuchungen"].Should().Be(1);
    }

    [Fact]
    public async Task Run_SageReaderThrows_LogsErrorAndDoesNotCrash()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        reader.ThrowOnRead = new InvalidOperationException("Sage offline");

        var result = await svc.RunAsync(dryRun: false);

        result.Errors.Should().Be(1);
        ctx.StockMovements.Should().BeEmpty();

        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Error" && e.Message.Contains("Sage offline"));
    }

    [Fact]
    public async Task Run_SageDuplicateTuple_SkipsAllAndLogsWarning()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
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

        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("mehrfach"));
    }

    // (f) Verwaistes Paar mit WMS > 0 auf Sage-aktivem Platz -> genau eine SageAusbuchung auf 0.
    [Fact]
    public async Task Run_OrphanPairWithStock_BooksSageAusbuchungToZero()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");   // verwaist (nicht in Sage-Read)
        SeedSageLocation(ctx, id: 2, code: "L-P");   // in Sage-Read vorhanden
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1, Quantity = 5m,
            MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
            WindowsUser = "tester", CreatedAt = DateTime.Now,
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
        await ctx.SaveChangesAsync();
        // Sage-Snapshot: nur (A-1, L-P), Bestand 0 (present, noChange). (A-1, L-1) fehlt -> verwaist.
        reader.Records = new() { new("A-1", "L-P", 0m) };

        var result = await svc.RunAsync(dryRun: false);

        var zeroing = ctx.StockMovements
            .Where(m => m.MovementType == MovementType.SageAusbuchung).ToList();
        zeroing.Should().ContainSingle();
        zeroing[0].StorageLocationId.Should().Be(1);
        zeroing[0].Quantity.Should().Be(5m);
        zeroing[0].WindowsUser.Should().Be(SyncUser);
        zeroing[0].Note.Should().Contain("auf 0 gesetzt");
        fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(1);
        result.CorrectionsMinus.Should().Be(0); // Zeroing zaehlt NICHT als Korrektur
    }

    // (g) Leerer Sage-Read -> keine Nullbuchung, Warn "leer", kein NotifyAsync.
    [Fact]
    public async Task Run_EmptySageRead_NoZeroing_LogsLeerWarning()
    {
        var (svc, reader, ctx, fakeLogger, notifier) = BuildWithNotifier();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1, Quantity = 5m,
            MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
            WindowsUser = "tester", CreatedAt = DateTime.Now,
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new();   // leerer Read

        await svc.RunAsync(dryRun: false);

        ctx.StockMovements.Where(m => m.MovementType >= MovementType.SageEinbuchung)
            .Should().BeEmpty();
        fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(0);
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("leer"));
        notifier.Verify(n => n.NotifyAsync(
            It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // (h) Cap ueberschritten -> keine Nullbuchung + NotifyAsync aufgerufen.
    // Hinweis: ServiceSettings liest aus der DB, in Tests faellt der Cap immer auf Default 100 zurueck
    // -> 101 verwaiste Paare noetig, um den Cap zu ueberschreiten.
    [Fact]
    public async Task Run_CapExceeded_NoZeroing_CallsNotify()
    {
        var (svc, reader, ctx, fakeLogger, notifier) = BuildWithNotifier();
        SeedArticle(ctx, id: 1, number: "A-1");
        for (int i = 1; i <= 101; i++)
        {
            SeedSageLocation(ctx, id: i, code: $"L-{i}");
            ctx.StockMovements.Add(new StockMovement
            {
                ArticleId = 1, StorageLocationId = i, Quantity = 1m,
                MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
                WindowsUser = "tester", CreatedAt = DateTime.Now,
                CreatedBy = "tester", CreatedByWindows = "tester"
            });
        }
        SeedSageLocation(ctx, id: 200, code: "L-P");   // present, damit Read nicht leer
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-P", 0m) };   // 101 Paare (L-1..L-101) verwaist

        await svc.RunAsync(dryRun: false);

        ctx.StockMovements.Where(m => m.MovementType >= MovementType.SageEinbuchung)
            .Should().BeEmpty();
        fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(0);
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("Cap"));
        notifier.Verify(n => n.NotifyAsync(
            It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // (i) Verwaistes Paar auf manuellem/inaktivem Platz -> NICHT genullt.
    [Fact]
    public async Task Run_OrphanOnManualOrInactiveLocation_NotZeroed()
    {
        var (svc, reader, ctx, _) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        ctx.StorageLocations.Add(new StorageLocation
        {
            Id = 1, Code = "MAN-1", BarcodeValue = "MAN-1",
            Source = StorageLocationSource.Manual, IsActive = true,
            IsPickingTransport = false, CreatedBy = "t", CreatedByWindows = "t"
        });
        SeedSageLocation(ctx, id: 2, code: "INACT-1", isActive: false);
        SeedSageLocation(ctx, id: 3, code: "L-P");
        ctx.StockMovements.AddRange(
            new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 5m, MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now, WindowsUser = "t", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new StockMovement { ArticleId = 1, StorageLocationId = 2, Quantity = 5m, MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now, WindowsUser = "t", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" }
        );
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-P", 0m) };

        await svc.RunAsync(dryRun: false);

        ctx.StockMovements.Where(m => m.MovementType >= MovementType.SageEinbuchung)
            .Should().BeEmpty();
    }

    // (j) Duplikat-Key in Sage -> NICHT genullt (Dups sind in Sage vorhanden, nur mehrdeutig).
    [Fact]
    public async Task Run_DuplicateSageKey_NotZeroed()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1, Quantity = 5m,
            MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
            WindowsUser = "t", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();
        // (A-1, L-1) doppelt -> Korrektur-Schleife ueberspringt (Warn "mehrfach"),
        // aber sagePresentKeys (aus Roh-Zeilen) enthaelt (A-1, L-1) -> KEIN Nullsetzen.
        reader.Records = new() { new("A-1", "L-1", 5m), new("A-1", "L-1", 7m) };

        await svc.RunAsync(dryRun: false);

        ctx.StockMovements.Where(m => m.MovementType >= MovementType.SageEinbuchung)
            .Should().BeEmpty();
        fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(0);
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("mehrfach"));
    }

    // (k) DryRun -> keine Writes, aber Count + Log.
    [Fact]
    public async Task Run_DryRun_NoZeroingWrites_ButCounts()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");   // verwaist
        SeedSageLocation(ctx, id: 2, code: "L-P");
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1, Quantity = 5m,
            MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
            WindowsUser = "t", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-P", 0m) };

        await svc.RunAsync(dryRun: true);

        ctx.StockMovements.Where(m => m.MovementType == MovementType.SageAusbuchung)
            .Should().BeEmpty();   // KEIN Write
        fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(1);   // Count trotzdem
    }

    // (l) Verwaistes Paar mit NEGATIVEM WMS-Bestand -> genau eine SageEinbuchung auf 0 (Math.Abs).
    //     Deckt den wmsBestand > 0 ? SageAusbuchung : SageEinbuchung-Negativzweig ab.
    [Fact]
    public async Task Run_OrphanPairWithNegativeStock_BooksSageEinbuchungToZero()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");   // verwaist (nicht in Sage-Read)
        SeedSageLocation(ctx, id: 2, code: "L-P");   // in Sage-Read vorhanden
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1, Quantity = 3m,
            MovementType = MovementType.Ausbuchung, Timestamp = DateTime.Now,
            WindowsUser = "tester", CreatedAt = DateTime.Now,
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
        await ctx.SaveChangesAsync();
        // WMS-Bestand auf L-1 = -3 (reine Ausbuchung). (A-1, L-1) fehlt im Sage-Read -> verwaist.
        reader.Records = new() { new("A-1", "L-P", 0m) };

        var result = await svc.RunAsync(dryRun: false);

        var zeroing = ctx.StockMovements
            .Where(m => m.MovementType == MovementType.SageEinbuchung).ToList();
        zeroing.Should().ContainSingle();
        zeroing[0].StorageLocationId.Should().Be(1);
        zeroing[0].Quantity.Should().Be(3m);   // Math.Abs(-3)
        zeroing[0].WindowsUser.Should().Be(SyncUser);
        zeroing[0].Note.Should().Contain("auf 0 gesetzt");
        fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(1);
        result.CorrectionsPlus.Should().Be(0);   // Zeroing zaehlt NICHT als Korrektur

        // Bestand danach 0: -3 (Ausbuchung) + 3 (SageEinbuchung) = 0
        var stockAfter = await new StockMovementRepository(ctx).GetCurrentStockByArticleAndLocationAsync();
        stockAfter.GetValueOrDefault((1, 1), 0m).Should().Be(0m);
    }

    // (m) Netto-0-Paar (z.B. +5/-5) verwaist -> NICHT genullt (value != 0m-Filter greift).
    [Fact]
    public async Task Run_OrphanPairWithNetZeroStock_NotZeroed()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");   // verwaist, aber netto-0
        SeedSageLocation(ctx, id: 2, code: "L-P");
        ctx.StockMovements.AddRange(
            new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 5m, MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now, WindowsUser = "tester", CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "tester" },
            new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 5m, MovementType = MovementType.Ausbuchung, Timestamp = DateTime.Now, WindowsUser = "tester", CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "tester" }
        );
        await ctx.SaveChangesAsync();
        // (A-1, L-1) fehlt im Sage-Read -> verwaist, aber WMS-Bestand netto 0 -> kein Nullsetzen.
        reader.Records = new() { new("A-1", "L-P", 0m) };

        await svc.RunAsync(dryRun: false);

        ctx.StockMovements.Where(m => m.MovementType >= MovementType.SageEinbuchung)
            .Should().BeEmpty();
        fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(0);
    }
}
