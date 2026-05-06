using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

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
}
