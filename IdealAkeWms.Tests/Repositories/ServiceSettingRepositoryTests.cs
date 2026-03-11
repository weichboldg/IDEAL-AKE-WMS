using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

public class ServiceSettingRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsAllSettings_OrderedByCategoryThenKey()
    {
        await using var db = TestDbContextFactory.Create();
        db.ServiceSettings.AddRange(
            new ServiceSetting { Key = "Sync:Articles",      Value = "true",  Category = "Sync" },
            new ServiceSetting { Key = "Sync:Orders",        Value = "true",  Category = "Sync" },
            new ServiceSetting { Key = "Notifications:Mail", Value = "true",  Category = "Notifications" },
            new ServiceSetting { Key = "Other:Key",          Value = "value", Category = null }
        );
        await db.SaveChangesAsync();

        var repo = new ServiceSettingRepository(db);
        var result = await repo.GetAllAsync();

        result.Should().HaveCount(4);
        // Reihenfolge: null-Category zuerst (alphabetisch vor "N"), dann Notifications, dann Sync
        result[0].Key.Should().Be("Other:Key");
        result[1].Key.Should().Be("Notifications:Mail");
        result[2].Key.Should().Be("Sync:Articles");
        result[3].Key.Should().Be("Sync:Orders");
    }

    [Fact]
    public async Task GetByCategoryAsync_ReturnsOnlyMatchingCategory()
    {
        await using var db = TestDbContextFactory.Create();
        db.ServiceSettings.AddRange(
            new ServiceSetting { Key = "Sync:A",   Value = "1", Category = "Sync" },
            new ServiceSetting { Key = "Sync:B",   Value = "2", Category = "Sync" },
            new ServiceSetting { Key = "Mail:Host", Value = "smtp", Category = "Mail" }
        );
        await db.SaveChangesAsync();

        var repo = new ServiceSettingRepository(db);
        var result = await repo.GetByCategoryAsync("Sync");

        result.Should().HaveCount(2);
        result.Select(r => r.Key).Should().BeEquivalentTo(["Sync:A", "Sync:B"]);
    }

    [Fact]
    public async Task GetByCategoryAsync_NoMatchingCategory_ReturnsEmpty()
    {
        await using var db = TestDbContextFactory.Create();
        db.ServiceSettings.Add(new ServiceSetting { Key = "Sync:A", Value = "1", Category = "Sync" });
        await db.SaveChangesAsync();

        var repo = new ServiceSettingRepository(db);
        var result = await repo.GetByCategoryAsync("Notifications");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetValueAsync_ExistingKey_ReturnsValue()
    {
        await using var db = TestDbContextFactory.Create();
        db.ServiceSettings.Add(new ServiceSetting { Key = "Test:Key", Value = "meinWert" });
        await db.SaveChangesAsync();

        var repo = new ServiceSettingRepository(db);
        var result = await repo.GetValueAsync("Test:Key");

        result.Should().Be("meinWert");
    }

    [Fact]
    public async Task GetValueAsync_NonExistingKey_ReturnsNull()
    {
        await using var db = TestDbContextFactory.Create();

        var repo = new ServiceSettingRepository(db);
        var result = await repo.GetValueAsync("NichtVorhanden:Key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_NewKey_InsertsRecord()
    {
        await using var db = TestDbContextFactory.Create();
        var repo = new ServiceSettingRepository(db);

        await repo.UpsertAsync("Neu:Key", "neuerWert", "Neu", "Beschreibung");

        var result = await repo.GetValueAsync("Neu:Key");
        result.Should().Be("neuerWert");

        var setting = db.ServiceSettings.Single();
        setting.Category.Should().Be("Neu");
        setting.Description.Should().Be("Beschreibung");
    }

    [Fact]
    public async Task UpsertAsync_ExistingKey_UpdatesValue()
    {
        await using var db = TestDbContextFactory.Create();
        db.ServiceSettings.Add(new ServiceSetting { Key = "Update:Key", Value = "altWert", Category = "Cat" });
        await db.SaveChangesAsync();

        var repo = new ServiceSettingRepository(db);
        await repo.UpsertAsync("Update:Key", "neuerWert");

        var result = await repo.GetValueAsync("Update:Key");
        result.Should().Be("neuerWert");
    }

    [Fact]
    public async Task UpsertAsync_ExistingKey_UpdatesCategoryWhenProvided()
    {
        await using var db = TestDbContextFactory.Create();
        db.ServiceSettings.Add(new ServiceSetting { Key = "Cat:Key", Value = "val", Category = "AltKat" });
        await db.SaveChangesAsync();

        var repo = new ServiceSettingRepository(db);
        await repo.UpsertAsync("Cat:Key", "val", category: "NeueKat");

        var setting = db.ServiceSettings.Single();
        setting.Category.Should().Be("NeueKat");
    }

    [Fact]
    public async Task UpsertAsync_ExistingKey_KeepsOldCategoryWhenNull()
    {
        await using var db = TestDbContextFactory.Create();
        db.ServiceSettings.Add(new ServiceSetting { Key = "Cat:Key", Value = "val", Category = "Behalten" });
        await db.SaveChangesAsync();

        var repo = new ServiceSettingRepository(db);
        // category = null → wird nicht überschrieben
        await repo.UpsertAsync("Cat:Key", "neuerWert", category: null);

        var setting = db.ServiceSettings.Single();
        setting.Category.Should().Be("Behalten");
    }

    [Fact]
    public async Task DeleteAsync_ExistingKey_RemovesRecord()
    {
        await using var db = TestDbContextFactory.Create();
        db.ServiceSettings.Add(new ServiceSetting { Key = "Del:Key", Value = "weg" });
        await db.SaveChangesAsync();

        var repo = new ServiceSettingRepository(db);
        await repo.DeleteAsync("Del:Key");

        db.ServiceSettings.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingKey_DoesNotThrow()
    {
        await using var db = TestDbContextFactory.Create();
        var repo = new ServiceSettingRepository(db);

        var act = async () => await repo.DeleteAsync("NichtDa:Key");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpsertAsync_CalledTwiceWithSameKey_UpdatesInsteadOfDuplicate()
    {
        await using var db = TestDbContextFactory.Create();
        var repo = new ServiceSettingRepository(db);

        await repo.UpsertAsync("Double:Key", "erster");
        await repo.UpsertAsync("Double:Key", "zweiter");

        db.ServiceSettings.Should().HaveCount(1);
        var result = await repo.GetValueAsync("Double:Key");
        result.Should().Be("zweiter");
    }

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmpty()
    {
        await using var db = TestDbContextFactory.Create();
        var repo = new ServiceSettingRepository(db);

        var result = await repo.GetAllAsync();

        result.Should().BeEmpty();
    }
}
