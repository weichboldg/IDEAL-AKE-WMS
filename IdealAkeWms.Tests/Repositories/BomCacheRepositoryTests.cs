using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class BomCacheRepositoryTests
{
    private static BomItem Item(string pos, string ressourcenummer, decimal menge, string bez1 = "")
        => new BomItem
        {
            Position = pos,
            Ressourcenummer = ressourcenummer,
            Menge = menge,
            Bezeichnung1 = bez1,
            Bezeichnung2 = "",
            Baugruppe = "",
            Beschaffungsartikel = "",
            Artikelgruppe = ""
        };

    [Fact]
    public async Task GetByArticleNumberAsync_ReturnsNull_WhenNoHeader()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new BomCacheRepository(db);

        var result = await repo.GetByArticleNumberAsync("DOES_NOT_EXIST");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByArticleNumberAsync_ReturnsItems_WhenHeaderExists()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new BomCacheRepository(db);

        await repo.UpsertBomAsync("ART1", "SAGE", "hash1", new List<BomItem>
        {
            Item("10", "R1", 2m, "Item 1"),
            Item("20", "R2", 3m, "Item 2")
        });

        var result = await repo.GetByArticleNumberAsync("ART1");

        result.Should().NotBeNull();
        result!.DataSource.Should().Be("SAGE");
        result.Items.Should().HaveCount(2);
        result.Items[0].Position.Should().Be("10");
        result.Items[1].Position.Should().Be("20");
    }

    [Fact]
    public async Task UpsertBomAsync_InsertsNewHeader()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new BomCacheRepository(db);

        await repo.UpsertBomAsync("ART1", "SAGE", "hash1", new List<BomItem> { Item("10", "R1", 1m) });

        db.CachedBomHeaders.Should().HaveCount(1);
        db.CachedBomItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpsertBomAsync_ReplacesExistingItems()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new BomCacheRepository(db);

        await repo.UpsertBomAsync("ART1", "SAGE", "hash1", new List<BomItem>
        {
            Item("10", "R1", 1m), Item("20", "R2", 2m)
        });
        await repo.UpsertBomAsync("ART1", "OSEON", "hash2", new List<BomItem>
        {
            Item("30", "R3", 3m)
        });

        db.CachedBomHeaders.Should().HaveCount(1);
        var header = db.CachedBomHeaders.Single();
        header.Source.Should().Be("OSEON");
        header.ContentHash.Should().Be("hash2");
        header.ItemCount.Should().Be(1);
        db.CachedBomItems.Should().HaveCount(1);
        db.CachedBomItems.Single().Position.Should().Be("30");
    }

    [Fact]
    public async Task DeleteOrphansAsync_RemovesHeadersNotInList()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new BomCacheRepository(db);

        await repo.UpsertBomAsync("ART1", "SAGE", "h", new List<BomItem> { Item("10", "R1", 1m) });
        await repo.UpsertBomAsync("ART2", "SAGE", "h", new List<BomItem> { Item("10", "R1", 1m) });
        await repo.UpsertBomAsync("ART3", "SAGE", "h", new List<BomItem> { Item("10", "R1", 1m) });

        await repo.DeleteOrphansAsync(new List<string> { "ART1", "ART3" });

        var remaining = db.CachedBomHeaders.Select(h => h.Artikelnummer).OrderBy(a => a).ToList();
        remaining.Should().BeEquivalentTo(new[] { "ART1", "ART3" });
    }

    [Fact]
    public async Task GetHeaderHashesAsync_ReturnsExistingHashesOnly()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new BomCacheRepository(db);

        await repo.UpsertBomAsync("ART1", "SAGE", "hash-1", new List<BomItem> { Item("10", "R1", 1m) });
        await repo.UpsertBomAsync("ART2", "SAGE", "hash-2", new List<BomItem> { Item("10", "R1", 1m) });

        var result = await repo.GetHeaderHashesAsync(new List<string> { "ART1", "ART2", "ART3" });

        result.Should().HaveCount(2);
        result["ART1"].Hash.Should().Be("hash-1");
        result["ART2"].Hash.Should().Be("hash-2");
        result.ContainsKey("ART3").Should().BeFalse();
    }

    [Fact]
    public async Task GetArticleNumbersWithCoatingPartsAsync_FindsMatchingCategory()
    {
        using var db = TestDbContextFactory.Create();

        var cat = new ArticleCategory { Id = 1, Name = "Lackierteil" };
        db.ArticleCategories.Add(cat);
        db.Articles.Add(new Article { Id = 1, ArticleNumber = "BAUTEIL1", Description = "X", ArticleCategoryId = 1 });
        db.SaveChanges();

        var repo = new BomCacheRepository(db);
        await repo.UpsertBomAsync("ART1", "SAGE", "h", new List<BomItem>
        {
            Item("10", "BAUTEIL1", 1m),
            Item("20", "OTHER", 1m)
        });
        await repo.UpsertBomAsync("ART2", "SAGE", "h", new List<BomItem>
        {
            Item("10", "OTHER", 1m)
        });

        var result = await repo.GetArticleNumbersWithCoatingPartsAsync(
            "Lackierteil",
            new List<string> { "ART1", "ART2" });

        result.Should().BeEquivalentTo(new[] { "ART1" });
    }

    [Fact]
    public async Task GetArticleNumbersWithCoatingPartsAsync_ReturnsEmpty_WhenCategoryNameBlank()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new BomCacheRepository(db);

        var result = await repo.GetArticleNumbersWithCoatingPartsAsync("", new List<string> { "ART1" });

        result.Should().BeEmpty();
    }
}
