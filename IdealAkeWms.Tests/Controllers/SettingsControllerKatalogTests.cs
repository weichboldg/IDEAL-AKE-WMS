using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung, E2 (AK 4-8): /Settings aus dem Katalog, Speichern ohne Nebenwirkung.</summary>
public class SettingsControllerKatalogTests
{
    private static (SettingsController ctrl, Mock<IAppSettingRepository> repo) Build(params AppSetting[] rows)
    {
        var repo = new Mock<IAppSettingRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(rows.ToList());
        var cats = new Mock<IArticleCategoryRepository>();
        cats.Setup(c => c.GetAllOrderedAsync()).ReturnsAsync(new List<ArticleCategory> { new() { Id = 1, Name = "Lackierteil" } });
        var recipients = new Mock<IOrderRecipientRepository>();
        recipients.Setup(r => r.GetAllGroupsAsync()).ReturnsAsync(new List<OrderRecipientGroup> { new() { Id = 7, Name = "Einkauf" } });
        var locations = new Mock<IStorageLocationRepository>();
        locations.Setup(l => l.GetAllOrderedAsync()).ReturnsAsync(new List<StorageLocation> { new() { Code = "NAN" }, new() { Code = "HR 0 0 0" } });
        var ops = new Mock<IOperationConfigRepository>();
        ops.Setup(o => o.GetAllAsync()).ReturnsAsync(new List<OperationConfig> { new() { OperationName = "PRODUKTION" } });
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

    private static async Task<List<SettingItem>> Items(SettingsController ctrl) =>
        ((SettingsViewModel)((ViewResult)await ctrl.Index()).Model!).Groups.SelectMany(g => g.Items).ToList();

    [Fact]
    public async Task Index_JederKatalogKey_AuchOhneZeile_MitDefault_OrphanReadOnly_AK4()
    {
        var (ctrl, _) = Build(new AppSetting { Key = "AlterKey", Value = "x" });

        var vm = (SettingsViewModel)((ViewResult)await ctrl.Index()).Model!;
        var items = vm.Groups.SelectMany(g => g.Items).ToList();

        items.Select(i => i.Key).Should().Equal(AppSettingDefinitions.All.Select(d => d.Key), "Katalog-Reihenfolge");
        var hw = items.Single(i => i.Key == AppSettingKeys.HardwareScannerAktiv);
        (hw.HatZeile, hw.Wirksam, hw.WeichtAb, hw.Anzeigename).Should().Be((false, "false", false, "Hardware-Barcodescanner"));
        vm.Orphans.Should().ContainSingle(o => o.Key == "AlterKey");
    }

    [Fact]
    public async Task Index_BoolNachLesart_UngueltigerRohwert_WirksamUndBadge_AK5()
    {
        var (ctrl, _) = Build(new AppSetting { Key = AppSettingKeys.LagerbestellungAktiv, Value = "1" },
            new AppSetting { Key = AppSettingKeys.BdeAktiv, Value = "TRUE" });

        var items = await Items(ctrl);

        var lb = items.Single(i => i.Key == AppSettingKeys.LagerbestellungAktiv);
        (lb.Wirksam, lb.Ungueltig).Should().Be(("true", true), "NurFalseSperrt: \"1\" ist an, aber kein gueltiger Wert");
        var bde = items.Single(i => i.Key == AppSettingKeys.BdeAktiv);
        (bde.Wirksam, bde.Ungueltig, bde.WeichtAb).Should().Be(("true", false, true));
    }

    [Fact]
    public async Task Index_Stammdaten_AlsAuswahl_AK8()
    {
        var (ctrl, _) = Build(new AppSetting { Key = AppSettingKeys.DefaultLagerbestellempfaengerId, Value = "99" });

        var items = await Items(ctrl);

        items.Single(i => i.Key == AppSettingKeys.LackierteilKategorieName).Optionen.Should().Contain(("Lackierteil", "Lackierteil"));
        var empf = items.Single(i => i.Key == AppSettingKeys.DefaultLagerbestellempfaengerId);
        empf.Optionen.Should().Contain(("7", "Einkauf (Id 7)"));
        empf.Ungueltig.Should().BeTrue("Gruppe 99 existiert nicht");
        items.Single(i => i.Key == AppSettingKeys.NegativeBuchungLagerplatz).Optionen.Select(o => o.Wert).Should().Contain("NAN");
        items.Single(i => i.Key == AppSettingKeys.BdeDefaultArbeitsgang).Optionen.Should().Contain(("PRODUKTION", "PRODUKTION"));
    }

    [Fact]
    public async Task Save_OhneAenderung_SchreibtNichts_KeineNeueZeile_AK6()
    {
        var (ctrl, repo) = Build(new AppSetting { Key = AppSettingKeys.BdeAktiv, Value = "true" });

        await ctrl.SaveSettings(new Dictionary<string, string>
        {
            [AppSettingKeys.BdeAktiv] = "true",                         // Zeile, unveraendert
            [AppSettingKeys.BeschichtungAbholtage] = "Dienstag,Donnerstag", // keine Zeile, = Default
            [AppSettingKeys.LagerbestellungAktiv] = "true",             // keine Zeile, wirksam = Default (NurFalseSperrt)
            [AppSettingKeys.HardwareScannerMaxZeichenabstandMs] = "",   // keine Zeile, leer = Default
        });

        repo.Verify(r => r.SetValueAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Save_Geaendert_Schreibt_UngueltigerBoolWirdKanonisch_AK5_AK6()
    {
        var (ctrl, repo) = Build(new AppSetting { Key = AppSettingKeys.LagerbestellungAktiv, Value = "1" });

        await ctrl.SaveSettings(new Dictionary<string, string>
        {
            [AppSettingKeys.LagerbestellungAktiv] = "true",
            [AppSettingKeys.BeschichtungAbholtage] = "Montag",
        });

        repo.Verify(r => r.SetValueAsync(AppSettingKeys.LagerbestellungAktiv, "true"), Times.Once);
        repo.Verify(r => r.SetValueAsync(AppSettingKeys.BeschichtungAbholtage, "Montag"), Times.Once);
    }

    [Fact]
    public async Task Save_UngueltigerWert_NichtsGeschrieben_MeldungMitAnzeigename_AK12()
    {
        var (ctrl, repo) = Build(new AppSetting { Key = AppSettingKeys.BdeAktiv, Value = "false" });

        await ctrl.SaveSettings(new Dictionary<string, string>
        {
            [AppSettingKeys.BdeAktiv] = "true",                                   // gueltig, geaendert
            [AppSettingKeys.OseonReportingHorizonDays] = "99",                    // Max 60
            [AppSettingKeys.DefaultGlasbestellempfaengerId] = "123",              // Gruppe existiert nicht
        });

        repo.Verify(r => r.SetValueAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never, "ganz oder gar nicht");
        var meldung = (string)ctrl.ViewData["WarningMessage"]!;
        meldung.Should().StartWith("Nicht gespeichert:").And.Contain("OSEON-Reporting: Horizont").And.Contain("von 1 bis 60")
            .And.Contain("Empfängergruppe Glasbestellung");
    }

    [Fact]
    public async Task Save_NurFaMeldungOhneDefaultArbeitsgang_Abgelehnt_AK13()
    {
        var (ctrl, repo) = Build(new AppSetting { Key = AppSettingKeys.BdeDefaultArbeitsgang, Value = "" });

        await ctrl.SaveSettings(new Dictionary<string, string> { [AppSettingKeys.BdeNurFaMeldung] = "true" });

        repo.Verify(r => r.SetValueAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        ((string)ctrl.ViewData["WarningMessage"]!).Should().Contain("Default-Arbeitsgang");
    }

    [Fact]
    public async Task Save_UnbekannterKey_Ignoriert_AK14()
    {
        var (ctrl, repo) = Build();

        await ctrl.SaveSettings(new Dictionary<string, string> { ["Erfunden"] = "x" });

        repo.Verify(r => r.SetValueAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        ctrl.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Save_Abgelehnt_SeiteMitEingaben_QA_B1()
    {
        var (ctrl, _) = Build(new AppSetting { Key = AppSettingKeys.BdeDefaultArbeitsgang, Value = "" });

        var result = await ctrl.SaveSettings(new Dictionary<string, string> { [AppSettingKeys.BdeNurFaMeldung] = "true" });

        var vm = (SettingsViewModel)result.Should().BeOfType<ViewResult>().Which.Model!;
        vm.Groups.SelectMany(g => g.Items).Single(i => i.Key == AppSettingKeys.BdeNurFaMeldung).Wirksam.Should().Be("true", "Eingabe bleibt");
    }

    [Fact]
    public async Task Index_BoolOhneTrim_WieDerLeser_QA_B3()
    {
        var (ctrl, _) = Build(new AppSetting { Key = AppSettingKeys.LagerbestellungAktiv, Value = " false" });

        var lb = (await Items(ctrl)).Single(i => i.Key == AppSettingKeys.LagerbestellungAktiv);

        (lb.Wirksam, lb.Ungueltig).Should().Be(("true", true), "der Leser vergleicht ungetrimmt: \" false\" sperrt NICHT");
    }
}
