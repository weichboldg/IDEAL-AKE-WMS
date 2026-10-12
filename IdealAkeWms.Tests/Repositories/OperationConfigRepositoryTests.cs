using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class OperationConfigRepositoryTests
{
    private static OperationConfig CreateConfig(string name, int offset, bool relevant = true, string? display = null)
        => new() { OperationName = name, DisplayName = display, DueDateOffsetDays = offset, IsExternalSystemRelevant = relevant };

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.AddRange(
            CreateConfig("ZB", 0, false),
            CreateConfig("BG", 2),
            CreateConfig("A-BT", 0, false));
        await context.SaveChangesAsync();

        var repo = new OperationConfigRepository(context);
        var result = await repo.GetAllAsync();

        result.Should().HaveCount(3);
        result[0].OperationName.Should().Be("A-BT");
        result[1].OperationName.Should().Be("BG");
        result[2].OperationName.Should().Be("ZB");
    }

    [Fact]
    public async Task GetByNameAsync_FindsCorrectConfig()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.Add(CreateConfig("ST", 0, true, "Stanzen"));
        await context.SaveChangesAsync();

        var repo = new OperationConfigRepository(context);
        var result = await repo.GetByNameAsync("ST");

        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Stanzen");
        result.DueDateOffsetDays.Should().Be(0);
        result.IsExternalSystemRelevant.Should().BeTrue();
    }

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new OperationConfigRepository(context);

        var result = await repo.GetByNameAsync("NOPE");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsDictionaryAsync_ReturnsDictKeyedByName()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.AddRange(
            CreateConfig("B", -1),
            CreateConfig("ST", 0),
            CreateConfig("ZB", 0, false));
        await context.SaveChangesAsync();

        var repo = new OperationConfigRepository(context);
        var dict = await repo.GetAllAsDictionaryAsync();

        dict.Should().HaveCount(3);
        dict["B"].DueDateOffsetDays.Should().Be(-1);
        dict["ST"].IsExternalSystemRelevant.Should().BeTrue();
        dict["ZB"].IsExternalSystemRelevant.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrueForExisting()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.Add(CreateConfig("BG", 2));
        await context.SaveChangesAsync();

        var repo = new OperationConfigRepository(context);

        (await repo.ExistsAsync("BG")).Should().BeTrue();
        (await repo.ExistsAsync("XX")).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_PersistsConfig()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new OperationConfigRepository(context);

        await repo.AddAsync(CreateConfig("MS", 4, true, "Maschinenschub"));

        context.OperationConfigs.Should().HaveCount(1);
        var saved = context.OperationConfigs.First();
        saved.OperationName.Should().Be("MS");
        saved.DueDateOffsetDays.Should().Be(4);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesConfig()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.Add(CreateConfig("SL", 5));
        await context.SaveChangesAsync();

        var repo = new OperationConfigRepository(context);
        var config = await repo.GetByNameAsync("SL");
        config!.DueDateOffsetDays = 3;
        config.IsExternalSystemRelevant = false;
        await repo.UpdateAsync(config);

        var updated = await repo.GetByNameAsync("SL");
        updated!.DueDateOffsetDays.Should().Be(3);
        updated.IsExternalSystemRelevant.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesConfig()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.Add(CreateConfig("RE", 5));
        await context.SaveChangesAsync();

        var repo = new OperationConfigRepository(context);
        var config = await repo.GetByNameAsync("RE");
        await repo.DeleteAsync(config!.Id);

        context.OperationConfigs.Should().BeEmpty();
    }

    private static async Task<int> SeedOseonAsync(IdealAkeWms.Data.ApplicationDbContext context, params string[] names)
    {
        var order = new OseonProductionOrder
        {
            OseonId = 1, OseonOrderNumber = "TEST", OseonStatus = 60,
            CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test"
        };
        context.OseonProductionOrders.Add(order);
        await context.SaveChangesAsync();
        var pos = 10;
        foreach (var n in names)
            context.OseonWorkOperations.Add(new OseonWorkOperation
            {
                OseonProductionOrderId = order.Id, PositionNumber = (pos += 10).ToString(), Name = n, OseonStatus = 60,
                CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test"
            });
        await context.SaveChangesAsync();
        return order.Id;
    }

    private static void SeedWms(IdealAkeWms.Data.ApplicationDbContext context, params string[] numbers)
    {
        foreach (var n in numbers)
            context.WorkOperations.Add(new WorkOperation
            {
                ProductionOrderId = 1, OperationNumber = n, Name = n, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test"
            });
        context.SaveChanges();
    }

    // ===== Liste „nicht konfiguriert" (Spec 2026-10-09 FA 5, AK 8) =====

    [Fact]
    public async Task GetUnconfiguredOperationsAsync_ZweiQuellen_OhneDefault01_CaseInsensitivGetrimmt()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.AddRange(CreateConfig("ST", 0), CreateConfig("KA", 3));
        await context.SaveChangesAsync();
        await SeedOseonAsync(context, "ST", "BG", "ZB", "LS");
        SeedWms(context, "ka", " LS ", "PG", "01", "PG");

        var result = await new OperationConfigRepository(context).GetUnconfiguredOperationsAsync();

        result.Select(u => u.Kuerzel).Should().Equal("BG", "LS", "PG", "ZB");
        result.Single(u => u.Kuerzel == "LS").Quellen.Should().Equal("OSEON", "WMS-Arbeitsgang");
        result.Single(u => u.Kuerzel == "PG").Quellen.Should().Equal("WMS-Arbeitsgang");
        result.Single(u => u.Kuerzel == "BG").Quellen.Should().Equal("OSEON");
    }

    [Fact]
    public async Task GetUnconfiguredOperationsAsync_NachAnlegenVerschwindetDasKuerzel()
    {
        using var context = TestDbContextFactory.Create();
        SeedWms(context, "KA");
        var repo = new OperationConfigRepository(context);
        (await repo.GetUnconfiguredOperationsAsync()).Should().ContainSingle(u => u.Kuerzel == "KA");

        await repo.AddAsync(CreateConfig("ka", 3));

        (await repo.GetUnconfiguredOperationsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetUnconfiguredOperationsAsync_OhneDaten_Leer()
    {
        using var context = TestDbContextFactory.Create();
        (await new OperationConfigRepository(context).GetUnconfiguredOperationsAsync()).Should().BeEmpty();
    }

    // ===== Offset-Lookup (FA 6, AK 9, AK 17) =====

    [Fact]
    public async Task GetDueDateOffsetsAsync_CaseInsensitiv_Getrimmt_Null_Fehlend_NichtRelevantEnthalten()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.AddRange(CreateConfig(" KA ", 3), CreateConfig("ST", 0), CreateConfig("ZB", 2, relevant: false));
        await context.SaveChangesAsync();

        var offsets = await new OperationConfigRepository(context).GetDueDateOffsetsAsync();

        offsets["ka"].Should().Be(3);
        offsets[" KA ".Trim()].Should().Be(3);
        offsets.ContainsKey("ST").Should().BeTrue();
        offsets["ST"].Should().Be(0);
        offsets.ContainsKey("XX").Should().BeFalse("kein Eintrag ist vom Wert 0 unterscheidbar");
        offsets["ZB"].Should().Be(2, "der Lookup wertet Fremdsystem-relevant nicht aus");
    }

    [Fact]
    public async Task GetDueDateOffsetsAsync_DoppelterNormalisierterSchluessel_KleinsteIdGewinnt()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.Add(CreateConfig("KA", 1));
        await context.SaveChangesAsync();
        context.OperationConfigs.Add(CreateConfig("ka ", 9)); // InMemory erzwingt den Unique-Index nicht — Altdaten-Fall
        await context.SaveChangesAsync();

        (await new OperationConfigRepository(context).GetDueDateOffsetsAsync())["KA"].Should().Be(1);
    }

    [Fact]
    public async Task ExistsAsync_GetrimmtUndCaseInsensitiv()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.Add(CreateConfig("KA", 1));
        await context.SaveChangesAsync();
        var repo = new OperationConfigRepository(context);

        (await repo.ExistsAsync(" ka ")).Should().BeTrue();
        (await repo.ExistsAsync("kb")).Should().BeFalse();
    }

    // ===== Decorator (AK 10) =====

    [Fact]
    public async Task Decorator_CachtOffsets_UndInvalidiertNachSchreiben()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.Add(CreateConfig("KA", 1));
        await context.SaveChangesAsync();
        var inner = new OperationConfigRepository(context);
        var cached = new CachedOperationConfigRepository(inner, new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

        var first = await cached.GetDueDateOffsetsAsync();
        context.OperationConfigs.Add(CreateConfig("PG", 4)); // an der Decorator-Kante vorbei
        await context.SaveChangesAsync();
        (await cached.GetDueDateOffsetsAsync()).Should().BeSameAs(first, "zweiter Aufruf aus dem Cache, ohne DB-Zugriff");

        await cached.AddAsync(CreateConfig("LS", 2));
        (await cached.GetDueDateOffsetsAsync()).Keys.Should().BeEquivalentTo(new[] { "KA", "PG", "LS" });

        var ls = context.OperationConfigs.Single(c => c.OperationName == "LS");
        ls.DueDateOffsetDays = 7;
        await cached.UpdateAsync(ls);
        (await cached.GetDueDateOffsetsAsync())["LS"].Should().Be(7);

        await cached.DeleteAsync(ls.Id);
        (await cached.GetDueDateOffsetsAsync()).ContainsKey("LS").Should().BeFalse();
    }

    // ===== OSEON-Regression (FA 7, AK 11) =====

    [Fact]
    public async Task GetAllAsDictionaryAsync_BleibtCaseSensitiv()
    {
        using var context = TestDbContextFactory.Create();
        context.OperationConfigs.Add(CreateConfig("BG", 2));
        await context.SaveChangesAsync();

        var dict = await new OperationConfigRepository(context).GetAllAsDictionaryAsync();

        dict.ContainsKey("BG").Should().BeTrue();
        dict.ContainsKey("bg").Should().BeFalse("die OSEON-Teileverfolgung vergleicht bewusst case-sensitiv (unveraendert)");
    }
}
