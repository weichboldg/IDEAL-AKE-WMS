using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.HierarchischeStruktur;
using IdealAkeWms.Services.Navigation;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.45 (ADR 0034, Spec dashboard-kacheln): Menue und Startseite aus EINER Quelle.
/// T1 Regeln/Caching, T2 nie mehr als der Filter am Ziel, T3 Layout-Quelltext haengt jeden Link an seinen NavZiel-Key,
/// T4 Startseite ohne eigene Rechte.</summary>
public class NavigationSichtTests
{
    private static readonly string[] Schalter =
    [
        AppSettingKeys.TeileverfolgungAktiv, AppSettingKeys.BestellungenAktiv, AppSettingKeys.LagerbestellungAktiv, AppSettingKeys.LeitstandAktiv,
        AppSettingKeys.BdeAktiv, AppSettingKeys.FaCompletionAktiv, AppSettingKeys.FaHierarchyKommissionierlistenAktiv,
        AppSettingKeys.FaHierarchyBeschichtungAktiv, AppSettingKeys.FaHierarchyVormontageAktiv, AppSettingKeys.WareneingangBestellungAktiv,
        AppSettingKeys.EinkaufAktiv, AppSettingKeys.BlechfertigungAktiv,
    ];

    private static IEnumerable<string?> Rollen() => typeof(RoleKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string?)f.GetValue(null)).Append(null);

    /// <summary>Schalter-Kombinationen: alle an, alle aus, je einer aus; LagerbestellungAktiv fehlend bzw. "false"; Master an/aus.</summary>
    private static IEnumerable<(Dictionary<string, string?> App, bool Master)> Kombinationen()
    {
        Dictionary<string, string?> Alle(string? v) => Schalter.ToDictionary(k => k, _ => v);
        yield return (Alle("true"), true);
        yield return (Alle("false"), false);
        yield return (Alle(null), false);
        foreach (var k in Schalter)
        {
            var d = Alle("true"); d[k] = "false"; yield return (d, false);
        }
        var ohneLb = Alle("true"); ohneLb[AppSettingKeys.LagerbestellungAktiv] = null; yield return (ohneLb, true);
    }

    public static TheoryData<string?> RollenDaten() { var t = new TheoryData<string?>(); foreach (var r in Rollen()) t.Add(r); return t; }

    private sealed record Umgebung(ICurrentUserService User, Mock<IAppSettingRepository> App, IServiceSettingRepository Service, NavigationSicht Sicht);

    private static Umgebung Bau(string? rolle, Dictionary<string, string?> app, bool master)
    {
        var session = new FakeSession();
        session.Set(CurrentUserService.SessionKeyUserId, [7, 0, 0, 0]);
        var http = new Mock<HttpContext>();
        http.Setup(c => c.Session).Returns(session);
        var acc = new Mock<IHttpContextAccessor>();
        acc.Setup(a => a.HttpContext).Returns(http.Object);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(r => r.GetRoleKeysByUserIdAsync(It.IsAny<int>())).ReturnsAsync(rolle == null ? new List<string>() : [rolle]);
        var user = new CurrentUserService(acc.Object, roles.Object, new Mock<IUserRepository>().Object);

        var appRepo = new Mock<IAppSettingRepository>();
        appRepo.Setup(r => r.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string k) => app.GetValueOrDefault(k));
        var svc = new Mock<IServiceSettingRepository>();
        svc.Setup(r => r.GetValueAsync(HierarchischeStrukturKeys.Master)).ReturnsAsync(master ? "true" : "false");
        return new Umgebung(user, appRepo, svc.Object, new NavigationSicht(user, appRepo.Object, svc.Object));
    }

    // ---------------- T1
    [Fact]
    public void Katalog_KeyGleichFeldname_UndEindeutig()
    {
        var felder = typeof(NavZiele).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(NavZiel)).ToList();
        felder.Should().OnlyContain(f => ((NavZiel)f.GetValue(null)!).Key == f.Name);
        NavZiele.Alle.Select(z => z.Key).Should().OnlyHaveUniqueItems().And.HaveCount(felder.Count, "jedes Feld steht in Alle");
        NavZiele.Alle.Where(z => z.Kategorie != null).Should().OnlyContain(z => NavZiele.Kategorien.Contains(z.Kategorie!) && z.Beschreibung != null);
    }

    [Theory]
    [MemberData(nameof(RollenDaten))]
    public async Task Kacheln_SindImmerAuchImMenue_UndKeineKategorieLeer(string? rolle)
    {
        foreach (var (app, master) in Kombinationen())
        {
            var nav = await Bau(rolle, app, master).Sicht.GetAsync();
            foreach (var (kat, ziele) in nav.Kacheln)
            {
                ziele.Should().NotBeEmpty();
                ziele.Should().OnlyContain(z => nav.Zeigt(z) && z.Kategorie == kat);
            }
            nav.Kacheln.Select(k => k.Kategorie).Should().ContainInOrder(NavZiele.Kategorien.Where(k => nav.Kacheln.Any(x => x.Kategorie == k)));
        }
    }

    [Fact]
    public async Task OhneRolle_KeineKachel()
        => (await Bau(null, Schalter.ToDictionary(k => k, _ => (string?)"true"), true).Sicht.GetAsync()).Kacheln.Should().BeEmpty();

    [Theory]
    [InlineData("true", null, null, true, false)]
    [InlineData("false", null, null, true, false)]      // LagerbestellungAktiv fehlt = an
    [InlineData("false", "false", "true", true, false)]
    [InlineData("false", "false", "false", false, false)]
    public async Task Empfaenger_UndArtikelgruppen_FolgenDenNutzernDerEmpfaengergruppen(string bestellungen, string? lagerbestellung, string? blech,
        bool empfaenger, bool _)
    {
        var app = new Dictionary<string, string?> { [AppSettingKeys.BestellungenAktiv] = bestellungen, [AppSettingKeys.LagerbestellungAktiv] = lagerbestellung,
            [AppSettingKeys.BlechfertigungAktiv] = blech };
        var nav = await Bau(RoleKeys.MasterDataRead, app, false).Sicht.GetAsync();
        nav.Zeigt(NavZiele.Empfaenger).Should().Be(empfaenger);
        nav.Zeigt(NavZiele.ArtikelgruppenZuordnung).Should().Be(bestellungen == "true");
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    public async Task OseonAgUebersicht_NurBeiAktivemOseonModul(string? teileverfolgung, bool sichtbar)
    {
        var nav = await Bau(RoleKeys.Reporting, new() { [AppSettingKeys.TeileverfolgungAktiv] = teileverfolgung }, false).Sicht.GetAsync();
        nav.Zeigt(NavZiele.OseonAgUebersicht).Should().Be(sichtbar);
    }

    [Theory]
    [InlineData(RoleKeys.Admin, true, true)]
    [InlineData(RoleKeys.Admin, false, false)]
    [InlineData(RoleKeys.MasterData, true, false)]
    public async Task ArbeitsgaengeSage_NurAdminMitMaster(string rolle, bool master, bool sichtbar)
        => (await Bau(rolle, new(), master).Sicht.GetAsync()).Zeigt(NavZiele.ArbeitsgaengeSage).Should().Be(sichtbar);

    [Fact]
    public async Task Artikelinfo_FolgtStammdatenLesen_NichtKommissionierung()
    {
        (await Bau(RoleKeys.Picking, new(), false).Sicht.GetAsync()).Zeigt(NavZiele.Artikelinfo).Should().BeFalse();
        (await Bau(RoleKeys.MasterDataRead, new(), false).Sicht.GetAsync()).Zeigt(NavZiele.Artikelinfo).Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_RechnetNurEinmalJeRequest()
    {
        var u = Bau(RoleKeys.Admin, new(), false);
        var a = await u.Sicht.GetAsync();
        (await u.Sicht.GetAsync()).Should().BeSameAs(a);
        u.App.Verify(r => r.GetValueAsync(AppSettingKeys.TeileverfolgungAktiv), Times.Once);
    }

    // ---------------- T2
    /// <summary>Gates, die nicht als Filter, sondern im Action-Rumpf pruefen — die Regel in NavigationSicht enthaelt sie ausdruecklich.</summary>
    private static readonly Dictionary<string, string> InlineGates = new()
    {
        ["FaAbarbeitungsliste"] = "FaWorklistController.Index prueft FaCompletionAktiv inline (Regel: faCompletion && vorbau)",
        ["ArbeitsgaengeSage"] = "SageFaArbeitsgaengeController prueft den Master inline (Regel: admin && master)",
    };

    [Theory]
    [MemberData(nameof(RollenDaten))]
    public async Task SichtbaresZiel_LaesstJedenFilterDurch(string? rolle)
    {
        InlineGates.Keys.Should().BeSubsetOf(NavZiele.Alle.Select(z => z.Key));
        foreach (var (app, master) in Kombinationen())
        {
            var u = Bau(rolle, app, master);
            var nav = await u.Sicht.GetAsync();
            var sp = new ServiceCollection().AddSingleton(u.User).AddSingleton(u.App.Object).AddSingleton(u.Service).BuildServiceProvider();
            foreach (var z in NavZiele.Alle.Where(nav.Zeigt))
                foreach (var f in Filter(z))
                {
                    var filter = (IAsyncActionFilter)ActivatorUtilities.CreateInstance(sp, f.ImplementationType);
                    var ctx = new ActionExecutingContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                        new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
                    var durch = false;
                    await filter.OnActionExecutionAsync(ctx, () => { durch = true; return Task.FromResult<ActionExecutedContext>(null!); });
                    durch.Should().BeTrue($"{z.Key} ist fuer Rolle '{rolle}' sichtbar, {f.ImplementationType.Name} sperrt aber");
                }
        }
    }

    private static IEnumerable<TypeFilterAttribute> Filter(NavZiel z)
    {
        var typ = typeof(NavigationSicht).Assembly.GetType($"IdealAkeWms.Controllers.{z.Controller}Controller");
        typ.Should().NotBeNull($"Controller zu {z.Key}");
        var actions = typ!.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == z.Action && !m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>().Any(a => !a.HttpMethods.Contains("GET")))
            .ToList();
        actions.Should().NotBeEmpty($"GET-Action {z.Controller}/{z.Action} zu {z.Key}");
        return typ.GetCustomAttributes<TypeFilterAttribute>(true).Concat(actions.SelectMany(m => m.GetCustomAttributes<TypeFilterAttribute>(true)))
            .Where(f => typeof(IAsyncActionFilter).IsAssignableFrom(f.ImplementationType));
    }

    // ---------------- T3 / T4 (Quelltext)
    private static string Datei(params string[] teile)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine([dir!.FullName, "IdealAkeWms", .. teile]));
    }

    [Fact]
    public void Layout_JederMenuelinkHaengtAnSeinemNavZiel_OhneEigeneRechteabfrage()
    {
        var zeilen = Datei("Views", "Shared", "_Layout.cshtml").Split('\n');
        var start = Array.FindIndex(zeilen, l => l.Contains("navbar-nav me-auto flex-wrap"));
        var ende = Array.FindIndex(zeilen, l => l.Contains("user-info"));
        start.Should().BePositive(); ende.Should().BeGreaterThan(start);
        var block = string.Join('\n', zeilen[start..ende]);
        block.Should().NotContain("CurrentUserService.").And.NotContain("AppSettings.GetValueAsync").And.NotContain("ServiceSettings.GetValueAsync");

        var ziele = NavZiele.Alle.ToDictionary(z => (z.Controller, z.Action), z => z.Key);
        var gefunden = new HashSet<string>();
        for (var i = start; i < ende; i++)
        {
            var m = Regex.Match(zeilen[i], @"asp-controller=""(\w+)"" asp-action=""(\w+)""");
            if (!m.Success) continue;
            ziele.Should().ContainKey((m.Groups[1].Value, m.Groups[2].Value), $"Menuelink Zeile {i + 1} braucht ein NavZiel");
            var key = ziele[(m.Groups[1].Value, m.Groups[2].Value)];
            Bedingung(zeilen, start, i).Should().Contain($"NavZiele.{key})", $"Link {key} (Zeile {i + 1}) haengt an seiner eigenen Regel");
            gefunden.Add(key);
        }
        gefunden.Should().BeEquivalentTo(NavZiele.Alle.Select(z => z.Key), "jedes NavZiel steht im Menue");
    }

    /// <summary>Naechste umschliessende Razor-Bedingung (Zeile vor der oeffnenden Klammer).</summary>
    private static string Bedingung(string[] zeilen, int start, int i)
    {
        var tiefe = 0;
        for (var j = i - 1; j > start; j--)
        {
            var t = zeilen[j].Trim();
            if (t == "}") tiefe++;
            else if (t == "{") { if (tiefe == 0) return zeilen[j - 1]; tiefe--; }
        }
        return "";
    }

    [Fact]
    public void Startseite_OhneViewBagUndOhneHartcodierteLinks()
    {
        Datei("Views", "Home", "Index.cshtml").Should().NotContain("ViewBag.").And.NotContain("asp-controller=");
        Datei("Controllers", "HomeController.cs").Should().NotContain("ViewBag");
    }
}
