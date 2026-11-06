using System.Reflection;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Kreditoren-Pflege (v1.73.0, E8a): Read/Edit-Split, Schreibschutz der Sage-Felder, Duplikat-Guards, Audit, Liste/Filter,
/// Dienstleister-Meldung. Index-DDL und Razor-Ausgabe sind Manual-UAT (TS-106.10).</summary>
public class CreditorsControllerTests
{
    private static (CreditorsController Ctrl, ApplicationDbContext Ctx) Create(ApplicationDbContext ctx, string? query = null,
        params FaHierarchyOrderInfo[] infos)
    {
        if (infos.Length > 0)
        {
            ctx.FaHierarchyOrderInfos.AddRange(infos);
            ctx.SaveChanges();
        }
        var user = new Mock<ICurrentUserService>();
        user.Setup(x => x.GetDisplayName()).Returns("Pfleger");
        user.Setup(x => x.GetWindowsUserName()).Returns(@"AKE\pfleger");
        user.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var ctrl = new CreditorsController(new CreditorRepository(ctx), new FaHierarchyOrderInfoRepository(ctx), user.Object);
        var http = new DefaultHttpContext();
        if (query != null) http.Request.QueryString = new QueryString(query);
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return (ctrl, ctx);
    }

    private static Creditor Seed(ApplicationDbContext ctx, string? shortName, string name = "Firma", string source = StorageLocationSource.Manual,
        bool active = true, bool coater = false, string? kto = null)
    {
        var c = new Creditor
        {
            ShortName = shortName, Name = name, Source = source, IsActive = active, IsCoater = coater, SageKto = kto,
            CreatedBy = "x", CreatedByWindows = "x", CreatedAt = new DateTime(2026, 1, 1)
        };
        ctx.Creditors.Add(c);
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();
        return c;
    }

    [Fact]
    public void ReadEditSplit_KlasseLesen_SchreibendeActionsBearbeiten()
    {
        typeof(CreditorsController).GetCustomAttribute<RequireMasterDataReadAccessAttribute>().Should().NotBeNull();
        var schreibend = typeof(CreditorsController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name is nameof(CreditorsController.Create) or nameof(CreditorsController.Edit) or nameof(CreditorsController.SetActive))
            .ToList();
        schreibend.Should().HaveCount(5, "Create GET/POST, Edit GET/POST, SetActive POST");
        schreibend.Should().OnlyContain(m => m.GetCustomAttribute<RequireMasterDataAccessAttribute>() != null);
        typeof(CreditorsController).GetMethod(nameof(CreditorsController.Index))!.GetCustomAttribute<RequireMasterDataAccessAttribute>().Should().BeNull();
        typeof(CreditorsController).GetMethod(nameof(CreditorsController.Details))!.GetCustomAttribute<RequireMasterDataAccessAttribute>().Should().BeNull();
        typeof(CreditorsController).GetMethods().Should().NotContain(m => m.Name.Contains("Delete"), "nur deaktivieren, kein Loeschen");
    }

    [Fact]
    public async Task Create_SetztAuditQuelleWmsUndTrimmt()
    {
        var (ctrl, ctx) = Create(TestDbContextFactory.Create());
        var result = await ctrl.Create(new Creditor
        {
            ShortName = "  Pulver  ", Name = " Müller & Söhne 'Pulver' <GmbH> ", SageKto = " K31013 ", IsCoater = true,
            Source = StorageLocationSource.Sage, SageGruppe = "HACK", IsActive = false
        });

        result.Should().BeOfType<RedirectToActionResult>();
        var c = ctx.Creditors.Single();
        c.ShortName.Should().Be("Pulver");
        c.Name.Should().Be("Müller & Söhne 'Pulver' <GmbH>");
        c.SageKto.Should().Be("K31013");
        c.IsCoater.Should().BeTrue();
        c.Source.Should().Be(StorageLocationSource.Manual, "Anlage im WMS ist immer Herkunft WMS");
        c.SageGruppe.Should().BeNull("Sage-Felder ausser dem Konto werden nie aus dem Formular uebernommen");
        c.IsActive.Should().BeTrue();
        c.SageUebergabeStatus.Should().Be(SageUebergabeStatus.NichtNoetig);
        c.CreatedBy.Should().Be("Pfleger");
        c.CreatedByWindows.Should().Be(@"AKE\pfleger");
        c.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Create_OhneKurzname_Feldfehler()
    {
        var (ctrl, ctx) = Create(TestDbContextFactory.Create());
        var result = await ctrl.Create(new Creditor { ShortName = "   ", Name = "Firma" });

        result.Should().BeOfType<ViewResult>();
        ctrl.ModelState[nameof(Creditor.ShortName)]!.Errors.Should().NotBeEmpty();
        ctx.Creditors.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_DoppelterKurzname_AndereSchreibweise_Feldfehler()
    {
        var ctx = TestDbContextFactory.Create();
        Seed(ctx, "Pulver");
        var (ctrl, _) = Create(ctx);

        var result = await ctrl.Create(new Creditor { ShortName = " PULVER ", Name = "Andere" });

        result.Should().BeOfType<ViewResult>();
        ctrl.ModelState[nameof(Creditor.ShortName)]!.Errors.Should().NotBeEmpty();
        ctx.Creditors.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_DoppeltesSageKonto_Feldfehler_LeeresKontoMehrfachErlaubt()
    {
        var ctx = TestDbContextFactory.Create();
        Seed(ctx, "A", kto: "K1");
        Seed(ctx, "B");
        var (ctrl, _) = Create(ctx);

        (await ctrl.Create(new Creditor { ShortName = "C", Name = "C", SageKto = " K1 " })).Should().BeOfType<ViewResult>();
        ctrl.ModelState[nameof(Creditor.SageKto)]!.Errors.Should().NotBeEmpty();

        var (ctrl2, _) = Create(ctx);
        (await ctrl2.Create(new Creditor { ShortName = "D", Name = "D", SageKto = "  " })).Should().BeOfType<RedirectToActionResult>();
        ctx.Creditors.Count(c => c.SageKto == null).Should().Be(2);
    }

    [Fact]
    public async Task Edit_Manuell_UebernimmtAlleFelder_SetztModified_EigenerKurznameKeinDuplikat()
    {
        var ctx = TestDbContextFactory.Create();
        var seeded = Seed(ctx, "Pulver");
        var (ctrl, _) = Create(ctx);

        var result = await ctrl.Edit(seeded.Id, new Creditor
        {
            ShortName = "pulver", Name = "Neu", City = "Graz", SageKto = "K9", Remark = "Bem", Matchcode = "MC", IsCoater = true
        });

        result.Should().BeOfType<RedirectToActionResult>();
        var c = ctx.Creditors.Single();
        (c.ShortName, c.Name, c.City, c.SageKto, c.Remark, c.Matchcode, c.IsCoater).Should().Be(("pulver", "Neu", "Graz", "K9", "Bem", "MC", true));
        c.ModifiedBy.Should().Be("Pfleger");
        c.ModifiedByWindows.Should().Be(@"AKE\pfleger");
        c.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Edit_SageHerkunft_SageFelderSchreibgeschuetzt_WmsFelderPflegbar_KurznameDarfLeerSein()
    {
        var ctx = TestDbContextFactory.Create();
        var seeded = Seed(ctx, null, name: "Sage-Name", source: StorageLocationSource.Sage, kto: "K31013");
        var (ctrl, _) = Create(ctx);
        // Formularwerte der Sage-Felder ungueltig/manipuliert: duerfen weder speichern noch die Validierung kippen.
        ctrl.ModelState.AddModelError(nameof(Creditor.Email), "ungueltig");
        ctrl.ModelState.AddModelError(nameof(Creditor.Name), "Pflicht");

        var result = await ctrl.Edit(seeded.Id, new Creditor
        {
            Name = "", Email = "kaputt", City = "HACK", SageKto = "HACK", Source = StorageLocationSource.Manual,
            ShortName = null, Remark = "Hinweis", Matchcode = "MC", IsCoater = true
        });

        result.Should().BeOfType<RedirectToActionResult>();
        var c = ctx.Creditors.Single();
        (c.Name, c.City, c.Email, c.SageKto, c.Source).Should().Be(("Sage-Name", null, null, "K31013", StorageLocationSource.Sage));
        (c.ShortName, c.Remark, c.Matchcode, c.IsCoater).Should().Be((null, "Hinweis", "MC", true));
        c.ModifiedBy.Should().Be("Pfleger");
    }

    [Fact]
    public async Task Edit_UnbekannteId_NotFound()
    {
        var (ctrl, _) = Create(TestDbContextFactory.Create());
        (await ctrl.Edit(42, new Creditor { Name = "x", ShortName = "x" })).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SetActive_DeaktiviertUndReaktiviert_MitModified()
    {
        var ctx = TestDbContextFactory.Create();
        var seeded = Seed(ctx, "Pulver");
        var (ctrl, _) = Create(ctx);

        await ctrl.SetActive(seeded.Id, false);
        var c = ctx.Creditors.Single();
        c.IsActive.Should().BeFalse();
        c.ModifiedBy.Should().Be("Pfleger");
        c.ModifiedByWindows.Should().Be(@"AKE\pfleger");

        await ctrl.SetActive(seeded.Id, true);
        ctx.Creditors.Single().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Index_StatusDefaultAktiv_InaktivAlle_Beschichter()
    {
        var ctx = TestDbContextFactory.Create();
        Seed(ctx, "A", coater: true);
        Seed(ctx, "B");
        Seed(ctx, "C", active: false, coater: true);

        async Task<List<string?>> Namen(string? status, bool coater = false)
        {
            var (ctrl, _) = Create(ctx);
            var v = (ViewResult)await ctrl.Index(status, coater);
            return ((List<Creditor>)v.Model!).Select(c => c.ShortName).ToList();
        }

        (await Namen(null)).Should().Equal("A", "B");
        (await Namen("unsinn")).Should().Equal("A", "B");
        (await Namen(CreditorsController.StatusInaktiv)).Should().Equal("C");
        (await Namen(CreditorsController.StatusAlle)).Should().Equal("A", "B", "C");
        (await Namen(CreditorsController.StatusAlle, coater: true)).Should().Equal("A", "C");
    }

    [Fact]
    public async Task Index_Spaltenfilter_WirktVorPagination_TotalCountGefiltert()
    {
        var ctx = TestDbContextFactory.Create();
        for (var i = 0; i < 30; i++) Seed(ctx, $"K{i:00}", name: i % 2 == 0 ? $"Pulver {i}" : $"Glas {i}");
        var (ctrl, _) = Create(ctx, "?colf_name=pulver");

        var v = (ViewResult)await ctrl.Index(page: 1);

        ((List<Creditor>)v.Model!).Should().HaveCount(15).And.OnlyContain(c => c.Name.StartsWith("Pulver"));
        ((IdealAkeWms.Models.ViewModels.PaginationState)ctrl.ViewBag.Pagination).TotalCount.Should().Be(15);
    }

    [Fact]
    public async Task Index_Meldekarte_UeberAlleKreditoren_UnabhaengigVomFilter()
    {
        var ctx = TestDbContextFactory.Create();
        Seed(ctx, "Pulver", active: false);
        var (ctrl, _) = Create(ctx, null,
            new FaHierarchyOrderInfo { HauptFA = 1, Dienstleister = " pulver" },
            new FaHierarchyOrderInfo { HauptFA = 2, Dienstleister = "Glas" });

        var v = (ViewResult)await ctrl.Index();

        ((List<UnassignedServiceProvider>)ctrl.ViewBag.Unassigned).Should().Equal(
            new UnassignedServiceProvider("Glas", 1, false),
            new UnassignedServiceProvider("pulver", 1, true));
        ((List<Creditor>)v.Model!).Should().BeEmpty("Default-Filter Aktiv");
    }

    [Fact]
    public void Create_Get_BelegtKurznameVor()
    {
        var (ctrl, _) = Create(TestDbContextFactory.Create());
        var v = (ViewResult)ctrl.Create(" Pulver ");
        var c = (Creditor)v.Model!;
        c.ShortName.Should().Be("Pulver");
        c.IsCoater.Should().BeTrue("Vorbelegung kommt aus der Dienstleister-Meldung der Beschichtungsauftraege");
    }

    [Fact]
    public void ColumnMap_StatusUndHerkunft_WieGerendert()
    {
        var c = new Creditor { IsActive = false, Source = StorageLocationSource.Sage, IsCoater = true, PostalCode = "8010", City = "Graz" };
        CreditorsController.ColumnMap["status"](c).Should().Be("Inaktiv");
        CreditorsController.ColumnMap["source"](c).Should().Be("Sage");
        CreditorsController.ColumnMap["coater"](c).Should().Be("Ja");
        CreditorsController.ColumnMap["city"](c).Should().Be("8010 Graz");
        CreditorsController.ColumnMap["source"](new Creditor()).Should().Be("WMS");
    }
}
