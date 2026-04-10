using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class UserViewPreferenceRepositoryTests
{
    private ApplicationDbContext CreateContext()
    {
        return TestDbContextFactory.Create();
    }

    private User CreateTestUser(ApplicationDbContext ctx)
    {
        var user = new User
        {
            Name = "TestUser",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        ctx.Users.Add(user);
        ctx.SaveChanges();
        return user;
    }

    [Fact]
    public async Task GetByUserAndViewAsync_ReturnsNull_WhenNoPreference()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);

        var result = await repo.GetByUserAndViewAsync(user.Id, "ProductionOrders");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_CreatesNew_WhenNoExistingPreference()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);
        var json = """{"columns":[{"key":"OrderNumber","visible":true}]}""";

        await repo.SaveAsync(user.Id, "ProductionOrders", json, "test", "test\\user");

        var result = await repo.GetByUserAndViewAsync(user.Id, "ProductionOrders");
        result.Should().NotBeNull();
        result!.SettingsJson.Should().Be(json);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExisting_WhenPreferenceExists()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);
        await repo.SaveAsync(user.Id, "ProductionOrders", "{}", "test", "test\\user");

        var newJson = """{"columns":[{"key":"OrderNumber","visible":false}]}""";
        await repo.SaveAsync(user.Id, "ProductionOrders", newJson, "test2", "test2\\user");

        var result = await repo.GetByUserAndViewAsync(user.Id, "ProductionOrders");
        result!.SettingsJson.Should().Be(newJson);
        result.ModifiedBy.Should().Be("test2");
    }

    [Fact]
    public async Task DeleteByUserAndViewAsync_RemovesPreference()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);
        await repo.SaveAsync(user.Id, "ProductionOrders", "{}", "test", "test\\user");

        await repo.DeleteByUserAndViewAsync(user.Id, "ProductionOrders");

        var result = await repo.GetByUserAndViewAsync(user.Id, "ProductionOrders");
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAllByUserAsync_RemovesAllPreferences()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);
        await repo.SaveAsync(user.Id, "ProductionOrders", "{}", "test", "test\\user");
        await repo.SaveAsync(user.Id, "Picking", "{}", "test", "test\\user");

        await repo.DeleteAllByUserAsync(user.Id);

        var all = await repo.GetAllByUserAsync(user.Id);
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllByUserAsync_ReturnsAllPreferencesForUser()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);
        await repo.SaveAsync(user.Id, "ProductionOrders", "{}", "test", "test\\user");
        await repo.SaveAsync(user.Id, "Picking", "{}", "test", "test\\user");

        var all = await repo.GetAllByUserAsync(user.Id);

        all.Should().HaveCount(2);
        all.Select(p => p.ViewKey).Should().Contain("ProductionOrders");
        all.Select(p => p.ViewKey).Should().Contain("Picking");
    }
}
