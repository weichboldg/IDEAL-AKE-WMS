using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;

namespace IdealAkeWms.Tests.Repositories;

public class UserRepositoryWindowsAuthTests
{
    [Fact]
    public async Task GetActiveByWindowsUserNameAsync_MatchesCaseInsensitive_AndOnlyActive()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Users.Add(new User { Name = "A", WindowsUserName = "jmuster", IsActive = true, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.Users.Add(new User { Name = "B", WindowsUserName = "inaktiv", IsActive = false, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.Users.Add(new User { Name = "C", WindowsUserName = null, IsActive = true, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();
        var repo = new UserRepository(ctx);

        (await repo.GetActiveByWindowsUserNameAsync("JMUSTER"))!.Name.Should().Be("A");
        (await repo.GetActiveByWindowsUserNameAsync("inaktiv")).Should().BeNull();
        (await repo.GetActiveByWindowsUserNameAsync("unbekannt")).Should().BeNull();
        (await repo.GetActiveByWindowsUserNameAsync("")).Should().BeNull();
    }
}
