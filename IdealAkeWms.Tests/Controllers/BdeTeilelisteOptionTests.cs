using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.32 (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung E1, AK 2): Werkbank-Option BDE-Teileliste.</summary>
public class BdeTeilelisteOptionTests
{
    private static ProductionWorkplace Wp(string? code = "KA", string? sageNr = null) => new()
    {
        Name = "Kanten", ArbeitsschrittCode = code, SageArbeitsplatznummer = sageNr, BdeAktiv = true,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
    };

    [Fact]
    public async Task Edit_MasterAn_BdeAktiv_MitKuerzel_SpeichertOption()
    {
        var ctx = TestDbContextFactory.Create();
        var wp = Wp(); ctx.ProductionWorkplaces.Add(wp); await ctx.SaveChangesAsync();

        var result = await ProductionWorkplacesControllerTests.CreateController(ctx, master: true).Edit(wp.Id,
            new ProductionWorkplaceEditViewModel { Id = wp.Id, Name = wp.Name, BdeAktiv = true, ArbeitsschrittCode = "KA", BdeTeileliste = true });

        result.Should().BeOfType<RedirectToActionResult>();
        ctx.ChangeTracker.Clear();
        (await ctx.ProductionWorkplaces.SingleAsync()).BdeTeileliste.Should().BeTrue();
    }

    [Theory]
    [InlineData(false, true, "KA")]   // Master aus (AKE, manipulierter POST)
    [InlineData(true, false, "KA")]   // BDE aus
    [InlineData(true, true, "  ")]    // Kuerzel leer
    public async Task Edit_Ungueltig_FehlerUndNichtsGespeichert(bool master, bool bdeAktiv, string code)
    {
        var ctx = TestDbContextFactory.Create();
        var wp = Wp(); ctx.ProductionWorkplaces.Add(wp); await ctx.SaveChangesAsync();
        var controller = ProductionWorkplacesControllerTests.CreateController(ctx, master);

        var result = await controller.Edit(wp.Id,
            new ProductionWorkplaceEditViewModel { Id = wp.Id, Name = "Geaendert", BdeAktiv = bdeAktiv, ArbeitsschrittCode = code, BdeTeileliste = true });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
        ctx.ChangeTracker.Clear();
        var saved = await ctx.ProductionWorkplaces.SingleAsync();
        saved.BdeTeileliste.Should().BeFalse();
        saved.Name.Should().Be("Kanten");
    }

    [Fact]
    public async Task Edit_SageWerkbank_KuerzelAusDb_ZaehltNichtDasFormular()
    {
        var ctx = TestDbContextFactory.Create();
        var wp = Wp("KA", "140"); ctx.ProductionWorkplaces.Add(wp); await ctx.SaveChangesAsync();

        // Formular-Kuerzel leer (readonly-Feld manipuliert) – entscheidend ist das gespeicherte Sage-Kuerzel
        var result = await ProductionWorkplacesControllerTests.CreateController(ctx, master: true).Edit(wp.Id,
            new ProductionWorkplaceEditViewModel { Id = wp.Id, Name = wp.Name, BdeAktiv = true, ArbeitsschrittCode = null, BdeTeileliste = true });

        result.Should().BeOfType<RedirectToActionResult>();
        ctx.ChangeTracker.Clear();
        (await ctx.ProductionWorkplaces.SingleAsync()).BdeTeileliste.Should().BeTrue();
    }

    [Fact]
    public async Task Create_MasterAus_OptionAbgewiesen()
    {
        var ctx = TestDbContextFactory.Create();
        var result = await ProductionWorkplacesControllerTests.CreateController(ctx).Create(
            new ProductionWorkplaceEditViewModel { Name = "Neu", BdeAktiv = true, ArbeitsschrittCode = "KA", BdeTeileliste = true });

        result.Should().BeOfType<ViewResult>();
        (await ctx.ProductionWorkplaces.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(true, true, false, "KA", true)]
    [InlineData(false, true, false, "KA", false)]
    [InlineData(true, false, false, "KA", false)]
    [InlineData(true, true, true, "KA", false)]    // BdeNurFaMeldung global gewinnt
    [InlineData(true, true, false, " ", false)]
    [InlineData(true, true, false, null, false)]
    public void TeilelisteWirksam(bool bde, bool option, bool nurFa, string? code, bool erwartet)
    {
        var w = new ProductionWorkplace { BdeAktiv = bde, BdeTeileliste = option, ArbeitsschrittCode = code };
        WerkbankKuerzelFamilie.TeilelisteWirksam(w, nurFa).Should().Be(erwartet);
    }
}
