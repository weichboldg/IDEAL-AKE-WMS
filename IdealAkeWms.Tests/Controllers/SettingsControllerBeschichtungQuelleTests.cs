using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Spec 2026-11-16-beschichtungslogik-je-standort A8 (AK 2, 3, 19): /Settings lehnt Beschichtungsquellen ohne Datenbasis ab.</summary>
public class SettingsControllerBeschichtungQuelleTests
{
    private static (SettingsController ctrl, Mock<IAppSettingRepository> repo) Build(string? master, params AppSetting[] bestand)
    {
        var repo = new Mock<IAppSettingRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(bestand.ToList());
        var svc = new Mock<IServiceSettingRepository>();
        svc.Setup(s => s.GetValueAsync("ProduktionsauftragHierarchisch")).ReturnsAsync(master);
        var ctrl = new SettingsController(repo.Object, Mock.Of<IHolidayRepository>(), Mock.Of<ICurrentUserService>(), Mock.Of<IHolidayImportService>(),
            Mock.Of<IArticleCategoryRepository>(), Mock.Of<IOrderRecipientRepository>(), Mock.Of<IStorageLocationRepository>(),
            Mock.Of<IOperationConfigRepository>(), svc.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());
        return (ctrl, repo);
    }

    [Theory]
    [InlineData("true", AppSettingKeys.BeschichtungKennzeichenQuelle, "Artikelkategorie")]
    [InlineData("false", AppSettingKeys.BeschichtungKennzeichenQuelle, "FaStruktur")]
    [InlineData(null, AppSettingKeys.BeschichtungKennzeichenQuelle, "FaStruktur")]   // Master-Zeile fehlt = flach (Default false)
    [InlineData("false", AppSettingKeys.BeschichtungTerminQuelle, "View")]           // AKE: View nicht waehlbar (Freigabe-Antwort 15)
    public async Task Save_UnpassendeKombination_NichtsGespeichert(string? master, string key, string wert)
    {
        var (ctrl, repo) = Build(master);

        var result = await ctrl.SaveSettings(new Dictionary<string, string> { [key] = wert });

        result.Should().BeOfType<ViewResult>();
        ((string)ctrl.ViewData["WarningMessage"]!).Should().Contain("passt nicht zum Auftragsmodell");
        repo.Verify(r => r.SetValueAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("true", AppSettingKeys.BeschichtungKennzeichenQuelle, "FaStruktur")]
    [InlineData("true", AppSettingKeys.BeschichtungTerminQuelle, "View")]
    [InlineData("true", AppSettingKeys.BeschichtungTerminQuelle, "Berechnet")]
    [InlineData("false", AppSettingKeys.BeschichtungKennzeichenQuelle, "Artikelkategorie")]
    [InlineData("false", AppSettingKeys.BeschichtungTerminQuelle, "berechnet")]
    public async Task Save_PassendeKombination_Gespeichert(string master, string key, string wert)
    {
        var (ctrl, repo) = Build(master);

        var result = await ctrl.SaveSettings(new Dictionary<string, string> { [key] = wert });

        result.Should().BeOfType<RedirectToActionResult>();
        var kanonisch = char.ToUpperInvariant(wert[0]) + wert[1..];
        repo.Verify(r => r.SetValueAsync(key, kanonisch), Times.Once);
    }

    [Fact]
    public async Task Save_WertAusserhalbDerListe_Abgelehnt_AK2()
    {
        var (ctrl, repo) = Build("true");

        var result = await ctrl.SaveSettings(new Dictionary<string, string> { [AppSettingKeys.BeschichtungTerminQuelle] = "SData" });

        result.Should().BeOfType<ViewResult>();
        repo.Verify(r => r.SetValueAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Save_AndererKey_BestandUnpassend_Gemeldet()
    {
        // Bestandswert passt nach einem Master-Wechsel nicht mehr: jedes Speichern meldet ihn (wie die BDE-Pflichtregel), bis er korrigiert ist.
        var (ctrl, repo) = Build("true", new AppSetting { Key = AppSettingKeys.BeschichtungKennzeichenQuelle, Value = "Artikelkategorie" });

        var result = await ctrl.SaveSettings(new Dictionary<string, string> { [AppSettingKeys.QrMitFaNummer] = "true" });

        result.Should().BeOfType<ViewResult>();
        repo.Verify(r => r.SetValueAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
