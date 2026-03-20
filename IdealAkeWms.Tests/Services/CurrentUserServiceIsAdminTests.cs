using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IdealAkeWms.Tests.Services;

public class CurrentUserServiceIsAdminTests
{
    /// <summary>
    /// Einfache Fake-ISession — zuverlässiger als Moq für out-Parameter.
    /// </summary>
    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new();

        public bool IsAvailable => true;
        public string Id => "fake-session";
        public IEnumerable<string> Keys => _data.Keys;

        public void Set(string key, byte[] value) => _data[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _data.TryGetValue(key, out value!);
        public void Remove(string key) => _data.Remove(key);
        public void Clear() => _data.Clear();
        public void Load() { }
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Commit() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static (CurrentUserService service, Mock<IRoleRepository> roleRepoMock) CreateService(
        int? sessionUserId, List<string>? roleKeys = null)
    {
        var session = new FakeSession();
        if (sessionUserId.HasValue)
        {
            var v = sessionUserId.Value;
            session.Set("AppUserId", new byte[] { (byte)v, (byte)(v >> 8), (byte)(v >> 16), (byte)(v >> 24) });
        }

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(session);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var roleRepoMock = new Mock<IRoleRepository>();
        roleRepoMock.Setup(r => r.GetRoleKeysByUserIdAsync(It.IsAny<int>()))
            .ReturnsAsync(roleKeys ?? new List<string>());
        roleRepoMock.Setup(r => r.GetRolesWithAdGroupAsync())
            .ReturnsAsync(new List<Role>());

        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:AdGroupCacheMinutes", "5" }
            })
            .Build();

        var service = new CurrentUserService(httpContextAccessor.Object, roleRepoMock.Object, memoryCache, configuration);
        return (service, roleRepoMock);
    }

    [Fact]
    public async Task IsAdminAsync_NotLoggedIn_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: null);

        var result = await service.IsAdminAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_LoggedIn_UserHasAdminRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 42,
            roleKeys: new List<string> { RoleKeys.Admin });

        var result = await service.IsAdminAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAdminAsync_LoggedIn_UserHasNoAdminRole_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 5,
            roleKeys: new List<string> { RoleKeys.Picking });

        var result = await service.IsAdminAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_LoggedIn_UserHasNoRoles_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 99);

        var result = await service.IsAdminAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_LoggedIn_UserHasMasterDataButNotAdmin_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 10,
            roleKeys: new List<string> { RoleKeys.MasterData });

        var result = await service.IsAdminAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasMasterDataAccessAsync_UserHasMasterDataRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 10,
            roleKeys: new List<string> { RoleKeys.MasterData });

        var result = await service.HasMasterDataAccessAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasMasterDataAccessAsync_UserHasAdminRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 10,
            roleKeys: new List<string> { RoleKeys.Admin });

        var result = await service.HasMasterDataAccessAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanPickAsync_UserHasPickingRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 10,
            roleKeys: new List<string> { RoleKeys.Picking });

        var result = await service.CanPickAsync();

        result.Should().BeTrue();
    }
}
