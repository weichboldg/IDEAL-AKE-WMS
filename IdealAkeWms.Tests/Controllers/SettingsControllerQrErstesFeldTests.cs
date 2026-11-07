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

/// <summary>v1.68.0 QrErstesFeld (Spec 2026-10-23-qr-code-matchcode-statt-artikelnummer AK 2): Freitext der /Settings-Maske wird
/// kanonisch gespeichert, ein ungueltiger Wert speichert nichts und meldet sich.</summary>
public class SettingsControllerQrErstesFeldTests
{
    private static (SettingsController ctrl, Mock<IAppSettingRepository> repo) Build()
    {
        var repo = new Mock<IAppSettingRepository>();
        // v1.77.0 F4: nur geaenderte Werte werden geschrieben — die Zeilen existieren hier mit anderem Wert.
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AppSetting>
        {
            new() { Key = AppSettingKeys.QrErstesFeld, Value = "alt" }, new() { Key = AppSettingKeys.QrMitFaNummer, Value = "false" },
            new() { Key = AppSettingKeys.QrTrennzeichen, Value = "alt" },
        });
        var ctrl = new SettingsController(repo.Object, Mock.Of<IHolidayRepository>(), Mock.Of<ICurrentUserService>(), Mock.Of<IHolidayImportService>(),
            Mock.Of<IArticleCategoryRepository>(), Mock.Of<IOrderRecipientRepository>(), Mock.Of<IStorageLocationRepository>(), Mock.Of<IOperationConfigRepository>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());
        return (ctrl, repo);
    }

    [Theory]
    [InlineData("matchcode", "Matchcode")]
    [InlineData(" ARTIKELNUMMER ", "Artikelnummer")]
    public async Task Save_GueltigerWert_KanonischGespeichert(string eingabe, string gespeichert)
    {
        var (ctrl, repo) = Build();

        await ctrl.SaveSettings(new Dictionary<string, string> { [AppSettingKeys.QrErstesFeld] = eingabe, [AppSettingKeys.QrMitFaNummer] = "true" });

        repo.Verify(r => r.SetValueAsync(AppSettingKeys.QrErstesFeld, gespeichert), Times.Once);
        repo.Verify(r => r.SetValueAsync(AppSettingKeys.QrMitFaNummer, "true"), Times.Once);
        ctrl.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Theory]
    [InlineData("Matchcod")]
    [InlineData("")]
    public async Task Save_UngueltigerWert_NichtsGespeichert_Meldung(string eingabe)
    {
        var (ctrl, repo) = Build();

        var result = await ctrl.SaveSettings(new Dictionary<string, string> { [AppSettingKeys.QrMitFaNummer] = "true", [AppSettingKeys.QrErstesFeld] = eingabe });

        result.Should().BeOfType<RedirectToActionResult>();
        repo.Verify(r => r.SetValueAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never, "kein Wert der Maske wird gespeichert");
        ctrl.TempData["WarningMessage"].Should().Be("QrErstesFeld: erlaubt sind „Artikelnummer“ oder „Matchcode“ – nichts gespeichert.");
    }

    // v1.69.0 QrTrennzeichen (Freigabe-Nachtrag 2): gleicher Mechanismus ueber AppSettingDefinitions (Choice), genau ; oder ,
    [Theory]
    [InlineData(";", ";")]
    [InlineData(" , ", ",")]
    public async Task Save_QrTrennzeichen_Gueltig_Gespeichert(string eingabe, string gespeichert)
    {
        var (ctrl, repo) = Build();

        await ctrl.SaveSettings(new Dictionary<string, string> { [AppSettingKeys.QrTrennzeichen] = eingabe, [AppSettingKeys.QrErstesFeld] = "Matchcode" });

        repo.Verify(r => r.SetValueAsync(AppSettingKeys.QrTrennzeichen, gespeichert), Times.Once);
        repo.Verify(r => r.SetValueAsync(AppSettingKeys.QrErstesFeld, "Matchcode"), Times.Once);
        ctrl.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Theory]
    [InlineData(";,")]
    [InlineData("|")]
    [InlineData("")]
    public async Task Save_QrTrennzeichen_Ungueltig_NichtsGespeichert_Meldung(string eingabe)
    {
        var (ctrl, repo) = Build();

        await ctrl.SaveSettings(new Dictionary<string, string> { [AppSettingKeys.QrErstesFeld] = "Matchcode", [AppSettingKeys.QrTrennzeichen] = eingabe });

        repo.Verify(r => r.SetValueAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        ctrl.TempData["WarningMessage"].Should().Be("QrTrennzeichen: erlaubt sind „;“ oder „,“ – nichts gespeichert.");
    }
}
