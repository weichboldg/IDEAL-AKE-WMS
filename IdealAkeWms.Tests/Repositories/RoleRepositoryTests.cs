using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class RoleRepositoryTests
{
    // v1.99.18 (Spec 2026-11-13-utc-schreiber-auf-lokalzeit, ADR 0025): Rollen-Zuweisung schreibt Lokalzeit.
    [Fact]
    public async Task SetUserRolesAsync_SchreibtCreatedAtInLokalzeit()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new RoleRepository(context);

        await repo.SetUserRolesAsync(1, new List<int> { 7 }, "test", "DOMAIN\test");

        context.UserRoles.Single().CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }
}
