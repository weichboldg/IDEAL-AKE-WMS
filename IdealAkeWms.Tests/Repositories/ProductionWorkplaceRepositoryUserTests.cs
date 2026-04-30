using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class ProductionWorkplaceRepositoryUserTests
{
    private static User NewUser(string name)
        => new() { Name = name, IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };

    private static ProductionWorkplace NewWorkplace(string name)
        => new() { Name = name, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };

    [Fact]
    public async Task GetByUserId_NoAssignment_ReturnsEmpty()
    {
        var ctx = TestDbContextFactory.Create();
        var u = NewUser("u1");
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();

        var repo = new ProductionWorkplaceRepository(ctx);
        var result = await repo.GetByUserIdAsync(u.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserId_OneAssignment_ReturnsOne()
    {
        var ctx = TestDbContextFactory.Create();
        var u = NewUser("u1"); var wp = NewWorkplace("WB-A");
        ctx.Users.Add(u); ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();
        ctx.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = u.Id, ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var repo = new ProductionWorkplaceRepository(ctx);
        var result = await repo.GetByUserIdAsync(u.Id);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("WB-A");
    }

    [Fact]
    public async Task GetByUserId_MultipleAssignments_ReturnsAlphabetical()
    {
        var ctx = TestDbContextFactory.Create();
        var u = NewUser("u1");
        var wpC = NewWorkplace("WB-C"); var wpA = NewWorkplace("WB-A"); var wpB = NewWorkplace("WB-B");
        ctx.Users.Add(u); ctx.ProductionWorkplaces.AddRange(wpC, wpA, wpB);
        await ctx.SaveChangesAsync();
        ctx.ProductionWorkplaceUsers.AddRange(
            new ProductionWorkplaceUser { UserId = u.Id, ProductionWorkplaceId = wpC.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionWorkplaceUser { UserId = u.Id, ProductionWorkplaceId = wpA.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionWorkplaceUser { UserId = u.Id, ProductionWorkplaceId = wpB.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var repo = new ProductionWorkplaceRepository(ctx);
        var result = await repo.GetByUserIdAsync(u.Id);

        result.Select(w => w.Name).Should().ContainInOrder("WB-A", "WB-B", "WB-C");
    }
}
