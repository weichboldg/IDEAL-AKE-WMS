using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class ServiceSettingsControllerTests
{
    private static ServiceSettingsController Build(Mock<IServiceSettingRepository> repo)
    {
        var ctrl = new ServiceSettingsController(repo.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    [Fact]
    public async Task Index_MergesCatalogAndDb_TypedAndGrouped()
    {
        var repo = new Mock<IServiceSettingRepository>();
        // DB: ein bekannter Key mit User-Override + ein Orphan (nicht im Katalog).
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ServiceSetting>
        {
            new() { Key = "Sync:BomCacheEnabled", Value = "true",  Category = "BOM-Cache", Description = "alt" },
            new() { Key = "Legacy:Frei",          Value = "xyz",   Category = "Sonstiges", Description = "orphan" }
        });

        var ctrl = Build(repo);
        var result = await ctrl.Index() as ViewResult;
        var vm = result!.Model as ServiceSettingsViewModel;

        vm.Should().NotBeNull();
        // Bekannter Katalog-Key: Wert = DB-Override "true", Typ = Bool aus Katalog.
        var allItems = vm!.Groups.SelectMany(g => g.Items).ToList();
        var bom = allItems.Single(i => i.Key == "Sync:BomCacheEnabled");
        bom.Type.Should().Be(ServiceSettingType.Bool);
        bom.Value.Should().Be("true");
        // Katalog-Key OHNE DB-Zeile faellt auf Default zurueck (z.B. Sync:BomCacheWeeks = "8").
        var weeks = allItems.Single(i => i.Key == "Sync:BomCacheWeeks");
        weeks.Type.Should().Be(ServiceSettingType.Int);
        weeks.Value.Should().Be("8");
        // Ein String-Multiline-Key existiert.
        allItems.Single(i => i.Key == "ErrorNotification:Recipients").Multiline.Should().BeTrue();
        // Orphan separat, NICHT in den Gruppen.
        vm.OrphanEntries.Should().ContainSingle(o => o.Key == "Legacy:Frei");
        allItems.Should().NotContain(i => i.Key == "Legacy:Frei");
    }

    [Fact]
    public async Task Index_ShowsAllCatalogKeys_EvenWithEmptyDb()
    {
        var repo = new Mock<IServiceSettingRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ServiceSetting>());

        var ctrl = Build(repo);
        var result = await ctrl.Index() as ViewResult;
        var vm = result!.Model as ServiceSettingsViewModel;

        var keys = vm!.Groups.SelectMany(g => g.Items).Select(i => i.Key).ToList();
        keys.Should().HaveCount(ServiceSettingDefinitions.All.Count);
        keys.Should().Contain("WorkerSettings:SyncIntervalMinutes");
        vm.OrphanEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveSettings_BoolNormalized_And_Upserted()
    {
        var repo = new Mock<IServiceSettingRepository>();
        var ctrl = Build(repo);

        var settings = new Dictionary<string, string>
        {
            ["Sync:BomCacheEnabled"] = "true",
            ["Sync:CoatingDetectionEnabled"] = "false"
        };

        var result = await ctrl.SaveSettings(settings);

        result.Should().BeOfType<RedirectToActionResult>();
        // Bool-Keys werden mit Kategorie/Beschreibung aus dem Katalog geschrieben.
        repo.Verify(r => r.UpsertAsync("Sync:BomCacheEnabled", "true", "BOM-Cache", It.IsAny<string>()), Times.Once);
        repo.Verify(r => r.UpsertAsync("Sync:CoatingDetectionEnabled", "false", "Lackierteile", It.IsAny<string>()), Times.Once);
        ctrl.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task SaveSettings_BoolCheckboxValueOn_NormalizedToTrue()
    {
        var repo = new Mock<IServiceSettingRepository>();
        var ctrl = Build(repo);

        // Falls je ein Rohwert != "true"/"false" ankommt (z.B. "on"): normalisieren.
        var settings = new Dictionary<string, string> { ["Sync:BomCacheEnabled"] = "on" };
        await ctrl.SaveSettings(settings);

        repo.Verify(r => r.UpsertAsync("Sync:BomCacheEnabled", "true", "BOM-Cache", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SaveSettings_IntParseError_AddsModelStateError_AndSkipsUpsertForThatKey()
    {
        var repo = new Mock<IServiceSettingRepository>();
        var ctrl = Build(repo);

        var settings = new Dictionary<string, string>
        {
            ["Sync:BomCacheWeeks"] = "abc",   // ungueltig
            ["Sync:BomCacheMaxOrders"] = "300" // gueltig
        };

        var result = await ctrl.SaveSettings(settings);

        // Ungueltiger Int-Key wird NICHT gespeichert, aber gueltiger schon.
        repo.Verify(r => r.UpsertAsync("Sync:BomCacheWeeks", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        repo.Verify(r => r.UpsertAsync("Sync:BomCacheMaxOrders", "300", "BOM-Cache", It.IsAny<string>()), Times.Once);
        ctrl.ModelState.IsValid.Should().BeFalse();
        // Bei Fehler: View zurueck (nicht Redirect), damit der Fehler sichtbar ist.
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task SaveSettings_UnknownKey_IgnoredSilently()
    {
        var repo = new Mock<IServiceSettingRepository>();
        var ctrl = Build(repo);

        var settings = new Dictionary<string, string> { ["Not:InCatalog"] = "whatever" };
        await ctrl.SaveSettings(settings);

        repo.Verify(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
