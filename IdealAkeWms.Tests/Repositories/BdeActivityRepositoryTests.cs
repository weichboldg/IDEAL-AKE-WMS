using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class BdeActivityRepositoryTests
{
    [Fact]
    public async Task GetAllActive_FiltersInactive()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.BdeActivities.AddRange(
            new BdeActivity { Code = "WART", Name = "Wartung", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new BdeActivity { Code = "OBSOLETE", Name = "Alt", IsActive = false, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" }
        );
        await ctx.SaveChangesAsync();
        var repo = new BdeActivityRepository(ctx);
        var list = await repo.GetAllActiveAsync();
        list.Should().HaveCount(1);
        list[0].Code.Should().Be("WART");
    }

    [Fact]
    public async Task GetByCode_ReturnsActivity()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.BdeActivities.Add(new BdeActivity { Code = "REIN", Name = "Reinigung", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();
        var repo = new BdeActivityRepository(ctx);
        var a = await repo.GetByCodeAsync("REIN");
        a.Should().NotBeNull();
        a!.Name.Should().Be("Reinigung");
    }

    [Fact]
    public async Task GetByCode_ReturnsNull_ForInactive()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.BdeActivities.Add(new BdeActivity { Code = "OFF", Name = "Deaktiv", IsActive = false, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();
        var repo = new BdeActivityRepository(ctx);
        var a = await repo.GetByCodeAsync("OFF");
        a.Should().BeNull();
    }
}
