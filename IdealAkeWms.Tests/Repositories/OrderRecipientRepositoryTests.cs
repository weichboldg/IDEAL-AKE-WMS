using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;

namespace IdealAkeWms.Tests.Repositories;

public class OrderRecipientRepositoryTests
{
    private static OrderRecipientGroup MakeGroup(string name = "Testgruppe")
    {
        return new OrderRecipientGroup
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
    }

    [Fact]
    public async Task AddGroup_And_GetAllGroups()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new OrderRecipientRepository(context);

        var group = MakeGroup("Einkauf");
        await repo.AddGroupAsync(group);

        var all = await repo.GetAllGroupsAsync();

        all.Should().ContainSingle(g => g.Name == "Einkauf");
        all[0].Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetGroupsByArticleGroupAsync_ReturnsMatchingGroups()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new OrderRecipientRepository(context);

        var group1 = MakeGroup("Einkauf");
        var group2 = MakeGroup("Lager");
        await repo.AddGroupAsync(group1);
        await repo.AddGroupAsync(group2);

        // Map group1 to article group "Elektronik", group2 to "Metall"
        await repo.SetMappingsForArticleGroupAsync("Elektronik", new List<int> { group1.Id }, "test", "DOMAIN\\test");
        await repo.SetMappingsForArticleGroupAsync("Metall", new List<int> { group2.Id }, "test", "DOMAIN\\test");

        var result = await repo.GetGroupsByArticleGroupAsync("Elektronik");

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Einkauf");
    }

    [Fact]
    public async Task GroupHasOpenRequisitionsAsync_ReturnsTrue()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new OrderRecipientRepository(context);

        // Create production order (FK requirement for PartRequisition)
        var order = new ProductionOrder
        {
            OrderNumber = "WA-2607151",
            Quantity = 1,
            ArticleNumber = "S0310395",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        context.ProductionOrders.Add(order);
        await context.SaveChangesAsync();

        var group = MakeGroup("Einkauf");
        await repo.AddGroupAsync(group);

        // Add an open requisition linked to this group
        var requisition = new PartRequisition
        {
            ProductionOrderId = order.Id,
            ArticleNumber = "ART-001",
            Quantity = 1,
            Status = PartRequisitionStatus.Offen,
            Priority = PartRequisitionPriority.Normal,
            OrderRecipientGroupId = group.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        context.PartRequisitions.Add(requisition);
        await context.SaveChangesAsync();

        var hasOpen = await repo.GroupHasOpenRequisitionsAsync(group.Id);
        hasOpen.Should().BeTrue();

        // Group with no requisitions should return false
        var group2 = MakeGroup("Lager");
        await repo.AddGroupAsync(group2);

        var hasOpenEmpty = await repo.GroupHasOpenRequisitionsAsync(group2.Id);
        hasOpenEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task SetMappingsForArticleGroupAsync_ReplacesExisting()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new OrderRecipientRepository(context);

        var group1 = MakeGroup("Einkauf");
        var group2 = MakeGroup("Lager");
        await repo.AddGroupAsync(group1);
        await repo.AddGroupAsync(group2);

        // Initial mapping: "Elektronik" → group1
        await repo.SetMappingsForArticleGroupAsync("Elektronik", new List<int> { group1.Id }, "test", "DOMAIN\\test");

        var before = await repo.GetGroupsByArticleGroupAsync("Elektronik");
        before.Should().ContainSingle(g => g.Name == "Einkauf");

        // Replace with group2 only
        await repo.SetMappingsForArticleGroupAsync("Elektronik", new List<int> { group2.Id }, "test", "DOMAIN\\test");

        var after = await repo.GetGroupsByArticleGroupAsync("Elektronik");
        after.Should().ContainSingle(g => g.Name == "Lager");
        after.Should().NotContain(g => g.Name == "Einkauf");
    }
}
