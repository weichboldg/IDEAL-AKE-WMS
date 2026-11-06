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

/// <summary>Debitoren-Pflege (v1.73.0, E8b), Muster <see cref="CreditorsControllerTests"/>: Read/Edit-Split, Schreibschutz der
/// Sage-Felder, Duplikat-Guards, Audit, Liste/Filter. Index-DDL und Razor-Ausgabe sind Manual-UAT (TS-106.11).</summary>
public class DebitorsControllerTests
{
    private static DebitorsController Create(ApplicationDbContext ctx, string? query = null)
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(x => x.GetDisplayName()).Returns("Pfleger");
        user.Setup(x => x.GetWindowsUserName()).Returns(@"AKE\pfleger");
        user.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var ctrl = new DebitorsController(new DebitorRepository(ctx), user.Object);
        var http = new DefaultHttpContext();
        if (query != null) http.Request.QueryString = new QueryString(query);
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    private static Debitor Seed(ApplicationDbContext ctx, string? shortName, string name = "Kunde", string source = StorageLocationSource.Manual,
        bool active = true, string? kto = null)
    {
        var d = new Debitor
        {
            ShortName = shortName, Name = name, Source = source, IsActive = active, SageKto = kto,
            CreatedBy = "x", CreatedByWindows = "x", CreatedAt = new DateTime(2026, 1, 1)
        };
        ctx.Debitors.Add(d);
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();
        return d;
    }

    [Fact]
    public void ReadEditSplit_KlasseLesen_SchreibendeActionsBearbeiten()
    {
        typeof(DebitorsController).GetCustomAttribute<RequireMasterDataReadAccessAttribute>().Should().NotBeNull();
        var schreibend = typeof(DebitorsController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name is nameof(DebitorsController.Create) or nameof(DebitorsController.Edit) or nameof(DebitorsController.SetActive))
            .ToList();
        schreibend.Should().HaveCount(5, "Create GET/POST, Edit GET/POST, SetActive POST");
        schreibend.Should().OnlyContain(m => m.GetCustomAttribute<RequireMasterDataAccessAttribute>() != null);
        typeof(DebitorsController).GetMethod(nameof(DebitorsController.Index))!.GetCustomAttribute<RequireMasterDataAccessAttribute>().Should().BeNull();
        typeof(DebitorsController).GetMethod(nameof(DebitorsController.Details))!.GetCustomAttribute<RequireMasterDataAccessAttribute>().Should().BeNull();
        typeof(DebitorsController).GetMethods().Should().NotContain(m => m.Name.Contains("Delete"), "nur deaktivieren, kein Loeschen");
    }

    [Fact]
    public async Task Create_SetztAuditQuelleWmsUndTrimmt_SageFelderNieAusFormular()
    {
        var ctx = TestDbContextFactory.Create();
        var ctrl = Create(ctx);
        var result = await ctrl.Create(new Debitor
        {
            ShortName = "  Hofer  ", Name = " Hofer & Co 'KG' <AT> ", SageKto = " D10001 ", Source = StorageLocationSource.Sage,
            SageVertreter = "HACK", SageKundenNameFA = "HACK", IsActive = false
        });

        result.Should().BeOfType<RedirectToActionResult>();
        var d = ctx.Debitors.Single();
        (d.ShortName, d.Name, d.SageKto).Should().Be(("Hofer", "Hofer & Co 'KG' <AT>", "D10001"));
        d.Source.Should().Be(StorageLocationSource.Manual);
        (d.SageVertreter, d.SageKundenNameFA).Should().Be(((string?)null, (string?)null), "Sage-Felder ausser dem Konto nie aus dem Formular");
        d.IsActive.Should().BeTrue();
        d.SageUebergabeStatus.Should().Be(SageUebergabeStatus.NichtNoetig);
        (d.CreatedBy, d.CreatedByWindows).Should().Be(("Pfleger", @"AKE\pfleger"));
    }

    [Fact]
    public async Task Create_OhneKurzname_DoppelterKurzname_DoppeltesKonto_Feldfehler()
    {
        var ctx = TestDbContextFactory.Create();
        Seed(ctx, "Hofer", kto: "D1");

        var c1 = Create(ctx);
        (await c1.Create(new Debitor { ShortName = "  ", Name = "X" })).Should().BeOfType<ViewResult>();
        c1.ModelState[nameof(Debitor.ShortName)]!.Errors.Should().NotBeEmpty();

        var c2 = Create(ctx);
        (await c2.Create(new Debitor { ShortName = " HOFER ", Name = "X" })).Should().BeOfType<ViewResult>();
        c2.ModelState[nameof(Debitor.ShortName)]!.Errors.Should().NotBeEmpty();

        var c3 = Create(ctx);
        (await c3.Create(new Debitor { ShortName = "Neu", Name = "X", SageKto = " D1 " })).Should().BeOfType<ViewResult>();
        c3.ModelState[nameof(Debitor.SageKto)]!.Errors.Should().NotBeEmpty();

        ctx.Debitors.Should().HaveCount(1);
    }

    [Fact]
    public async Task Edit_Manuell_UebernimmtFelder_SetztModified()
    {
        var ctx = TestDbContextFactory.Create();
        var seeded = Seed(ctx, "Hofer");

        var result = await Create(ctx).Edit(seeded.Id, new Debitor { ShortName = "hofer", Name = "Neu", City = "Linz", SageKto = "D9", Remark = "Bem" });

        result.Should().BeOfType<RedirectToActionResult>();
        var d = ctx.Debitors.Single();
        (d.ShortName, d.Name, d.City, d.SageKto, d.Remark).Should().Be(("hofer", "Neu", "Linz", "D9", "Bem"));
        (d.ModifiedBy, d.ModifiedByWindows).Should().Be(("Pfleger", @"AKE\pfleger"));
        d.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Edit_SageHerkunft_SageFelderSchreibgeschuetzt_KurznameDarfLeerSein()
    {
        var ctx = TestDbContextFactory.Create();
        var seeded = Seed(ctx, null, name: "Sage-Kunde", source: StorageLocationSource.Sage, kto: "D31013");
        var ctrl = Create(ctx);
        ctrl.ModelState.AddModelError(nameof(Debitor.Email), "ungueltig");
        ctrl.ModelState.AddModelError(nameof(Debitor.Name), "Pflicht");

        var result = await ctrl.Edit(seeded.Id, new Debitor
        {
            Name = "", Email = "kaputt", City = "HACK", SageKto = "HACK", Source = StorageLocationSource.Manual, ShortName = null, Remark = "Hinweis"
        });

        result.Should().BeOfType<RedirectToActionResult>();
        var d = ctx.Debitors.Single();
        (d.Name, d.City, d.Email, d.SageKto, d.Source).Should().Be(("Sage-Kunde", null, null, "D31013", StorageLocationSource.Sage));
        (d.ShortName, d.Remark).Should().Be((null, "Hinweis"));
    }

    [Fact]
    public async Task Edit_UnbekannteId_NotFound() =>
        (await Create(TestDbContextFactory.Create()).Edit(42, new Debitor { Name = "x", ShortName = "x" })).Should().BeOfType<NotFoundResult>();

    [Fact]
    public async Task SetActive_DeaktiviertUndReaktiviert_MitModified()
    {
        var ctx = TestDbContextFactory.Create();
        var seeded = Seed(ctx, "Hofer");
        var ctrl = Create(ctx);

        await ctrl.SetActive(seeded.Id, false);
        var d = ctx.Debitors.Single();
        d.IsActive.Should().BeFalse();
        (d.ModifiedBy, d.ModifiedByWindows).Should().Be(("Pfleger", @"AKE\pfleger"));

        await ctrl.SetActive(seeded.Id, true);
        ctx.Debitors.Single().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Index_StatusDefaultAktiv_InaktivAlle()
    {
        var ctx = TestDbContextFactory.Create();
        Seed(ctx, "A");
        Seed(ctx, "B", active: false);

        async Task<List<string?>> Namen(string? status) =>
            ((List<Debitor>)((ViewResult)await Create(ctx).Index(status)).Model!).Select(d => d.ShortName).ToList();

        (await Namen(null)).Should().Equal("A");
        (await Namen("unsinn")).Should().Equal("A");
        (await Namen(CreditorsController.StatusInaktiv)).Should().Equal("B");
        (await Namen(CreditorsController.StatusAlle)).Should().Equal("A", "B");
    }

    [Fact]
    public async Task Index_Spaltenfilter_WirktVorPagination_TotalCountGefiltert()
    {
        var ctx = TestDbContextFactory.Create();
        for (var i = 0; i < 30; i++) Seed(ctx, $"D{i:00}", name: i % 2 == 0 ? $"Hofer {i}" : $"Billa {i}");
        var ctrl = Create(ctx, "?colf_name=hofer");

        var v = (ViewResult)await ctrl.Index(page: 1);

        ((List<Debitor>)v.Model!).Should().HaveCount(15).And.OnlyContain(d => d.Name.StartsWith("Hofer"));
        ((IdealAkeWms.Models.ViewModels.PaginationState)ctrl.ViewBag.Pagination).TotalCount.Should().Be(15);
    }

    [Fact]
    public void ColumnMap_StatusUndHerkunft_WieGerendert()
    {
        var d = new Debitor { IsActive = false, Source = StorageLocationSource.Sage, PostalCode = "4020", City = "Linz" };
        DebitorsController.ColumnMap["status"](d).Should().Be("Inaktiv");
        DebitorsController.ColumnMap["source"](d).Should().Be("Sage");
        DebitorsController.ColumnMap["city"](d).Should().Be("4020 Linz");
        DebitorsController.ColumnMap["source"](new Debitor()).Should().Be("WMS");
    }
}
