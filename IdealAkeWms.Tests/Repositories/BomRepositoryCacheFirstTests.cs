using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class BomRepositoryCacheFirstTests
{
    private static IConfiguration FakeConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OseonConnection"] = "Server=fake;Database=fake;"
            })
            .Build();

    [Fact]
    public async Task GetBomItemsAsync_ReturnsCache_WhenHit()
    {
        var cacheMock = new Mock<IBomCacheRepository>();
        cacheMock.Setup(c => c.GetByArticleNumberAsync("ART1"))
            .ReturnsAsync(new BomQueryResult(
                new List<BomItem>
                {
                    new BomItem { Position = "10", Ressourcenummer = "R1", Menge = 1m }
                },
                "SAGE"));

        using var db = TestDbContextFactory.Create();
        var repo = new BomRepository(db, FakeConfig(), cacheMock.Object);

        var result = await repo.GetBomItemsAsync("ART1");

        result.DataSource.Should().Be("SAGE");
        result.Items.Should().HaveCount(1);
        cacheMock.Verify(c => c.GetByArticleNumberAsync("ART1"), Times.Once);
    }

    [Fact]
    public async Task GetBomItemsAsync_ConsultsCache_WhenCacheMiss()
    {
        // This test verifies that the cache is always consulted first, even on a miss.
        // The live SAGE path will throw on an InMemory DB — we only care that
        // GetByArticleNumberAsync was called before the live path is attempted.
        var cacheMock = new Mock<IBomCacheRepository>();
        cacheMock.Setup(c => c.GetByArticleNumberAsync("ART1")).ReturnsAsync((BomQueryResult?)null);

        using var db = TestDbContextFactory.Create();
        var repo = new BomRepository(db, FakeConfig(), cacheMock.Object);

        // The live SAGE query throws on InMemory DB — that is expected.
        Func<Task> act = () => repo.GetBomItemsAsync("ART1");
        await act.Should().ThrowAsync<Exception>();

        // The key assertion: cache was consulted before attempting the live query.
        cacheMock.Verify(c => c.GetByArticleNumberAsync("ART1"), Times.Once);
    }
}
