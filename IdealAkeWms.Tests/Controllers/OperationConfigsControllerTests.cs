using System.Reflection;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>
/// Reiter „Arbeitsgänge" der BDE-Stammdaten (Spec 2026-10-09-arbeitsgang-stammdaten-vereinheitlichen, v1.54.0): Zugriff nur
/// Admin und NICHT an BdeAktiv gebunden, Liste mit Server-Spaltenfilter, Dubletten-Warnung, Anlegen/Aendern mit Audit,
/// Ablehnung leerer und getrimmt/case-insensitiv doppelter Kuerzel, alte URL als 302. Reiter-Sichtbarkeit je Rolle und
/// BdeAktiv ist Razor-Logik im Partial → Manual-UAT TS-87.1/87.2.
/// </summary>
public class OperationConfigsControllerTests
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create(Guid.NewGuid().ToString());
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly Mock<IServiceSettingRepository> _settings = new();

    public OperationConfigsControllerTests()
    {
        _user.Setup(u => u.GetDisplayName()).Returns("Admin");
        _user.Setup(u => u.GetWindowsUserName()).Returns(@"DOM\admin");
        _user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
    }

    private OperationConfigsController Ctrl(string? query = null)
    {
        var c = new OperationConfigsController(new OperationConfigRepository(_db), _user.Object, _settings.Object,
            new FaHierarchyNodeRepository(_db), new ProductionWorkplaceRepository(_db));
        var http = new DefaultHttpContext();
        if (query != null) http.Request.QueryString = new QueryString(query);
        c.ControllerContext = new ControllerContext { HttpContext = http };
        c.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return c;
    }

    private void Seed(params (string Name, int Offset, bool Relevant)[] rows)
    {
        foreach (var r in rows)
            _db.OperationConfigs.Add(new OperationConfig
            {
                OperationName = r.Name, DueDateOffsetDays = r.Offset, IsExternalSystemRelevant = r.Relevant,
                CreatedAt = DateTime.Now, CreatedBy = "system", CreatedByWindows = "system"
            });
        _db.SaveChanges();
    }

    [Fact]
    public void Zugriff_NurAdmin_OhneBdeAktivGate_PostsMitAntiforgery()
    {
        var t = typeof(OperationConfigsController);
        t.GetCustomAttribute<RequireAdminAccessAttribute>().Should().NotBeNull();
        t.GetCustomAttribute<RequireBdeActiveAttribute>().Should().BeNull("Entscheidung 1: unabhaengig von BdeAktiv");
        var posts = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null).ToList();
        posts.Select(m => m.Name).Should().BeEquivalentTo(new[] { "Add", "Update", "Delete" });
        posts.Should().OnlyContain(m => m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() != null);
    }

    [Fact]
    public async Task Index_Spaltenfilter_Offset_UndRelevanz_WieAngezeigt()
    {
        Seed(("B", -1, true), ("BG", 2, true), ("ZB", 0, false));

        var plus = (OperationConfigIndexViewModel)((ViewResult)await Ctrl("?colf_offset=%2B2").Index()).Model!;
        plus.Configs.Select(c => c.OperationName).Should().Equal("BG");

        var nein = (OperationConfigIndexViewModel)((ViewResult)await Ctrl("?colf_relevant=nein").Index()).Model!;
        nein.Configs.Select(c => c.OperationName).Should().Equal("ZB");
        nein.Pagination.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Index_MeldetDubletten_UndListetNichtKonfigurierte()
    {
        Seed(("KA", 1, true), ("ka ", 2, true)); // InMemory erzwingt den Unique-Index nicht — Altdaten-Fall
        _db.WorkOperations.Add(new WorkOperation { ProductionOrderId = 1, OperationNumber = "PG", Name = "PG", CreatedBy = "t", CreatedByWindows = "t" });
        _db.SaveChanges();

        var vm = (OperationConfigIndexViewModel)((ViewResult)await Ctrl().Index()).Model!;

        vm.DuplicateNames.Should().ContainSingle().Which.Should().Contain("KA").And.Contain("ka");
        vm.Unconfigured.Select(u => u.Kuerzel).Should().Equal("PG");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Add_LeeresKuerzel_Warnung(string? name)
    {
        var c = Ctrl();
        await c.Add(name, null, 0, true);
        c.TempData["WarningMessage"].Should().NotBeNull();
        _db.OperationConfigs.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_Getrimmt_MitAudit_DubletteCaseInsensitivAbgelehnt()
    {
        var c = Ctrl();
        await c.Add("  KA ", " Kanten ", 3, false);

        var ka = _db.OperationConfigs.Single();
        (ka.OperationName, ka.DisplayName, ka.DueDateOffsetDays, ka.IsExternalSystemRelevant).Should().Be(("KA", "Kanten", 3, false));
        (ka.CreatedBy, ka.CreatedByWindows).Should().Be(("Admin", @"DOM\admin"));
        ka.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
        c.TempData["SuccessMessage"].Should().NotBeNull();

        var dup = Ctrl();
        await dup.Add("ka", null, 1, true);
        dup.TempData["WarningMessage"].Should().BeOfType<string>().Which.Should().Contain("existiert bereits");
        _db.OperationConfigs.Should().ContainSingle();
    }

    [Fact]
    public async Task Update_SetztWerteUndModified()
    {
        Seed(("BG", 2, true));
        var id = _db.OperationConfigs.Single().Id;
        _db.ChangeTracker.Clear();

        await Ctrl().Update(id, "Biegen", 4, false);

        var bg = _db.OperationConfigs.Single();
        (bg.DisplayName, bg.DueDateOffsetDays, bg.IsExternalSystemRelevant).Should().Be(("Biegen", 4, false));
        (bg.ModifiedBy, bg.ModifiedByWindows).Should().Be(("Admin", @"DOM\admin"));
        bg.ModifiedAt.Should().NotBeNull();
        bg.CreatedBy.Should().Be("system", "Anlage-Audit bleibt");
    }

    // ===== FA 4a / AK 19 (FA-Struktur Teil 2, v1.55.0): leerer/fehlender Offset wird abgewiesen =====

    [Fact]
    public async Task Add_OhneOffset_Warnung_NichtsGespeichert()
    {
        var c = Ctrl();
        await c.Add("KA", "Kanten", null, false);

        c.TempData["WarningMessage"].Should().Be(OperationConfigsController.OffsetFehlt);
        _db.OperationConfigs.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_OhneOffset_Warnung_EintragUnveraendert()
    {
        Seed(("KA", 3, true));
        var id = _db.OperationConfigs.Single().Id;
        _db.ChangeTracker.Clear();

        var c = Ctrl();
        await c.Update(id, "Neu", null, false);

        c.TempData["WarningMessage"].Should().Be(OperationConfigsController.OffsetFehlt);
        _db.ChangeTracker.Clear();
        var ka = _db.OperationConfigs.Single();
        (ka.DueDateOffsetDays, ka.DisplayName, ka.IsExternalSystemRelevant, ka.ModifiedAt).Should().Be((3, (string?)null, true, (DateTime?)null));
    }

    [Fact]
    public async Task OffsetNullUndNegativ_BleibenGueltig()
    {
        await Ctrl().Add("KA", null, 0, true);
        var id = _db.OperationConfigs.Single().Id;
        _db.ChangeTracker.Clear();
        await Ctrl().Update(id, null, -1, true);

        _db.ChangeTracker.Clear();
        _db.OperationConfigs.Single().DueDateOffsetDays.Should().Be(-1);
    }

    [Fact]
    public async Task Update_Delete_UnbekannteId_NotFound()
    {
        (await Ctrl().Update(999, null, 0, true)).Should().BeOfType<NotFoundResult>();
        (await Ctrl().Delete(999)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_EntferntPhysisch()
    {
        Seed(("BG", 2, true));
        await Ctrl().Delete(_db.OperationConfigs.Single().Id);
        _db.OperationConfigs.Should().BeEmpty();
    }

    [Fact]
    public void AlteUrl_Settings_OperationConfig_302_PostsEntfallen()
    {
        var settings = typeof(SettingsController);
        var result = (RedirectToActionResult)settings.GetMethod("OperationConfig")!
            .Invoke(new SettingsController(null!, null!, null!, null!, null!, null!, null!, null!, null!), null)!;
        (result.ControllerName, result.ActionName, result.Permanent).Should().Be(("OperationConfigs", "Index", false));
        settings.GetMethods().Select(m => m.Name).Should().NotContain(new[] { "AddOperationConfig", "UpdateOperationConfig", "DeleteOperationConfig" });
    }

    // ===== Arbeitsgang-Abgleich: Quelle + Kennzeichnung „nicht in Sage" (Spec 2026-10-16, v1.61.0) =====

    private void SeedSource(string name, string source, DateTime? missing)
    {
        _db.OperationConfigs.Add(new OperationConfig
        {
            OperationName = name, Source = source, SageMissingSince = missing,
            CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x"
        });
        _db.SaveChanges();
    }

    [Theory]
    [InlineData("true", new[] { "ZZ", "SA" })]
    [InlineData("false", new string[0])]
    [InlineData(null, new string[0])]
    public async Task Index_NichtInSage_NurBeiSchalterAn_NieFremdsystem(string? schalter, string[] erwartet)
    {
        var t = new DateTime(2026, 10, 16);
        SeedSource("ZZ", OperationConfigSource.Manual, t);
        SeedSource("SA", OperationConfigSource.Sage, t);
        SeedSource("BR", OperationConfigSource.ExternalSystem, t); // Zeitstempel per SQL gesetzt -> trotzdem kein Badge (AK 24)
        SeedSource("KA", OperationConfigSource.Sage, null);
        _settings.Setup(s => s.GetValueAsync("Sync:OperationConfigSyncEnabled")).ReturnsAsync(schalter);

        var vm = (OperationConfigIndexViewModel)((ViewResult)await Ctrl().Index()).Model!;

        vm.MarkerActive.Should().Be(schalter == "true");
        vm.NotInSage.Select(c => c.OperationName).Should().BeEquivalentTo(erwartet);
    }

    [Fact]
    public async Task Add_SetztQuelleManuell()
    {
        await Ctrl().Add("ZZ", null, 0, true);

        _db.OperationConfigs.Single().Source.Should().Be(OperationConfigSource.Manual);
    }

    [Fact]
    public async Task Update_LaesstQuelleUndZeitstempelUnveraendert()
    {
        var t = new DateTime(2026, 10, 16, 8, 0, 0);
        SeedSource("KA", OperationConfigSource.Sage, t);
        var id = _db.OperationConfigs.Single().Id;
        _db.ChangeTracker.Clear();

        await Ctrl().Update(id, "Kanten", 2, false);

        var ka = _db.OperationConfigs.Single();
        (ka.Source, ka.SageMissingSince, ka.DueDateOffsetDays).Should().Be((OperationConfigSource.Sage, t, 2));
    }

    [Fact]
    public async Task Index_Spaltenfilter_Quelle_WieAngezeigt()
    {
        SeedSource("SA", OperationConfigSource.Sage, null);
        SeedSource("MA", OperationConfigSource.Manual, null);
        SeedSource("FR", OperationConfigSource.ExternalSystem, null);

        var manuell = (OperationConfigIndexViewModel)((ViewResult)await Ctrl("?colf_source=%3DManuell").Index()).Model!;
        manuell.Configs.Select(c => c.OperationName).Should().Equal("MA");
        var ohneFremd = (OperationConfigIndexViewModel)((ViewResult)await Ctrl("?colf_source=!Fremdsystem").Index()).Model!;
        ohneFremd.Configs.Select(c => c.OperationName).Should().BeEquivalentTo("SA", "MA");
    }

    [Fact]
    public void FormatSource_DreiWerte_UnbekanntRoh()
    {
        new[] { "Sage", "ExternalSystem", "Manual", "Xyz" }.Select(s => OperationConfigsController.FormatSource(s))
            .Should().Equal("Sage", "Fremdsystem", "Manuell", "Xyz");
        OperationConfigsController.ColumnMap["source"](new OperationConfig { Source = OperationConfigSource.ExternalSystem })
            .Should().Be("Fremdsystem");
    }

    [Fact]
    public void FormatOffset_MitVorzeichen()
        => new[] { -1, 0, 2 }.Select(OperationConfigsController.FormatOffset).Should().Equal("-1", "0", "+2");

    [Fact]
    public void Begriffe_KeinOseonRelevantMehrInViews_AK7()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        var views = Path.Combine(dir!.FullName, "IdealAkeWms", "Views");
        var treffer = Directory.GetFiles(views, "*.cshtml", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("Changelog.cshtml"))
            .Where(f => File.ReadAllText(f) is var t && (t.Contains("OSEON-relevant") || t.Contains("OSEON-Relevanz")))
            .Select(Path.GetFileName).ToList();
        treffer.Should().BeEmpty("Spalte/Schalter heissen jetzt „Fremdsystem-relevant“");
    }

    // ===== Arbeitsgang-Mapping: Sage-Arbeitsgangnummer (Spec 2026-11-16, v1.99.27) =====

    private int SeedNummer(string name, string? sage, string? hand)
    {
        var c = new OperationConfig
        {
            OperationName = name, SageArbeitsgangnummer = sage, SageArbeitsgangnummerManuell = hand,
            CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x"
        };
        _db.OperationConfigs.Add(c);
        _db.SaveChanges();
        return c.Id;
    }

    [Fact]
    public async Task Update_Handwert_GetrimmtGespeichert_LeerLoescht_SageWertUnberuehrt()
    {
        var id = SeedNummer("EG", "140", null);

        await Ctrl().Update(id, null, 0, true, " 141 ");
        _db.ChangeTracker.Clear();
        var c = _db.OperationConfigs.Single();
        (c.SageArbeitsgangnummerManuell, c.SageArbeitsgangnummer, c.WirksameSageArbeitsgangnummer).Should().Be(("141", "140", "141"));
        c.SageArbeitsgangnummerWeichtAb.Should().BeTrue();
        c.ModifiedBy.Should().Be("Admin");

        await Ctrl().Update(id, null, 0, true, "  ");
        _db.ChangeTracker.Clear();
        c = _db.OperationConfigs.Single();
        c.SageArbeitsgangnummerManuell.Should().BeNull();
        c.WirksameSageArbeitsgangnummer.Should().Be("140");
    }

    [Fact]
    public async Task Update_Handwert32Zeichen_Warnung_NichtsGespeichert()
    {
        var id = SeedNummer("EG", "140", null);
        var c = Ctrl();

        await c.Update(id, "Neu", 5, false, new string('1', 32));

        c.TempData["WarningMessage"].Should().Be("Sage-AG (Hand) höchstens 31 Zeichen – nichts gespeichert.");
        _db.ChangeTracker.Clear();
        var row = _db.OperationConfigs.Single();
        (row.SageArbeitsgangnummerManuell, row.DisplayName, row.DueDateOffsetDays, row.ModifiedAt).Should().Be(((string?)null, (string?)null, 0, (DateTime?)null));
    }

    [Fact]
    public async Task Index_Abweichung_Spaltenfilter_WirksamerWert_PrueflisteNurBeiMaster()
    {
        SeedNummer("EG", "140", "141");
        SeedNummer("EK", "142", null);
        SeedNummer("KA", "150", "150");

        var vm = (OperationConfigIndexViewModel)((ViewResult)await Ctrl().Index()).Model!;
        vm.Abweichungen.Select(c => c.OperationName).Should().Equal("EG");
        vm.Pruefliste.Should().BeNull("Master ProduktionsauftragHierarchisch nicht gesetzt (AKE)");

        var gefiltert = (OperationConfigIndexViewModel)((ViewResult)await Ctrl("?colf_sageag=14").Index()).Model!;
        gefiltert.Configs.Select(c => c.OperationName).Should().BeEquivalentTo("EG", "EK");

        _settings.Setup(s => s.GetValueAsync("ProduktionsauftragHierarchisch")).ReturnsAsync("true");
        var ideal = (OperationConfigIndexViewModel)((ViewResult)await Ctrl().Index()).Model!;
        ideal.Pruefliste.Should().NotBeNull();
    }

    private static ProductionWorkplace Werkbank(int id, string name, string? nummer, string? code, bool bde = false) => new()
    {
        Id = id, Name = name, SageArbeitsplatznummer = nummer, ArbeitsschrittCode = code, BdeAktiv = bde,
        CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x",
    };

    [Fact]
    public void Pruefliste_Tokenize_Ausschluss_OhneEintragUndOhneNummer_B_B2()
    {
        var configs = new List<OperationConfig>
        {
            new() { Id = 1, OperationName = "PU", SageArbeitsgangnummer = "180" },
            new() { Id = 2, OperationName = "VL", SageArbeitsgangnummerManuell = "260" },
            new() { Id = 3, OperationName = "MK" },                      // ohne Sage-AG
            new() { Id = 4, OperationName = "PL", SageArbeitsgangnummer = "320" },
        };
        var werkbaenke = new List<ProductionWorkplace>
        {
            Werkbank(1, "Probelauf", "320", "PL", bde: true),             // (b2): 320 = Sage-AG von PL
            Werkbank(2, "Punkten Ladenbau", "2550", "PL"),                // echter Arbeitsplatz
            Werkbank(3, "as", null, null),                                // (b)
        };
        var ausschluss = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "STO" };

        var p = OperationConfigsController.BuildPruefliste(
            new[] { "PU ISO VL MK", "  iso  STO", null }, configs, werkbaenke, ausschluss);

        p.KuerzelOhneSageAg.Should().BeEquivalentTo(new[]
        {
            new KuerzelOhneSageAg("PU ISO VL MK".Split(' ')[1], "nicht in Arbeitsgang-Stammdaten", 2),
            new KuerzelOhneSageAg("MK", "ohne Sage-AG", 1),
        }, o => o.WithStrictOrdering());
        p.KuerzelOhneSageAgGesamt.Should().Be(2);
        p.WerkbaenkeOhneArbeitsplatz.Select(w => w.Name).Should().Equal("as");
        p.WerkbankNummerGleichAg.Should().ContainSingle().Which.Should().Match<WerkbankNummerGleichAg>(x => x.Werkbank.Name == "Probelauf" && x.Arbeitsgaenge == "PL");
    }

    [Fact]
    public void Pruefliste_KappungSichtbar()
    {
        var tokens = string.Join(' ', Enumerable.Range(1, OperationConfigsController.PrueflisteMax + 3).Select(i => "K" + i));
        var p = OperationConfigsController.BuildPruefliste(new[] { tokens }, new List<OperationConfig>(), new List<ProductionWorkplace>(),
            new HashSet<string>());
        p.KuerzelOhneSageAg.Should().HaveCount(OperationConfigsController.PrueflisteMax);
        p.KuerzelOhneSageAgGesamt.Should().Be(OperationConfigsController.PrueflisteMax + 3);
    }
}
