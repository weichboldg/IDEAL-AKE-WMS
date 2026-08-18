using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services.Standort;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// IDEAL Teil 6: atomarer Zwei-Backend-Schreiber. InMemory kennt keine Transaktionen (IsRelational
/// false) → der Writer öffnet keine, die Atomarität am einen SaveChanges bleibt geprüft.
/// </summary>
public class StandortSettingsWriterTests
{
    private static readonly IReadOnlyList<(string, string, string?, string?)> NoService =
        new List<(string, string, string?, string?)>();

    private static readonly IReadOnlyDictionary<string, string?> NoApp =
        new Dictionary<string, string?>();

    [Fact]
    public async Task SaveAtomic_UpdatesExisting_AndInsertsNew_AcrossBothBackends()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.AppSettings.Add(new AppSetting { Key = AppSettingKeys.Firmenname, Value = "Alt AG" });
        ctx.ServiceSettings.Add(new ServiceSetting { Key = "Sync:HierarchicalFaEnabled", Value = "false", Category = "FA-Hierarchie" });
        await ctx.SaveChangesAsync();
        var writer = new StandortSettingsWriter(ctx, new MemoryCache(new MemoryCacheOptions()));

        await writer.SaveAtomicAsync(
            new Dictionary<string, string?>
            {
                [AppSettingKeys.Firmenname] = "Neu AG",           // update
                [AppSettingKeys.Firmenanschrift] = "Musterweg 1", // insert
            },
            new List<(string, string, string?, string?)>
            {
                ("Sync:HierarchicalFaEnabled", "true", "FA-Hierarchie", null), // update
                ("SData:Dataset", "ake_TEST;1", "Sage-Lagerbuchung", null),    // insert
            });

        (await ctx.AppSettings.FindAsync(AppSettingKeys.Firmenname))!.Value.Should().Be("Neu AG");
        (await ctx.AppSettings.FindAsync(AppSettingKeys.Firmenanschrift))!.Value.Should().Be("Musterweg 1");
        (await ctx.ServiceSettings.FindAsync("Sync:HierarchicalFaEnabled"))!.Value.Should().Be("true");
        var dataset = await ctx.ServiceSettings.FindAsync("SData:Dataset");
        dataset!.Value.Should().Be("ake_TEST;1");
        dataset.Category.Should().Be("Sage-Lagerbuchung");
    }

    [Fact]
    public async Task SaveAtomic_NullAppValue_StoredAsEmptyString()
    {
        var ctx = TestDbContextFactory.Create();
        var writer = new StandortSettingsWriter(ctx, new MemoryCache(new MemoryCacheOptions()));

        await writer.SaveAtomicAsync(
            new Dictionary<string, string?> { [AppSettingKeys.Firmenname] = null }, NoService);

        (await ctx.AppSettings.FindAsync(AppSettingKeys.Firmenname))!.Value.Should().Be(string.Empty);
    }

    [Fact]
    public async Task SaveAtomic_OnlyServiceValues_LeavesAppUntouched()
    {
        var ctx = TestDbContextFactory.Create();
        var writer = new StandortSettingsWriter(ctx, new MemoryCache(new MemoryCacheOptions()));

        await writer.SaveAtomicAsync(NoApp,
            new List<(string, string, string?, string?)> { ("Sync:FaHierarchyListeViewName", "[v]", "FA-Hierarchie", null) });

        (await ctx.ServiceSettings.FindAsync("Sync:FaHierarchyListeViewName"))!.Value.Should().Be("[v]");
        (await ctx.AppSettings.CountAsync()).Should().Be(0);
    }
}
