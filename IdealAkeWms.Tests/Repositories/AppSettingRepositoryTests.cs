using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

public class AppSettingRepositoryTests
{
    [Fact]
    public async Task SetValueAsync_NullValue_StoresEmptyString_OnExistingKey()
    {
        // Form-Bind eines leeren Text-Inputs liefert in Dictionary<string, string> ein null —
        // das Repository muss das tolerieren, weil Value in der DB NOT NULL ist.
        using var ctx = TestDbContextFactory.Create();
        ctx.AppSettings.Add(new AppSetting { Key = "LackierteilKategorieName", Value = "alt" });
        await ctx.SaveChangesAsync();

        var repo = new AppSettingRepository(ctx);
        await repo.SetValueAsync("LackierteilKategorieName", null!);

        var stored = await repo.GetValueAsync("LackierteilKategorieName");
        stored.Should().Be(string.Empty);
    }

    [Fact]
    public async Task SetValueAsync_NullValue_StoresEmptyString_OnNewKey()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new AppSettingRepository(ctx);

        await repo.SetValueAsync("NeuerKey", null!);

        var stored = await repo.GetValueAsync("NeuerKey");
        stored.Should().Be(string.Empty);
    }
}
