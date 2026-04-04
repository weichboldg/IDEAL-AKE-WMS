using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Tests.Repositories;

public class CachedSettingRepositoryTests
{
    [Fact]
    public async Task GetValueAsync_ReturnsCachedValue_OnSecondCall()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.AppSettings.Add(new AppSetting { Key = "TestKey", Value = "TestValue" });
        ctx.SaveChanges();

        var inner = new AppSettingRepository(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedSettingRepository(inner, cache);

        var value1 = await cached.GetValueAsync("TestKey");
        value1.Should().Be("TestValue");

        // Change value directly in DB (bypassing cache)
        var setting = ctx.AppSettings.First(s => s.Key == "TestKey");
        setting.Value = "Changed";
        ctx.SaveChanges();

        // Should still return cached value
        var value2 = await cached.GetValueAsync("TestKey");
        value2.Should().Be("TestValue");
    }

    [Fact]
    public async Task SetValueAsync_InvalidatesCache()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.AppSettings.Add(new AppSetting { Key = "TestKey", Value = "OldValue" });
        ctx.SaveChanges();

        var inner = new AppSettingRepository(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedSettingRepository(inner, cache);

        await cached.GetValueAsync("TestKey"); // populate cache
        await cached.SetValueAsync("TestKey", "NewValue");

        var value = await cached.GetValueAsync("TestKey");
        value.Should().Be("NewValue");
    }

    [Fact]
    public async Task GetIntValueAsync_ReturnsDefault_WhenKeyMissing()
    {
        using var ctx = TestDbContextFactory.Create();
        var inner = new AppSettingRepository(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedSettingRepository(inner, cache);

        var value = await cached.GetIntValueAsync("NonExistent", 42);
        value.Should().Be(42);
    }

    [Fact]
    public async Task GetValueAsync_CachesNullValues()
    {
        using var ctx = TestDbContextFactory.Create();
        var inner = new AppSettingRepository(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedSettingRepository(inner, cache);

        var value1 = await cached.GetValueAsync("Missing");
        value1.Should().BeNull();

        // Add value directly to DB
        ctx.AppSettings.Add(new AppSetting { Key = "Missing", Value = "NowExists" });
        ctx.SaveChanges();

        // Should still return null (cached)
        var value2 = await cached.GetValueAsync("Missing");
        value2.Should().BeNull();
    }
}
