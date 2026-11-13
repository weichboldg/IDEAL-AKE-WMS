using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;

namespace IdealAkeWms.Tests.Services.SageSdata;

/// <summary>Spec 2026-11-13-sdata-teil-2-userfelder-stueckliste: F6 Zugangsdaten, F3/F4 Aufbereitung, Lese-Client nur GET, Register-Pruefungen.</summary>
public class SageTestSdataTests
{
    private static readonly IDataProtectionProvider Schutz = new EphemeralDataProtectionProvider();
    private static IConfiguration Config(string? user = "appuser", string? pw = "apppw") =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [SdataZugangsdaten.AppsettingsBenutzer] = user,
            [SdataZugangsdaten.AppsettingsKennwort] = pw,
        }).Build();

    private static string Datei(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "SageSdata", name));

    // ===== F6 Zugangsdaten =====

    [Fact]
    public void Schuetzen_Praefix_Rueckweg_UndLaengeFuer128ZeichenPasstInValue()
    {
        var kennwort = new string('ä', SdataZugangsdaten.MaxLaenge);
        var gespeichert = SdataZugangsdaten.Schuetzen(kennwort, Schutz);

        gespeichert.Should().StartWith("dp1:").And.NotContain(kennwort);
        gespeichert.Length.Should().BeLessThanOrEqualTo(500, "ServiceSettings.Value ist nvarchar(500)");
        SdataZugangsdaten.Entschluesseln(gespeichert, Schutz).Should().Be(kennwort);
    }

    [Fact]
    public void Aufloesen_DbGewinnt()
    {
        var z = SdataZugangsdaten.Aufloesen(" dbuser ", SdataZugangsdaten.Schuetzen("dbpw", Schutz), Config(), Schutz);
        z.Should().BeEquivalentTo(new { Benutzer = "dbuser", Kennwort = "dbpw", Fehler = (string?)null, Warnung = (string?)null });
        z.ToString().Should().NotContain("dbpw");
    }

    [Theory]
    [InlineData("klartext")]                 // per SQL im Klartext eingetragen (AK 7)
    [InlineData("dp1:keinGueltigerSchutz")]
    public void Aufloesen_NichtEntschluesselbar_Fehler_KeinRueckfall(string gespeichert)
    {
        var z = SdataZugangsdaten.Aufloesen("dbuser", gespeichert, Config(), Schutz);

        z.Fehler.Should().Be(SdataZugangsdaten.FehlerNichtEntschluesselbar);
        z.Kennwort.Should().BeEmpty("nie der appsettings-Wert und nie der gespeicherte Text");
        z.Benutzer.Should().Be("dbuser");
        z.Warnung.Should().BeNull();
    }

    [Fact]
    public void Aufloesen_FremderSchluesselring_Fehler()
    {
        // DEV mit kopierter Prod-DB: anderer Schluesselring
        var fremd = SdataZugangsdaten.Schuetzen("prodpw", new EphemeralDataProtectionProvider());
        SdataZugangsdaten.Aufloesen("dbuser", fremd, Config(), Schutz).Fehler.Should().NotBeNull();
        SdataZugangsdaten.Status(fremd, Schutz).Should().Be(SdataZugangsdaten.StatusNichtEntschluesselbar);
    }

    [Fact]
    public void Aufloesen_OhneDbBenutzer_RueckfallAppsettings_MitWarnung()
    {
        var z = SdataZugangsdaten.Aufloesen("", null, Config(), Schutz);
        z.Should().BeEquivalentTo(new { Benutzer = "appuser", Kennwort = "apppw", Quelle = "appsettings", Fehler = (string?)null,
            Warnung = SdataZugangsdaten.WarnungAppsettings });
        z.Warnung.Should().NotContain("apppw").And.NotContain("appuser");
        SdataZugangsdaten.KennwortStatus(z, null, Schutz).Should().Be(SdataZugangsdaten.StatusAppsettings);
    }

    [Fact]
    public void Aufloesen_Nichts_KeineQuelle()
    {
        var z = SdataZugangsdaten.Aufloesen(null, null, Config(null, null), Schutz);
        z.Quelle.Should().Be("keine");
        z.Benutzer.Should().BeEmpty();
    }

    [Fact]
    public void Katalog_KenntBenutzerUndKennwort_KennwortAlsGeheim()
    {
        ServiceSettingDefinitions.TryGet(SdataZugangsdaten.BenutzerKey, out var u).Should().BeTrue();
        u!.Type.Should().Be(SettingType.String);
        ServiceSettingDefinitions.TryGet(SdataZugangsdaten.KennwortKey, out var p).Should().BeTrue();
        p!.Type.Should().Be(SettingType.Geheim);
        p.Warnhinweis.Should().NotBeNullOrEmpty();
        p.Anzeigename.Should().Be("SData: Kennwort");
    }

    [Fact]
    public void Validator_Geheim_ZitiertNieDenWert()
    {
        var d = ServiceSettingDefinitions.All.Single(x => x.Key == SdataZugangsdaten.KennwortKey);
        var zuLang = new string('x', 129) + "GEHEIMNIS";
        var r = SettingValidator.Validate(d, zuLang, exakt: true);
        r.Gueltig.Should().BeFalse();
        r.Fehler.Should().NotContain("GEHEIMNIS");
        SettingValidator.Validate(d, "ok", exakt: true).Gueltig.Should().BeTrue();
    }

    [Fact]
    public void SettingItem_UndEinstellungspruefung_ZeigenNieDasKennwort()
    {
        var d = ServiceSettingDefinitions.All.Single(x => x.Key == SdataZugangsdaten.KennwortKey);
        var item = SettingItem.Aus(d, "klartextGEHEIM", exakt: true);
        item.Wirksam.Should().BeEmpty();
        item.RohWert.Should().BeNull();

        var befunde = Einstellungspruefung.Befunde(Array.Empty<(string, string?)>(),
            new[] { (SdataZugangsdaten.KennwortKey, (string?)"klartextGEHEIM") },
            new Dictionary<SettingStammdaten, IReadOnlyList<string>>());
        befunde.Should().NotContain(b => b.Contains("klartextGEHEIM"));
    }

    // ===== /ServiceSettings: Speichern, Fallback-Masken =====

    private static (ServiceSettingsController C, Mock<IServiceSettingRepository> Repo) Svc(List<ServiceSetting>? bestand = null)
    {
        var repo = new Mock<IServiceSettingRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(bestand ?? new List<ServiceSetting>());
        var c = new ServiceSettingsController(repo.Object, Mock.Of<IdealAkeWms.Services.SyncLogger.ISyncLogger>(),
            Mock.Of<IdealAkeWms.Services.ICurrentUserService>(), Schutz, Config(), NullLogger<ServiceSettingsController>.Instance);
        c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        c.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(c.HttpContext, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        return (c, repo);
    }

    [Fact]
    public async Task Speichern_Kennwort_Verschluesselt()
    {
        var (c, repo) = Svc();
        string? geschrieben = null;
        repo.Setup(r => r.UpsertAsync(SdataZugangsdaten.KennwortKey, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string, string>((_, v, _, _) => geschrieben = v).Returns(Task.CompletedTask);

        var r = await c.SaveSettings(new Dictionary<string, string> { [SdataZugangsdaten.KennwortKey] = "neuesKennwort" });

        r.Should().BeOfType<RedirectToActionResult>();
        geschrieben.Should().StartWith("dp1:").And.NotContain("neuesKennwort");
        SdataZugangsdaten.Entschluesseln(geschrieben, Schutz).Should().Be("neuesKennwort");
    }

    [Fact]
    public async Task Speichern_LeeresKennwort_Unveraendert_Entfernen_Leert()
    {
        var bestand = new List<ServiceSetting> { new() { Key = SdataZugangsdaten.KennwortKey, Value = SdataZugangsdaten.Schuetzen("alt", Schutz) } };
        var (c, repo) = Svc(bestand);
        await c.SaveSettings(new Dictionary<string, string> { [SdataZugangsdaten.KennwortKey] = "" });
        repo.Verify(r => r.UpsertAsync(SdataZugangsdaten.KennwortKey, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        await c.SaveSettings(new Dictionary<string, string> { [SdataZugangsdaten.KennwortKey] = "" }, new[] { SdataZugangsdaten.KennwortKey });
        repo.Verify(r => r.UpsertAsync(SdataZugangsdaten.KennwortKey, "", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Speichern_FehlerAndererEinstellung_KennwortNichtGespeichert_NichtZurueckgegeben()
    {
        var (c, repo) = Svc();
        var r = await c.SaveSettings(new Dictionary<string, string>
        {
            [SdataZugangsdaten.KennwortKey] = "eingabeGEHEIM",
            ["Sync:BomCacheWeeks"] = "keineZahl",
        });

        var view = r.Should().BeOfType<ViewResult>().Subject;
        repo.Verify(x => x.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        c.ViewData["GeheimHinweis"].Should().Be("Kennwort nicht gespeichert, bitte erneut eingeben.");
        var vm = (ServiceSettingsViewModel)view.Model!;
        var item = vm.Groups.SelectMany(g => g.Items).Single(i => i.Key == SdataZugangsdaten.KennwortKey);
        item.Wirksam.Should().BeEmpty();
        item.GeheimStatus.Should().Be(SdataZugangsdaten.StatusNichtGesetzt);
    }

    [Fact]
    public async Task Index_ZeigtNurStatus()
    {
        var (c, _) = Svc(new List<ServiceSetting> { new() { Key = SdataZugangsdaten.KennwortKey, Value = "klartext" } });
        var vm = (ServiceSettingsViewModel)((ViewResult)await c.Index()).Model!;
        var item = vm.Groups.SelectMany(g => g.Items).Single(i => i.Key == SdataZugangsdaten.KennwortKey);
        item.GeheimStatus.Should().Be(SdataZugangsdaten.StatusNichtEntschluesselbar);
        item.Wirksam.Should().BeEmpty();
        vm.OrphanEntries.Should().NotContain(o => o.Key == SdataZugangsdaten.KennwortKey);
    }

    [Fact]
    public async Task Freitext_CreateEditDelete_LehnenGeheimAb()
    {
        var (c, repo) = Svc(new List<ServiceSetting> { new() { Key = SdataZugangsdaten.KennwortKey, Value = "dp1:x" } });
        (await c.Create(new ServiceSetting { Key = SdataZugangsdaten.KennwortKey, Value = "klartext" })).Should().BeOfType<RedirectToActionResult>();
        (await c.Edit(SdataZugangsdaten.KennwortKey)).Should().BeOfType<RedirectToActionResult>();
        (await c.Edit(SdataZugangsdaten.KennwortKey, new ServiceSetting { Key = SdataZugangsdaten.KennwortKey, Value = "klartext" }))
            .Should().BeOfType<RedirectToActionResult>();
        (await c.Delete(SdataZugangsdaten.KennwortKey)).Should().BeOfType<RedirectToActionResult>();
        repo.Verify(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        repo.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
        c.TempData["WarningMessage"].Should().NotBeNull();
    }

    // ===== Lese-Client: nur GET, URL (AK 12) =====

    private sealed class Stub : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _antwort;
        public List<HttpRequestMessage> Anfragen { get; } = new();
        public Stub(Func<HttpRequestMessage, HttpResponseMessage> antwort) => _antwort = antwort;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Anfragen.Add(request);
            return Task.FromResult(_antwort(request));
        }
    }

    private static readonly SdataLeseZiel Ziel = new("https://sagetest01.ake.at:5493", "ol", "ake_TEST2026;1", "u", "p", true);

    [Fact]
    public async Task Client_EineSeiteMitInclude_UndSchema_NurGet()
    {
        var stub = new Stub(r => r.RequestUri!.AbsolutePath.EndsWith("$schema")
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Datei("schema-ressourcenliste.xml")) }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Datei("ressourcenliste-86003871.json"), Encoding.UTF8, "application/json") });
        var client = new SageSdataReadClient(new HttpClient(stub), NullLogger<SageSdataReadClient>.Instance);

        var seite = await client.QueryPageAsync(Ziel, SageSdataReadClient.VertragResourcesSpecific, "PpsRessourcenListenInfo",
            "Mandant eq 1 and Artikelnummer eq '86003871'", "Artikelnummer", "PpsRessourcenPositionenResolvedInfo,SpecificUserfields", 1, CancellationToken.None);
        var schema = await client.GetSchemaAsync(Ziel, SageSdataReadClient.VertragResourcesSpecific, "PpsRessourcenListenInfo", CancellationToken.None);
        await client.GetSchemaAsync(Ziel, SageSdataReadClient.VertragResources, null, CancellationToken.None);

        stub.Anfragen.Should().OnlyContain(a => a.Method == HttpMethod.Get);
        seite.Zeilen.Should().ContainSingle();
        seite.Url.Should().Be("https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServicesResourcesSpecific/ake_TEST2026;1/PpsRessourcenListenInfo"
            + "?format=json&orderBy=Artikelnummer&count=1&startIndex=0&include=PpsRessourcenPositionenResolvedInfo,SpecificUserfields"
            + "&where=Mandant eq 1 and Artikelnummer eq '86003871'");
        stub.Anfragen[1].RequestUri!.AbsoluteUri.Should().EndWith("/CommonWawiServicesResourcesSpecific/ake_TEST2026;1/PpsRessourcenListenInfo/$schema");
        stub.Anfragen[2].RequestUri!.AbsoluteUri.Should().EndWith("/CommonWawiServicesResources/ake_TEST2026;1/$schema");
        schema.Should().Contain("PpsRessourcenListenInfo--type");
    }

    [Fact]
    public async Task Client_Schema401_WirftMitStatus()
    {
        var stub = new Stub(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("") });
        var client = new SageSdataReadClient(new HttpClient(stub), NullLogger<SageSdataReadClient>.Instance);
        var ex = await ((Func<Task>)(() => client.GetSchemaAsync(Ziel, "CommonWawiServicesResources", "PpsFaBelegeInfo", CancellationToken.None)))
            .Should().ThrowAsync<SdataLeseException>();
        ex.Which.Status.Should().Be(401);
    }

    // ===== Aufbereitung (F3/F4) mit echten Antworten des AKE-Tests (Lesetest 2026-11-13) =====

    private static JsonElement Kopf() => JsonDocument.Parse(Datei("ressourcenliste-86003871.json")).RootElement.GetProperty("$resources")[0];

    [Fact]
    public void Schema_Ressourcenliste_FelderUndRechte()
    {
        var s = SageTestAufbereitung.SchemaLesen(Datei("schema-ressourcenliste.xml"), "PpsRessourcenListenInfo")!;
        s.Lesen.Should().BeTrue();
        s.Anlegen.Should().BeFalse();
        s.Aendern.Should().BeFalse();
        s.Loeschen.Should().BeFalse();
        s.Felder.Select(f => f.Name).Should().StartWith(new[] { "Mandant", "Artikelnummer" });
        s.Felder.Single(f => f.Name == "Mandant").Should().BeEquivalentTo(new { Typ = "short", Pflicht = true });
        SageTestAufbereitung.SchemaLesen(Datei("schema-ressourcenliste.xml"), "Unbekannt").Should().BeNull();
        SageTestAufbereitung.SchemaLesen("kein xml", "X").Should().BeNull();
    }

    [Fact]
    public void Feldliste_Benutzerfelder_AlsEigeneZeilen_SchemaUndDaten()
    {
        var schema = SageTestAufbereitung.SchemaLesen(Datei("schema-ressourcenliste.xml"), "PpsRessourcenListenInfo");
        var felder = SageTestAufbereitung.Feldliste(schema, new[] { Kopf() });

        var user = felder.Single(f => f.Feld == "SpecificUserfields.USER_VKZuschlag");
        user.Benutzerfeld.Should().BeTrue();
        user.InDaten.Should().BeTrue();
        user.TypLaenge.Should().Be("decimal (8) (Daten)");
        felder.Single(f => f.Feld == "Mandant").Should().BeEquivalentTo(new { ImSchema = true, InDaten = true, Benutzerfeld = false });
        felder.Should().Contain(f => f.Feld == "PpsRessourcenPositionenResolvedInfo[0].SpecificUserfields.USER_AUSCAD" && f.Benutzerfeld);
        felder.Should().NotContain(f => f.Feld.StartsWith("$"));
    }

    [Fact]
    public void RessourcenBaum_86003871_EbeneEinsUndZwei_Eingerueckt()
    {
        var pos = Kopf().GetProperty("PpsRessourcenPositionenResolvedInfo").EnumerateArray().ToList();
        var baum = SageTestAufbereitung.RessourcenBaum(pos);

        baum.Should().HaveCount(17);
        baum.Count(z => z.Tiefe == 0).Should().Be(11);
        baum.Count(z => z.Tiefe == 1).Should().Be(6);
        baum.Should().OnlyContain(z => z.Ebene == (z.Tiefe + 1).ToString());
        baum[0].Ressourcetyp.Should().Be("MA");
        baum[0].AlleFelder.Should().Contain(f => f.Feld == "SpecificUserfields.USER_AUSCAD" && f.Wert == "-1");
    }

    [Fact]
    public void FaBaum_SubFaUnterDerPositionMitNachfolger()
    {
        static JsonElement J(string s) => JsonDocument.Parse(s).RootElement;
        var fas = new[] { J("""{"BelID":835,"StrukturID":835,"Artikelnummer":"A"}"""), J("""{"BelID":836,"StrukturID":835,"Artikelnummer":"B"}""") };
        var pos = new[]
        {
            J("""{"BelPosID":1,"BelID":835,"Position":1,"RessourceTyp":"MA","RessourceNummer":"B","BelIDNachfolger":836,"Sollmenge":2,"Istmenge":0}"""),
            J("""{"BelPosID":2,"BelID":836,"Position":1,"RessourceTyp":"MA","RessourceNummer":"C","BelIDNachfolger":0}"""),
        };
        var baum = SageTestAufbereitung.FaBaum(fas, pos, 835);

        baum.Select(z => (z.IstFa, z.Tiefe, z.Position)).Should().Equal((true, 0, "835"), (false, 1, "1"), (true, 2, "836"), (false, 3, "1"));
        baum[1].Ressourcetyp.Should().Be("MA", "Feldname ohne Gross/Klein (RessourceTyp)");
        baum[1].BelPosId.Should().Be("1");
    }

    [Theory]
    [InlineData("ake_TEST2026;1", 1)]
    [InlineData("x;y;12", 12)]
    [InlineData("ohne", null)]
    [InlineData(null, null)]
    public void Mandant_ZahlNachDemLetztenSemikolon(string? ds, int? erwartet) =>
        SageTestAufbereitung.MandantAusDataset(ds).Should().Be(erwartet);

    // ===== Register: keine Anfrage bei ungueltiger Eingabe / ohne Konfiguration (AK 9, 13) =====

    private static (SageTestController C, Mock<ISageSdataReadClient> Client) Register(string? baseUrl, Dictionary<string, StringValues> query)
    {
        var settings = new Mock<IServiceSettingRepository>();
        settings.Setup(s => s.GetValueAsync("SData:BaseUrl")).ReturnsAsync(baseUrl);
        settings.Setup(s => s.GetValueAsync("SData:Dataset")).ReturnsAsync("ake_TEST2026;1");
        var client = new Mock<ISageSdataReadClient>(MockBehavior.Strict);
        var c = new SageTestController(client.Object, settings.Object, Config(), Schutz);
        var http = new DefaultHttpContext();
        http.Request.Query = new QueryCollection(query);
        c.ControllerContext = new ControllerContext { HttpContext = http };
        return (c, client);
    }

    [Theory]
    [InlineData("CommonWawiServicesResources", "PpsFaBelegeInfo", "", 1, null, "orderBy")]
    [InlineData("CommonWawiServicesResources", "PpsFaBelegeInfo", "BelID", 0, null, "1 bis 5")]
    [InlineData("CommonWawiServicesResources", "PpsFaBelegeInfo", "BelID", 6, null, "1 bis 5")]
    [InlineData("Fremd", "PpsFaBelegeInfo", "BelID", 1, null, "Vertrag")]
    [InlineData("RealTimeData", "dtsFaBelegsuche.Sage.PPS", "BelID", 1, null, "where")]
    [InlineData("CommonWawiServicesResources", "a/b", "BelID", 1, null, "Ressource")]
    public async Task Feldliste_UngueltigeEingabe_Meldung_KeineAnfrage(string vertrag, string ressource, string orderBy, int anzahl, string? where, string meldung)
    {
        var (c, _) = Register("https://sagetest01:5493", new() { ["Ressource"] = ressource });
        var r = await c.Feldliste(new SageTestFeldlisteViewModel { Vertrag = vertrag, Ressource = ressource, OrderBy = orderBy, Anzahl = anzahl, Where = where },
            CancellationToken.None);
        var vm = (SageTestFeldlisteViewModel)((ViewResult)r).Model!;
        vm.Fehler.Should().Contain(f => f.Contains(meldung));
        vm.Ausgefuehrt.Should().BeFalse(); // Strict-Mock: jeder Client-Aufruf wuerde werfen
    }

    [Fact]
    public async Task UngueltigeZahl_Meldung_KeineAnfrage() // QA Q-2: „abc“ fiel still auf den Default
    {
        var (c, _) = Register("https://sagetest01:5493", new() { ["Ressource"] = "PpsFaBelegeInfo", ["Anzahl"] = "abc" });
        c.ModelState.AddModelError("Anzahl", "The value 'abc' is not valid.");
        var f = (SageTestFeldlisteViewModel)((ViewResult)await c.Feldliste(
            new SageTestFeldlisteViewModel { Ressource = "PpsFaBelegeInfo", OrderBy = "BelID" }, CancellationToken.None)).Model!;
        f.Fehler.Should().ContainSingle(x => x.StartsWith("Anzahl:"));
        f.Ausgefuehrt.Should().BeFalse();

        c.ModelState.AddModelError("FaNummer", "The value 'abc' is not valid.");
        var s = (SageTestStuecklisteViewModel)((ViewResult)await c.Stueckliste(new SageTestStuecklisteViewModel(), CancellationToken.None)).Model!;
        s.Fehler.Should().ContainSingle(x => x.StartsWith("FaNummer:"));
        s.Ausgefuehrt.Should().BeFalse();
    }

    [Fact]
    public void Stueckliste_Einrueckung_KulturunabhaengigFormatiert() // QA Q-1: „0,5rem“ unter de-AT ist ungueltiges CSS
    {
        var view = File.ReadAllText(Path.Combine(Rueckwaerts("IdealAkeWms"), "Views", "SageTest", "Stueckliste.cshtml"));
        view.Should().Contain("padding-left: @((0.5 + z.Tiefe * 1.25).ToString(System.Globalization.CultureInfo.InvariantCulture))rem");
    }

    private static string Rueckwaerts(string ordner)
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !Directory.Exists(Path.Combine(d.FullName, ordner))) d = d.Parent;
        return Path.Combine(d!.FullName, ordner);
    }

    [Fact]
    public async Task OhneBaseUrl_NichtKonfiguriert_KeineAnfrage()
    {
        var (c, _) = Register(null, new() { ["Ressource"] = "PpsFaBelegeInfo", ["Artikelnummer"] = "86003871" });
        var f = (SageTestFeldlisteViewModel)((ViewResult)await c.Feldliste(
            new SageTestFeldlisteViewModel { Ressource = "PpsFaBelegeInfo", OrderBy = "BelID" }, CancellationToken.None)).Model!;
        f.Stand!.NichtKonfiguriert.Should().Contain("SData:BaseUrl");
        f.Ausgefuehrt.Should().BeFalse();
        var s = (SageTestStuecklisteViewModel)((ViewResult)await c.Stueckliste(
            new SageTestStuecklisteViewModel { Artikelnummer = "86003871" }, CancellationToken.None)).Model!;
        s.Ausgefuehrt.Should().BeFalse();
    }

    [Fact]
    public async Task Feldliste_Schema401_NimmtVertragsSchema()
    {
        var (c, client) = Register("https://sagetest01:5493", new() { ["Ressource"] = "PpsRessourcenListenInfo" });
        client.Setup(x => x.GetSchemaAsync(It.IsAny<SdataLeseZiel>(), "CommonWawiServicesResourcesSpecific", "PpsRessourcenListenInfo", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SdataLeseException("PpsRessourcenListenInfo", 401, "Unauthorized"));
        client.Setup(x => x.GetSchemaAsync(It.IsAny<SdataLeseZiel>(), "CommonWawiServicesResourcesSpecific", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Datei("schema-ressourcenliste.xml"));
        client.Setup(x => x.QueryPageAsync(It.IsAny<SdataLeseZiel>(), "CommonWawiServicesResourcesSpecific", "PpsRessourcenListenInfo", "Mandant eq 1",
                "Artikelnummer", "SpecificUserfields", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SdataEineSeite(new[] { Kopf() }, 1, "url", 5));

        var vm = (SageTestFeldlisteViewModel)((ViewResult)await c.Feldliste(new SageTestFeldlisteViewModel
        {
            Vertrag = "CommonWawiServicesResourcesSpecific", Ressource = "PpsRessourcenListenInfo", Where = "Mandant eq 1",
            OrderBy = "Artikelnummer", Include = "SpecificUserfields", Anzahl = 2,
        }, CancellationToken.None)).Model!;

        vm.Fehler.Should().BeEmpty();
        vm.Hinweise.Should().Contain(h => h.Contains("401")).And.Contain("Felder aus dem Schema des Vertrags.");
        vm.Schema.Should().NotBeNull();
        vm.Felder.Should().Contain(f => f.Feld == "SpecificUserfields.USER_VKZuschlag");
    }

    [Fact]
    public async Task Stueckliste_UnbekannterArtikel_Meldung()
    {
        var (c, client) = Register("https://sagetest01:5493", new() { ["Artikelnummer"] = "999" });
        client.Setup(x => x.QueryPageAsync(It.IsAny<SdataLeseZiel>(), "CommonWawiServicesResourcesSpecific", "PpsRessourcenListenInfo",
                "Mandant eq 1 and Artikelnummer eq '999'", "Artikelnummer", "PpsRessourcenPositionenResolvedInfo,SpecificUserfields", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SdataEineSeite(Array.Empty<JsonElement>(), 0, "url", 5));

        var vm = (SageTestStuecklisteViewModel)((ViewResult)await c.Stueckliste(new SageTestStuecklisteViewModel { Artikelnummer = "999" }, CancellationToken.None)).Model!;

        vm.Fehler.Should().ContainSingle().Which.Should().Contain("Keine Ressourcenliste für Artikel 999");
        vm.Zeilen.Should().BeEmpty();
    }

    [Fact]
    public async Task Stueckliste_Artikel_86003871_KopfUndBaum()
    {
        var (c, client) = Register("https://sagetest01:5493", new() { ["Artikelnummer"] = "86003871" });
        client.Setup(x => x.QueryPageAsync(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SdataEineSeite(new[] { Kopf() }, 1, "url", 5));

        var vm = (SageTestStuecklisteViewModel)((ViewResult)await c.Stueckliste(new SageTestStuecklisteViewModel { Artikelnummer = "86003871" }, CancellationToken.None)).Model!;

        vm.Fehler.Should().BeEmpty();
        vm.Kopf.Should().Contain(k => k.Feld == "SpecificUserfields.USER_VKZuschlag");
        vm.Kopf.Should().NotContain(k => k.Feld.StartsWith("PpsRessourcenPositionenResolvedInfo"));
        vm.Zeilen.Should().HaveCount(17);
        vm.Mandant.Should().Be(1, "aus dem Dataset ake_TEST2026;1");
    }

    [Fact]
    public void SageTest_NurAdmin() =>
        typeof(SageTestController).GetCustomAttributes(typeof(IdealAkeWms.Filters.RequireAdminAccessAttribute), true).Should().ContainSingle();
}
