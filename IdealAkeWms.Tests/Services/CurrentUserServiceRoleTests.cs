using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace IdealAkeWms.Tests.Services;

public class CurrentUserServiceRoleTests
{
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

        var userRepoMock = new Mock<IUserRepository>();

        var service = new CurrentUserService(httpContextAccessor.Object, roleRepoMock.Object, userRepoMock.Object);
        return (service, roleRepoMock);
    }

    [Fact]
    public async Task HasRoleAsync_WithMatchingDirectRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Picking });

        var result = await service.HasRoleAsync(RoleKeys.Picking);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasRoleAsync_WithNoRoles_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string>());

        var result = await service.HasRoleAsync(RoleKeys.Picking);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_WithAdminRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Admin });

        var result = await service.IsAdminAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanPickAsync_WithAdminRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Admin });

        var result = await service.CanPickAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanPickAsync_WithPickingRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Picking });

        var result = await service.CanPickAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessStockAsync_WithStockRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Stock });

        var result = await service.CanAccessStockAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessStockAsync_WithPickingRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Picking });

        var result = await service.CanAccessStockAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanTransferStockAsync_WithStockRole_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Stock });

        var result = await service.CanTransferStockAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanTransferStockAsync_WithStockKeyUserRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.StockKeyUser });

        var result = await service.CanTransferStockAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAnyRoleAsync_WithOneMatchingRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Stock });

        var result = await service.HasAnyRoleAsync(RoleKeys.Picking, RoleKeys.Stock);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RolesAreCachedPerRequest()
    {
        var (service, roleRepoMock) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Picking });

        await service.HasRoleAsync(RoleKeys.Picking);
        await service.HasRoleAsync(RoleKeys.Admin);

        roleRepoMock.Verify(r => r.GetRoleKeysByUserIdAsync(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task CanAccessLagerbestellungAsync_WithLagerbestellungRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Lagerbestellung });

        (await service.CanAccessLagerbestellungAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessLagerbestellungAsync_WithAdminRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Admin });

        (await service.CanAccessLagerbestellungAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessLagerbestellungAsync_WithUnrelatedRole_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Stock });

        (await service.CanAccessLagerbestellungAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task LagerbestellungRole_DoesNotGrantPickingOrStock()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Lagerbestellung });

        (await service.CanPickAsync()).Should().BeFalse();
        (await service.CanAccessStockAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessGlasbestellungAsync_WithGlasbestellungRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Glasbestellung });

        (await service.CanAccessGlasbestellungAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessGlasbestellungAsync_WithAdminRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Admin });

        (await service.CanAccessGlasbestellungAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessGlasbestellungAsync_WithLagerbestellungRole_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Lagerbestellung });

        (await service.CanAccessGlasbestellungAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessGlasbestellungAsync_WithStockRole_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Stock });

        (await service.CanAccessGlasbestellungAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessGlasbestellungAsync_WithPickingRole_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Picking });

        (await service.CanAccessGlasbestellungAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessGlasbestellungAsync_WithNoRoles_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string>());

        (await service.CanAccessGlasbestellungAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CanOrderLagerAsync_WithAdminRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Admin });

        (await service.CanOrderLagerAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanOrderLagerAsync_WithPickingRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Picking });

        (await service.CanOrderLagerAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanOrderLagerAsync_WithStockRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Stock });

        (await service.CanOrderLagerAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanOrderLagerAsync_WithStockKeyUserRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.StockKeyUser });

        (await service.CanOrderLagerAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanOrderLagerAsync_WithLagerbestellungRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Lagerbestellung });

        (await service.CanOrderLagerAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanOrderLagerAsync_WithGlasbestellungRoleOnly_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Glasbestellung });

        (await service.CanOrderLagerAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CanOrderLagerAsync_WithNoRoles_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string>());

        (await service.CanOrderLagerAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CanOrderGlasAsync_WithAdminRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Admin });

        (await service.CanOrderGlasAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanOrderGlasAsync_WithPickingRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Picking });

        (await service.CanOrderGlasAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanOrderGlasAsync_WithStockRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Stock });

        (await service.CanOrderGlasAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanOrderGlasAsync_WithStockKeyUserRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.StockKeyUser });

        (await service.CanOrderGlasAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanOrderGlasAsync_WithGlasbestellungRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Glasbestellung });

        (await service.CanOrderGlasAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanOrderGlasAsync_WithLagerbestellungRoleOnly_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Lagerbestellung });

        (await service.CanOrderGlasAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CanOrderGlasAsync_WithNoRoles_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string>());

        (await service.CanOrderGlasAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessStockReadAsync_WithAdminRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Admin });

        (await service.CanAccessStockReadAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessStockReadAsync_WithStockRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Stock });

        (await service.CanAccessStockReadAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessStockReadAsync_WithStockKeyUserRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.StockKeyUser });

        (await service.CanAccessStockReadAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessStockReadAsync_WithPickingRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Picking });

        (await service.CanAccessStockReadAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessStockReadAsync_WithStockReadRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.StockRead });

        (await service.CanAccessStockReadAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessStockReadAsync_WithTrackingRoleOnly_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Tracking });

        (await service.CanAccessStockReadAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessStockReadAsync_WithNoRoles_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string>());

        (await service.CanAccessStockReadAsync()).Should().BeFalse();
    }
}
