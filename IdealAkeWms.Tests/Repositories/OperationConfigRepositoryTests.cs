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

    [Fact]
    public async Task GetUnconfiguredOperationNamesAsync_FindsNamesWithoutConfig()
    {
        using var context = TestDbContextFactory.Create();

        // Config nur fuer ST
        context.OperationConfigs.Add(CreateConfig("ST", 0));

        // OSEON-Daten mit ST, BG und ZB
        var order = new OseonProductionOrder
        {
            OseonId = 1, OseonOrderNumber = "TEST", OseonStatus = 60,
            CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test"
        };
        context.OseonProductionOrders.Add(order);
        await context.SaveChangesAsync();

        context.OseonWorkOperations.AddRange(
            new OseonWorkOperation { OseonProductionOrderId = order.Id, PositionNumber = "10", Name = "ST", OseonStatus = 90, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" },
            new OseonWorkOperation { OseonProductionOrderId = order.Id, PositionNumber = "20", Name = "BG", OseonStatus = 60, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" },
            new OseonWorkOperation { OseonProductionOrderId = order.Id, PositionNumber = "30", Name = "ZB", OseonStatus = 30, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" }
        );
        await context.SaveChangesAsync();

        var repo = new OperationConfigRepository(context);
        var unconfigured = await repo.GetUnconfiguredOperationNamesAsync();

        unconfigured.Should().Contain("BG");
        unconfigured.Should().Contain("ZB");
        unconfigured.Should().NotContain("ST");
    }
}
