using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class BdeTerminalRepositoryTests
{
    [Fact]
    public async Task GetByUserId_ReturnsTerminal_WithWorkplaceIncluded()
    {
        using var ctx = TestDbContextFactory.Create();
        var user = new User { Name = "Terminal 1", PasswordHash = "x", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wp = new ProductionWorkplace { Name = "Werkbank 1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.Users.Add(user);
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();
        ctx.BdeTerminals.Add(new BdeTerminal
        {
            UserId = user.Id,
            DefaultProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var repo = new BdeTerminalRepository(ctx);
        var t = await repo.GetByUserIdAsync(user.Id);

        t.Should().NotBeNull();
        t!.DefaultProductionWorkplace.Should().NotBeNull();
        t.DefaultProductionWorkplace.Name.Should().Be("Werkbank 1");
    }

    [Fact]
    public async Task GetByUserId_ReturnsNull_WhenUserHasNoTerminal()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new BdeTerminalRepository(ctx);
        (await repo.GetByUserIdAsync(999)).Should().BeNull();
    }

    [Fact]
    public async Task GetAll_OrdersByWorkplaceName()
    {
        using var ctx = TestDbContextFactory.Create();
        var u1 = new User { Name = "u1", PasswordHash = "x", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var u2 = new User { Name = "u2", PasswordHash = "x", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wpZ = new ProductionWorkplace { Name = "Zeta", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wpA = new ProductionWorkplace { Name = "Alpha", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.Users.AddRange(u1, u2);
        ctx.ProductionWorkplaces.AddRange(wpZ, wpA);
        await ctx.SaveChangesAsync();
        ctx.BdeTerminals.AddRange(
            new BdeTerminal { UserId = u1.Id, DefaultProductionWorkplaceId = wpZ.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new BdeTerminal { UserId = u2.Id, DefaultProductionWorkplaceId = wpA.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" }
        );
        await ctx.SaveChangesAsync();

        var repo = new BdeTerminalRepository(ctx);
        var list = await repo.GetAllAsync();

        list.Select(t => t.DefaultProductionWorkplace.Name).Should().ContainInOrder("Alpha", "Zeta");
    }
}
