using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
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

    private static (CurrentUserService service, Mock<IUserRepository> repoMock) CreateService(int? sessionUserId)
    {
        var session = new FakeSession();
        if (sessionUserId.HasValue)
        {
            // Bytes direkt im Format das GetInt32 erwartet speichern (identisch mit SetInt32)
            var v = sessionUserId.Value;
            session.Set("AppUserId", new byte[] { (byte)v, (byte)(v >> 8), (byte)(v >> 16), (byte)(v >> 24) });
        }

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(session);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var repoMock = new Mock<IUserRepository>();
        var settingRepoMock = new Mock<IAppSettingRepository>();
        settingRepoMock.Setup(r => r.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var service = new CurrentUserService(httpContextAccessor.Object, repoMock.Object, settingRepoMock.Object);
        return (service, repoMock);
    }

    [Fact]
    public async Task IsAdminAsync_NotLoggedIn_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: null);

        var result = await service.IsAdminAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_LoggedIn_UserIsAdmin_ReturnsTrue()
    {
        var (service, repoMock) = CreateService(sessionUserId: 42);
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 42, Name = "admin", IsAdmin = true, IsActive = true });

        var result = await service.IsAdminAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAdminAsync_LoggedIn_UserIsNotAdmin_ReturnsFalse()
    {
        var (service, repoMock) = CreateService(sessionUserId: 5);
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 5, Name = "normaluser", IsAdmin = false, IsActive = true });

        var result = await service.IsAdminAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_LoggedIn_UserNotFoundInRepo_ReturnsFalse()
    {
        var (service, repoMock) = CreateService(sessionUserId: 99);
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await service.IsAdminAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_LoggedIn_UserIsNotAdmin_FlagFalse_ReturnsFalse()
    {
        var (service, repoMock) = CreateService(sessionUserId: 7);
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 7, Name = "regular", IsAdmin = false, IsActive = true });

        var result = await service.IsAdminAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_LoggedIn_HasMasterDataButNotAdmin_ReturnsFalse()
    {
        // HasMasterDataAccess ist kein Ersatz für IsAdmin
        var (service, repoMock) = CreateService(sessionUserId: 10);
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 10, Name = "stammdaten", HasMasterDataAccess = true, IsAdmin = false });

        var result = await service.IsAdminAsync();

        result.Should().BeFalse();
    }
}
