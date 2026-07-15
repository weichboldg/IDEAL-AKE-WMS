using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Tests.Repositories;

public class SyncLogRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsEntry()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new SyncLogRepository(ctx);

        await repo.AddAsync(new SyncLog
        {
            Service = "Lagerplatz",
            Level = SyncLogLevel.Warning,
            Message = "Konflikt: ABC manuell",
            Reference = "ABC"
        });

        var all = await repo.GetRecentAsync(service: null, level: null, limit: 10);
        all.Should().ContainSingle();
        all[0].Service.Should().Be("Lagerplatz");
        all[0].Reference.Should().Be("ABC");
    }

    [Fact]
    public async Task GetRecentAsync_FiltersByServiceAndLevel_OrdersDesc()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new SyncLogRepository(ctx);

        await repo.AddAsync(new SyncLog { Service = "Lagerplatz", Level = SyncLogLevel.Info,    Message = "A", Timestamp = new DateTime(2026, 5, 1) });
        await repo.AddAsync(new SyncLog { Service = "Lagerplatz", Level = SyncLogLevel.Warning, Message = "B", Timestamp = new DateTime(2026, 5, 3) });
        await repo.AddAsync(new SyncLog { Service = "OseonTracking", Level = SyncLogLevel.Warning, Message = "C", Timestamp = new DateTime(2026, 5, 2) });

        var lagerplatzWarnings = await repo.GetRecentAsync(service: "Lagerplatz", level: SyncLogLevel.Warning, limit: 10);
        lagerplatzWarnings.Should().HaveCount(1);
        lagerplatzWarnings[0].Message.Should().Be("B");

        var allDesc = await repo.GetRecentAsync(service: null, level: null, limit: 10);
        allDesc.Select(x => x.Message).Should().ContainInOrder("B", "C", "A");
    }

    [Fact]
    public async Task DeleteOlderThanAsync_deletes_only_older_and_returns_count()
    {
        using var ctx = TestDbContextFactory.Create();
        var cutoff = new DateTime(2026, 06, 01);
        ctx.SyncLogs.AddRange(
            new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = "alt1",  Timestamp = cutoff.AddDays(-10) },
            new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = "alt2",  Timestamp = cutoff.AddDays(-1)  },
            new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = "neu",   Timestamp = cutoff.AddDays(1)   });
        await ctx.SaveChangesAsync();
        var repo = new SyncLogRepository(ctx);

        var deleted = await repo.DeleteOlderThanAsync(cutoff, batchSize: 5000, dryRun: false);

        deleted.Should().Be(2);
        (await ctx.SyncLogs.CountAsync()).Should().Be(1);
        (await ctx.SyncLogs.SingleAsync()).Message.Should().Be("neu");
    }

    [Fact]
    public async Task DeleteOlderThanAsync_dryRun_counts_but_does_not_delete()
    {
        using var ctx = TestDbContextFactory.Create();
        var cutoff = new DateTime(2026, 06, 01);
        ctx.SyncLogs.AddRange(
            new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = "alt", Timestamp = cutoff.AddDays(-1) },
            new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = "neu", Timestamp = cutoff.AddDays(1)  });
        await ctx.SaveChangesAsync();
        var repo = new SyncLogRepository(ctx);

        var wouldDelete = await repo.DeleteOlderThanAsync(cutoff, batchSize: 5000, dryRun: true);

        wouldDelete.Should().Be(1);
        (await ctx.SyncLogs.CountAsync()).Should().Be(2); // nichts geloescht
    }

    [Fact]
    public async Task DeleteOlderThanAsync_loops_over_batches()
    {
        using var ctx = TestDbContextFactory.Create();
        var cutoff = new DateTime(2026, 06, 01);
        for (var i = 0; i < 5; i++)
            ctx.SyncLogs.Add(new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = $"alt{i}", Timestamp = cutoff.AddDays(-1) });
        await ctx.SaveChangesAsync();
        var repo = new SyncLogRepository(ctx);

        var deleted = await repo.DeleteOlderThanAsync(cutoff, batchSize: 2, dryRun: false);

        deleted.Should().Be(5);
        (await ctx.SyncLogs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_terminates_on_exact_multiple_of_batchSize()
    {
        using var ctx = TestDbContextFactory.Create();
        var cutoff = new DateTime(2026, 06, 01);
        for (var i = 0; i < 4; i++)
            ctx.SyncLogs.Add(new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = $"alt{i}", Timestamp = cutoff.AddDays(-1) });
        await ctx.SaveChangesAsync();
        var repo = new SyncLogRepository(ctx);

        var deleted = await repo.DeleteOlderThanAsync(cutoff, batchSize: 2, dryRun: false);

        deleted.Should().Be(4);
        (await ctx.SyncLogs.CountAsync()).Should().Be(0);
    }
}
