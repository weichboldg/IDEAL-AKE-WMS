using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;

namespace IdealAkeWms.Tests.Repositories;

public class ProductionWorkplaceUserTests
{
    private static ProductionWorkplace CreateWorkplace(string name)
    {
        return new ProductionWorkplace
        {
            Name = name,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
    }

    private static User CreateUser(string name)
    {
        return new User
        {
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
    }

    [Fact]
    public async Task SetProductionWorkplaceUsersAsync_AssignsUsers()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        var workplace = CreateWorkplace("Werkbank 1");
        await repo.AddAsync(workplace);

        var user1 = CreateUser("Alice");
        var user2 = CreateUser("Bob");
        ctx.Users.AddRange(user1, user2);
        await ctx.SaveChangesAsync();

        await repo.SetProductionWorkplaceUsersAsync(
            workplace.Id, new List<int> { user1.Id, user2.Id }, "Test", "TEST\\user");

        var loaded = await repo.GetByIdWithUsersAsync(workplace.Id);

        loaded.Should().NotBeNull();
        loaded!.ProductionWorkplaceUsers.Should().HaveCount(2);
        loaded.ProductionWorkplaceUsers.Select(wu => wu.UserId)
            .Should().Contain(new[] { user1.Id, user2.Id });
    }

    [Fact]
    public async Task SetProductionWorkplaceUsersAsync_ReplacesExistingUsers()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        var workplace = CreateWorkplace("Werkbank 2");
        await repo.AddAsync(workplace);

        var user1 = CreateUser("Alice");
        var user2 = CreateUser("Bob");
        var user3 = CreateUser("Charlie");
        ctx.Users.AddRange(user1, user2, user3);
        await ctx.SaveChangesAsync();

        // Erst user1+user2 zuordnen
        await repo.SetProductionWorkplaceUsersAsync(
            workplace.Id, new List<int> { user1.Id, user2.Id }, "Test", "TEST\\user");

        // Dann nur user2+user3
        await repo.SetProductionWorkplaceUsersAsync(
            workplace.Id, new List<int> { user2.Id, user3.Id }, "Test", "TEST\\user");

        var loaded = await repo.GetByIdWithUsersAsync(workplace.Id);

        loaded!.ProductionWorkplaceUsers.Should().HaveCount(2);
        loaded.ProductionWorkplaceUsers.Select(wu => wu.UserId)
            .Should().Contain(new[] { user2.Id, user3.Id })
            .And.NotContain(user1.Id);
    }

    [Fact]
    public async Task SetProductionWorkplaceUsersAsync_EmptyList_RemovesAll()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        var workplace = CreateWorkplace("Werkbank 3");
        await repo.AddAsync(workplace);

        var user1 = CreateUser("Alice");
        ctx.Users.Add(user1);
        await ctx.SaveChangesAsync();

        await repo.SetProductionWorkplaceUsersAsync(
            workplace.Id, new List<int> { user1.Id }, "Test", "TEST\\user");

        await repo.SetProductionWorkplaceUsersAsync(
            workplace.Id, new List<int>(), "Test", "TEST\\user");

        var loaded = await repo.GetByIdWithUsersAsync(workplace.Id);
        loaded!.ProductionWorkplaceUsers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllWithUsersOrderedAsync_IncludesUserData()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        var workplace = CreateWorkplace("Werkbank A");
        await repo.AddAsync(workplace);

        var user = CreateUser("Alice");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        await repo.SetProductionWorkplaceUsersAsync(
            workplace.Id, new List<int> { user.Id }, "Test", "TEST\\user");

        var result = await repo.GetAllWithUsersOrderedAsync();

        result.Should().HaveCount(1);
        result.First().ProductionWorkplaceUsers.Should().HaveCount(1);
        result.First().ProductionWorkplaceUsers.First().User.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetByIdWithUsersAsync_UnknownId_ReturnsNull()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        var result = await repo.GetByIdWithUsersAsync(9999);

        result.Should().BeNull();
    }
}
