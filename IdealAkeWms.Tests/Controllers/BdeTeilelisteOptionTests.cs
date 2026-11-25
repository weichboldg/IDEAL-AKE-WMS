using FluentAssertions;
using IdealAkeWms.Controllers;
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

    // v1.99.76 (E4-7, AK 28): Standardsicht „Teile“ nur mit verfuegbarer Teile-Sicht; gueltiger Wechsel setzt Modified*.
    [Fact]
    public async Task Edit_StandardsichtTeile_MitTeileliste_GespeichertUndModifiedGesetzt()
    {
        var ctx = TestDbContextFactory.Create();
        var wp = Wp(); ctx.ProductionWorkplaces.Add(wp); await ctx.SaveChangesAsync();

        var result = await ProductionWorkplacesControllerTests.CreateController(ctx, master: true).Edit(wp.Id,
            new ProductionWorkplaceEditViewModel { Id = wp.Id, Name = wp.Name, BdeAktiv = true, ArbeitsschrittCode = "KA", BdeTeileliste = true, BdeStandardsichtTeile = true });

        result.Should().BeOfType<RedirectToActionResult>();
        ctx.ChangeTracker.Clear();
        var saved = await ctx.ProductionWorkplaces.SingleAsync();
        saved.BdeStandardsichtTeile.Should().BeTrue();
        saved.ModifiedAt.Should().NotBeNull();
        saved.ModifiedBy.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(true, true, "KA", false)]  // BdeTeileliste aus (auch: Ausschalten bei stehender Standardsicht Teile)
    [InlineData(true, true, "  ", false)]  // ohne Kuerzel
    [InlineData(true, false, "KA", true)]  // BDE aus
    [InlineData(false, true, "KA", false)] // AKE: manipulierter POST ohne Master
    public async Task Edit_StandardsichtTeile_OhneTeileSicht_Abgewiesen(bool master, bool bdeAktiv, string code, bool teileliste)
    {
        var ctx = TestDbContextFactory.Create();
        var wp = Wp(); wp.BdeTeileliste = true; wp.BdeStandardsichtTeile = true; ctx.ProductionWorkplaces.Add(wp); await ctx.SaveChangesAsync();
        var controller = ProductionWorkplacesControllerTests.CreateController(ctx, master);

        var result = await controller.Edit(wp.Id, new ProductionWorkplaceEditViewModel
            { Id = wp.Id, Name = "Geaendert", BdeAktiv = bdeAktiv, ArbeitsschrittCode = code, BdeTeileliste = teileliste, BdeStandardsichtTeile = true });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.ContainsKey(nameof(ProductionWorkplaceEditViewModel.BdeStandardsichtTeile)).Should().BeTrue();
        ctx.ChangeTracker.Clear();
        (await ctx.ProductionWorkplaces.SingleAsync()).Name.Should().Be("Kanten");
    }

    // v1.99.76 (AK 29): Sicht-Aufloesung – ohne Angabe Standardsicht, Teile-Reiter ohne Teile-Sicht → FA.
    [Theory]
    [InlineData(null, true, true, "einzeln")]
    [InlineData(null, true, false, "fa")]
    [InlineData("summiert", true, false, "summiert")]
    [InlineData("einzeln", false, true, "fa")]   // Master aus: gespeicherte Teile-Sicht faellt auf FA
    [InlineData("fa", true, true, "fa")]
    [InlineData("quatsch", true, true, "einzeln")]
    public void SichtAufloesen(string? sicht, bool master, bool standardTeile, string erwartet)
    {
        var w = new ProductionWorkplace { BdeAktiv = true, BdeTeileliste = true, ArbeitsschrittCode = "KA", BdeStandardsichtTeile = standardTeile };
        BdeTeilelisteController.SichtAufloesen(sicht, w, master).Should().Be(erwartet);
    }

    [Fact]
    public void SichtAufloesen_OhneKuerzel_ImmerFa() =>
        BdeTeilelisteController.SichtAufloesen("einzeln", new ProductionWorkplace { BdeAktiv = true, BdeTeileliste = true }, true).Should().Be("fa");

    [Fact]
    public void AnstempelnText_Kollision_NenntWerkerUndNichtsGebucht()
    {
        var r = BdeBookingResult.Collision(new BdeBooking
        {
            BdeOperator = new BdeOperator { FirstName = "Anna", LastName = "Berg" }, ProductionWorkplace = new ProductionWorkplace { Name = "Kanten" },
            StartedAt = new DateTime(2026, 11, 25, 7, 5, 0)
        });
        BdeTeilelisteController.AnstempelnText(r, ruesten: false).Should().Be("AG ist bereits in Arbeit durch Anna Berg an Kanten seit 07:05 – nichts gebucht.");
        BdeTeilelisteController.AnstempelnText(BdeBookingResult.Invalid("Gesperrt."), true).Should().Be("Gesperrt. – nichts gebucht.");
    }

    // v1.99.76 (Spec 2026-11-25-bde-masken-vereinheitlichung E4-7, Antworten 2/13/15): Wirksamkeit an EINEM Ort, ohne NurFA-Parameter (G5).
    [Theory]
    [InlineData(true, true, "KA", true)]
    [InlineData(false, true, "KA", false)]
    [InlineData(true, false, "KA", false)]
    [InlineData(true, true, " ", false)]
    [InlineData(true, true, null, false)]
    public void TeilelisteWirksam(bool bde, bool option, string? code, bool erwartet)
    {
        var w = new ProductionWorkplace { BdeAktiv = bde, BdeTeileliste = option, ArbeitsschrittCode = code };
        WerkbankKuerzelFamilie.TeilelisteWirksam(w).Should().Be(erwartet);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void SeiteVerfuegbar_NurBdeAktiv_OhneKuerzelUndMaster(bool bde, bool erwartet) =>
        WerkbankKuerzelFamilie.SeiteVerfuegbar(new ProductionWorkplace { BdeAktiv = bde }).Should().Be(erwartet);

    [Theory]
    [InlineData(true, true, true, "KA", true)]
    [InlineData(false, true, true, "KA", false)]  // Master aus (AKE): nie Teile-Sicht
    [InlineData(true, true, false, "KA", false)]  // Kuerzel, aber BdeTeileliste aus (Antwort 15): nur FA
    [InlineData(true, true, true, null, false)]
    [InlineData(true, false, true, "KA", false)]
    public void TeileSichtVerfuegbar(bool hierarchisch, bool bde, bool option, string? code, bool erwartet)
    {
        var w = new ProductionWorkplace { BdeAktiv = bde, BdeTeileliste = option, ArbeitsschrittCode = code };
        WerkbankKuerzelFamilie.TeileSichtVerfuegbar(w, hierarchisch).Should().Be(erwartet);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]   // Standardsicht FA
    [InlineData(false, true, false)]   // Standardsicht Teile ohne verfuegbare Teile-Sicht faellt auf FA
    public void TeileStandard(bool hierarchisch, bool standardTeile, bool erwartet)
    {
        var w = new ProductionWorkplace { BdeAktiv = true, BdeTeileliste = true, ArbeitsschrittCode = "KA", BdeStandardsichtTeile = standardTeile };
        WerkbankKuerzelFamilie.TeileStandard(w, hierarchisch).Should().Be(erwartet);
    }
}
