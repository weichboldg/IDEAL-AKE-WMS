using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class BdeOperatorRepositoryTests
{
    [Fact]
    public async Task GetByPersonnelNumber_ReturnsOperator_WhenExists()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.BdeOperators.Add(new BdeOperator {
            PersonnelNumber = "P123", FirstName = "Max", LastName = "Muster",
            IsActive = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var repo = new BdeOperatorRepository(ctx);
        var op = await repo.GetByPersonnelNumberAsync("P123");

        op.Should().NotBeNull();
        op!.FirstName.Should().Be("Max");
    }

    [Fact]
    public async Task GetByPersonnelNumber_ReturnsNull_WhenInactive()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.BdeOperators.Add(new BdeOperator {
            PersonnelNumber = "P999", FirstName = "In", LastName = "Aktiv",
            IsActive = false,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var repo = new BdeOperatorRepository(ctx);
        var op = await repo.GetByPersonnelNumberAsync("P999");

        op.Should().BeNull();
    }

    [Fact]
    public async Task GetAllActive_OrdersByLastName()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.BdeOperators.AddRange(
            new BdeOperator { PersonnelNumber = "2", FirstName = "A", LastName = "Zeta", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new BdeOperator { PersonnelNumber = "1", FirstName = "A", LastName = "Alpha", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" }
        );
        await ctx.SaveChangesAsync();

        var repo = new BdeOperatorRepository(ctx);
        var list = await repo.GetAllActiveAsync();

        list.Select(o => o.LastName).Should().ContainInOrder("Alpha", "Zeta");
    }
}
