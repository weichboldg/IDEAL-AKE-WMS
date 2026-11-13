using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Spec 2026-11-08-bereinigungen-nach-buendel-2026-11-07 (v1.79.0): F1, F2 (+ QA-W2 des Einstellungen-Epics), F5, F6.</summary>
public class BereinigungenV179Tests
{
    private static SettingDefinition App(string key) => AppSettingDefinitions.All.Single(d => d.Key == key);

    // ---- F2: fehlende Zeile, Standard-Lagerplatz nicht vorhanden ----

    [Fact]
    public void F2_MasterDataOhneZeile_DefaultNichtInStammdaten_Ungueltig_AK3()
    {
        var d = App(AppSettingKeys.NegativeBuchungLagerplatz);
        var ohneNan = SettingItem.Aus(d, null, new List<(string, string)> { ("HR 0 0 0", "HR 0 0 0") });
        (ohneNan.HatZeile, ohneNan.Ungueltig, ohneNan.Wirksam).Should().Be((false, true, "NAN"));
        ohneNan.Fehler.Should().Contain("NAN");

        SettingItem.Aus(d, null, new List<(string, string)> { ("NAN", "NAN") }).Ungueltig.Should().BeFalse();
        // Choice und Bool ohne Zeile bleiben ungeprueft.
        SettingItem.Aus(App(AppSettingKeys.QrErstesFeld), null).Ungueltig.Should().BeFalse();
        SettingItem.Aus(App(AppSettingKeys.BdeAktiv), null).Ungueltig.Should().BeFalse();
    }

    private static (SettingsController ctrl, Mock<IAppSettingRepository> repo) BuildSettings(bool mitNan, params AppSetting[] rows)
    {
        var repo = new Mock<IAppSettingRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(rows.ToList());
        var cats = new Mock<IArticleCategoryRepository>();
        cats.Setup(c => c.GetAllOrderedAsync()).ReturnsAsync(new List<ArticleCategory>());
        var recipients = new Mock<IOrderRecipientRepository>();
        recipients.Setup(r => r.GetAllGroupsAsync()).ReturnsAsync(new List<OrderRecipientGroup>());
        var locations = new Mock<IStorageLocationRepository>();
        var plaetze = new List<StorageLocation> { new() { Code = "HR 0 0 0" } };
        if (mitNan) plaetze.Add(new StorageLocation { Code = "NAN" });
        locations.Setup(l => l.GetAllOrderedAsync()).ReturnsAsync(plaetze);
        var ops = new Mock<IOperationConfigRepository>();
        ops.Setup(o => o.GetAllAsync()).ReturnsAsync(new List<OperationConfig>());
        var holidays = new Mock<IHolidayRepository>();
        holidays.Setup(h => h.GetAllOrderedAsync()).ReturnsAsync(new List<Holiday>());
        var ctrl = new SettingsController(repo.Object, holidays.Object, Mock.Of<ICurrentUserService>(), Mock.Of<IHolidayImportService>(),
            cats.Object, recipients.Object, locations.Object, ops.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());
        return (ctrl, repo);
    }

    [Theory]
    [InlineData(true)]   // Zeile vorhanden mit nicht vorhandenem Platz
    [InlineData(false)]  // keine Zeile, Default NAN fehlt
    public async Task F2_W2_Settings_UngueltigerAusweichplatz_BlockiertAndereAenderungNicht_AK4(bool mitZeile)
    {
        var rows = mitZeile ? new[] { new AppSetting { Key = AppSettingKeys.NegativeBuchungLagerplatz, Value = "WEG" } } : Array.Empty<AppSetting>();
        var (ctrl, repo) = BuildSettings(mitNan: false, rows);

        var index = (SettingsViewModel)((ViewResult)await ctrl.Index()).Model!;
        index.Groups.SelectMany(g => g.Items).Single(i => i.Key == AppSettingKeys.NegativeBuchungLagerplatz).Ungueltig.Should().BeTrue();

        var result = await ctrl.SaveSettings(new Dictionary<string, string>
        {
            [AppSettingKeys.NegativeBuchungLagerplatz] = mitZeile ? "WEG" : "NAN",  // unveraendert gepostet
            [AppSettingKeys.BdeAktiv] = "true",
        });

        result.Should().BeOfType<RedirectToActionResult>();
        repo.Verify(r => r.SetValueAsync(AppSettingKeys.BdeAktiv, "true"), Times.Once);
        repo.Verify(r => r.SetValueAsync(AppSettingKeys.NegativeBuchungLagerplatz, It.IsAny<string?>()), Times.Never);
    }

    private static ServiceSettingsController BuildSvc(Mock<IServiceSettingRepository> repo)
    {
        var ctrl = new ServiceSettingsController(repo.Object, Mock.Of<ISyncLogger>(), Mock.Of<ICurrentUserService>(),
            new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider(),
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ServiceSettingsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    [Fact]
    public async Task W2_ServiceSettings_Ablehnung_GibtGeposteteWerteZurueck_B1()
    {
        var repo = new Mock<IServiceSettingRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ServiceSetting>());
        var ctrl = BuildSvc(repo);

        var result = await ctrl.SaveSettings(new Dictionary<string, string>
        {
            ["Sync:BomCacheWeeks"] = "abc",
            ["Sync:BomCacheMaxOrders"] = "300",
        });

        var vm = (ServiceSettingsViewModel)((ViewResult)result).Model!;
        var items = vm.Groups.SelectMany(g => g.Items).ToList();
        items.Single(i => i.Key == "Sync:BomCacheWeeks").Wirksam.Should().Be("abc");
        items.Single(i => i.Key == "Sync:BomCacheMaxOrders").Wirksam.Should().Be("300");
        repo.Verify(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task W2_ServiceSettings_UnveraendertUngueltig_BlockiertNicht_B2()
    {
        var repo = new Mock<IServiceSettingRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ServiceSetting>
        {
            new() { Key = "Sync:BomCacheWeeks", Value = "abc", Category = "BOM-Cache", Description = "" },
        });
        var ctrl = BuildSvc(repo);

        var result = await ctrl.SaveSettings(new Dictionary<string, string>
        {
            ["Sync:BomCacheWeeks"] = "abc",
            ["Sync:BomCacheMaxOrders"] = "300",
        });

        result.Should().BeOfType<RedirectToActionResult>();
        repo.Verify(r => r.UpsertAsync("Sync:BomCacheMaxOrders", "300", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        repo.Verify(r => r.UpsertAsync("Sync:BomCacheWeeks", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ---- F1: Hinweise innerhalb der .setting-row ----

    private static readonly string Root = FindRoot();

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views", "Shared", "_SettingField.cshtml")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }

    private static string Read(params string[] teile) => File.ReadAllText(Path.Combine(new[] { Root, "IdealAkeWms" }.Concat(teile).ToArray()));

    [Fact]
    public void F1_SperrUndTlsHinweis_ImFeldBaustein_NichtNachDemPartial_AK1()
    {
        var svc = Read("Views", "ServiceSettings", "Index.cshtml");
        svc.Should().Contain("[\"SperrGrund\"] = sperrGrund").And.Contain("[\"TlsWarnung\"] = tlsWarnung")
            .And.NotContain("mt-n2").And.NotContain("SData ist hier gesperrt bis");
        var feld = Read("Views", "Shared", "_SettingField.cshtml");
        var ende = feld.LastIndexOf("</div>", StringComparison.Ordinal);
        feld.IndexOf("ViewData[\"SperrGrund\"]", StringComparison.Ordinal).Should().BeInRange(0, ende, "Hinweis vor dem Schluss der .setting-row");
        feld.IndexOf("ViewData[\"TlsWarnung\"]", StringComparison.Ordinal).Should().BeInRange(0, ende);
    }

    // ---- F5 / F6 ----

    [Fact]
    public void F5_RueckmeldeSchalter_NichtMehrImKatalog_AK7()
    {
        AppSettingDefinitions.All.Should().NotContain(d => d.Key == "OseonRueckmeldungAktiv" || d.Key == "SageRueckmeldungAktiv");
    }

    [Fact]
    public void F6_NichtNoetig_LesbarerText_GleichImFilter_AK8()
    {
        SageUebergabeStatusText.Text(SageUebergabeStatus.NichtNoetig).Should().Be("nicht nötig");
        SageUebergabeStatusText.IstOffen(SageUebergabeStatus.NichtNoetig).Should().BeFalse("Hinweis-Zähler unverändert");
        Read("Views", "Shared", "_SageUebergabeBadge.cshtml").Should().Contain("text-body-secondary");
    }
}
