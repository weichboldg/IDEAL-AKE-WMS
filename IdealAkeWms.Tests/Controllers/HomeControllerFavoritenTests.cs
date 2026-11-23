using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.Navigation;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.51 (Spec dashboard-favoriten-reiter): Stern-Actions schreiben nur sichtbare Kacheln in UserViewPreferences.</summary>
public class HomeControllerFavoritenTests
{
    private const string Key = HomeController.FavoritenViewKey;

    /// <summary>Benutzer 7 mit Lager-Schreib- und -Leserecht: Einbuchung sichtbar (Kachel), SageLagerbuchungen sichtbar aber ohne Kategorie,
    /// Benutzer (admin) unsichtbar.</summary>
    private static HomeController Build(ApplicationDbContext db, int? userId = 7)
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(userId);
        user.Setup(u => u.GetDisplayName()).Returns("Tester");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\tester");
        user.Setup(u => u.CanAccessStockAsync()).ReturnsAsync(true);
        user.Setup(u => u.CanAccessStockReadAsync()).ReturnsAsync(true);
        var nav = new NavigationSicht(user.Object, Mock.Of<IAppSettingRepository>(), Mock.Of<IServiceSettingRepository>());
        var http = new DefaultHttpContext();
        return new HomeController(nav, new UserViewPreferenceRepository(db), user.Object, NullLogger<HomeController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>()),
        };
    }

    private static string? Json(ApplicationDbContext db) => db.UserViewPreferences.SingleOrDefault(p => p.UserId == 7 && p.ViewKey == Key)?.SettingsJson;

    [Fact]
    public void ViewKey_PasstInDieSpalte()
    {
        Key.Length.Should().BeLessThanOrEqualTo(50, "UserViewPreferences.ViewKey ist NVARCHAR(50)");
    }

    [Fact]
    public async Task Hinzufuegen_Sichtbar_LegtZeileMitAuditAn_UndLeitetInDenReiterZurueck()
    {
        using var db = TestDbContextFactory.Create();
        var ctrl = Build(db);
        var r = (RedirectToActionResult)await ctrl.FavoritUmschalten("Einbuchung", "alle");
        r.RouteValues!["reiter"].Should().Be("alle");
        var pref = db.UserViewPreferences.Single();
        (pref.ViewKey, pref.SettingsJson, pref.CreatedBy, pref.CreatedByWindows).Should().Be((Key, "{\"ziele\":[\"Einbuchung\"]}", "Tester", "AKE\\tester"));
        ctrl.TempData["SuccessMessage"].Should().Be("„Einbuchung“ zu den Favoriten hinzugefügt.");

        var vm = (StartseiteViewModel)((ViewResult)await Build(db).Index(null)).Model!;
        vm.Favoriten.Should().Equal("Einbuchung");
        vm.Reiter.Should().BeNull();
    }

    [Theory]
    [InlineData("Benutzer")]            // unsichtbar (admin)
    [InlineData("SageLagerbuchungen")]  // sichtbar, aber ohne Kategorie (keine Kachel)
    [InlineData("Quatsch")]             // unbekannt
    [InlineData(null)]
    public async Task Hinzufuegen_NichtErlaubt_KeineZeile_Warnung(string? key)
    {
        using var db = TestDbContextFactory.Create();
        var ctrl = Build(db);
        await ctrl.FavoritUmschalten(key!, "manipuliert");
        db.UserViewPreferences.Should().BeEmpty();
        ctrl.TempData["WarningMessage"].Should().Be("Diese Funktion ist für Sie nicht verfügbar.");
    }

    [Fact]
    public async Task Entfernen_DesLetzten_LoeschtZeile_UndUnsichtbarerKeyDarfEntferntWerden()
    {
        using var db = TestDbContextFactory.Create();
        await new UserViewPreferenceRepository(db).SaveAsync(7, Key, "{\"ziele\":[\"Benutzer\",\"Einbuchung\"]}", "x", "x");
        var ctrl = Build(db);
        await ctrl.FavoritUmschalten("Benutzer", "favoriten");
        Json(db).Should().Be("{\"ziele\":[\"Einbuchung\"]}", "Entfernen geht auch fuer einen nicht sichtbaren Key");
        db.UserViewPreferences.Single().ModifiedBy.Should().Be("Tester");
        await Build(db).FavoritUmschalten("Einbuchung", "favoriten");
        db.UserViewPreferences.Should().BeEmpty("leere Liste = keine Zeile");
    }

    [Fact]
    public async Task Bereinigen_EntferntNurNichtVerfuegbare()
    {
        using var db = TestDbContextFactory.Create();
        await new UserViewPreferenceRepository(db).SaveAsync(7, Key, "{\"ziele\":[\"Quatsch\",\"Einbuchung\",\"Benutzer\",\"SageLagerbuchungen\"]}", "x", "x");
        var ctrl = Build(db);
        var r = (RedirectToActionResult)await ctrl.FavoritenBereinigen();
        Json(db).Should().Be("{\"ziele\":[\"Einbuchung\"]}");
        r.RouteValues!["reiter"].Should().Be("favoriten");
        ctrl.TempData["SuccessMessage"].Should().Be("3 nicht verfügbare Favoriten entfernt.");
    }

    [Fact]
    public async Task OhneBenutzerId_KeinSchreiben_KeineFavoriten()
    {
        using var db = TestDbContextFactory.Create();
        var ctrl = Build(db, userId: null);
        await ctrl.FavoritUmschalten("Einbuchung", null);
        await ctrl.FavoritenBereinigen();
        db.UserViewPreferences.Should().BeEmpty();
        ((StartseiteViewModel)((ViewResult)await ctrl.Index("favoriten")).Model!).Favoriten.Should().BeNull();
    }

    [Theory]
    [InlineData("kaputt")]
    [InlineData("[\"Einbuchung\"]")]
    [InlineData("{\"ziele\":[1]}")]
    [InlineData("null")]
    public async Task KaputtesJson_GiltAlsLeer_OhneWurf(string json)
    {
        using var db = TestDbContextFactory.Create();
        await new UserViewPreferenceRepository(db).SaveAsync(7, Key, json, "x", "x");
        var vm = (StartseiteViewModel)((ViewResult)await Build(db).Index("unsinn")).Model!;
        vm.Favoriten.Should().BeEmpty();
        vm.Reiter.Should().BeNull("nur favoriten/alle werden durchgereicht");
        await Build(db).FavoritUmschalten("Einbuchung", null);
        Json(db).Should().Be("{\"ziele\":[\"Einbuchung\"]}");
    }
}
