using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer das FaAttributeRepository (FA-Vorbau v1.22.0, Plan Task 4).
/// </summary>
public class FaAttributeRepositoryTests
{
    [Fact]
    public async Task UpsertValue_CreatesUpdatesAndClearsRow()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new FaAttributeRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
        var def = new FaAttributeDefinition { Id = 5, Name = "Verdampfergroesse", AttributeType = AttributeType.Dropdown };
        var opt = new FaAttributeOption { Id = 50, FaAttributeDefinitionId = 5, Value = "UKW 3/1" };
        ctx.FaAttributeDefinitions.Add(def); ctx.FaAttributeOptions.Add(opt);
        await ctx.SaveChangesAsync();

        await repo.UpsertValueAsync(1, 5, 50, null, null, "t", "w");
        ctx.FaAttributeValues.Should().ContainSingle(v => v.SelectedOptionId == 50);

        await repo.UpsertValueAsync(1, 5, null, null, null, "t", "w"); // "leer" -> Zeile weg
        ctx.FaAttributeValues.Should().BeEmpty();
    }

    [Fact]
    public async Task UpsertValue_StoresAndClearsTextValue()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new FaAttributeRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
        ctx.FaAttributeDefinitions.Add(new FaAttributeDefinition { Id = 9, Name = "Seriennummer", AttributeType = AttributeType.Text });
        await ctx.SaveChangesAsync();

        // Text-Wert anlegen
        await repo.UpsertValueAsync(1, 9, null, null, "ABC-123", "t", "w");
        ctx.FaAttributeValues.Should().ContainSingle(v => v.TextValue == "ABC-123");

        // Text-Wert aktualisieren
        await repo.UpsertValueAsync(1, 9, null, null, "XYZ-999", "t", "w");
        ctx.FaAttributeValues.Should().ContainSingle(v => v.TextValue == "XYZ-999");

        // Leerer/Whitespace-Text -> Zeile loeschen
        await repo.UpsertValueAsync(1, 9, null, null, "   ", "t", "w");
        ctx.FaAttributeValues.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteOption_ReturnsFalse_WhenValuesReference()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new FaAttributeRepository(ctx);
        ctx.FaAttributeDefinitions.Add(new FaAttributeDefinition { Id = 5, Name = "X", AttributeType = AttributeType.Dropdown });
        ctx.FaAttributeOptions.Add(new FaAttributeOption { Id = 50, FaAttributeDefinitionId = 5, Value = "A" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
        ctx.FaAttributeValues.Add(new FaAttributeValue { ProductionOrderId = 1, FaAttributeDefinitionId = 5, SelectedOptionId = 50 });
        await ctx.SaveChangesAsync();

        (await repo.DeleteOptionAsync(50)).Should().BeFalse();
        ctx.FaAttributeOptions.Should().ContainSingle();
    }

    [Fact]
    public async Task GetActiveForWorkSteps_FiltersByJunctionAndActive()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new FaAttributeRepository(ctx);
        ctx.WorkSteps.Add(new WorkStep { Id = 10, Code = "VK", Name = "K" });
        ctx.FaAttributeDefinitions.AddRange(
            new FaAttributeDefinition { Id = 5, Name = "Zugeordnet", IsActive = true },
            new FaAttributeDefinition { Id = 6, Name = "Inaktiv", IsActive = false },
            new FaAttributeDefinition { Id = 7, Name = "NichtZugeordnet", IsActive = true });
        ctx.FaAttributeWorkSteps.AddRange(
            new FaAttributeWorkStep { FaAttributeDefinitionId = 5, WorkStepId = 10 },
            new FaAttributeWorkStep { FaAttributeDefinitionId = 6, WorkStepId = 10 });
        await ctx.SaveChangesAsync();

        var result = await repo.GetActiveForWorkStepsAsync(new List<int> { 10 });
        result.Should().ContainSingle(d => d.Name == "Zugeordnet");
    }
}
