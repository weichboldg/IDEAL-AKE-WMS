using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;
using Sub = IdealAkeWms.Services.LeitstandHauptFaZusammenfassung.SubFaZeile;
using S = IdealAkeWms.Models.FaWorkStepStatus;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.88 (Spec 2026-11-26 Leitstand nur HauptFA, Teil 2): Regel Z1-Z5, SubFaBestimmt (Antwort 8), LackNurSubFa (9b), W4 (AK 12/17).</summary>
public class LeitstandHauptFaZusammenfassungTests
{
    private static ProductionOrderPickingStatus Ps(bool glas = false, bool zukauf = false, bool lack = false, bool lackErledigt = false) =>
        new() { HasGlass = glas, HasExternalPurchase = zukauf, HasCoatingParts = lack, IsCoatingDone = lackErledigt };

    private static Sub SubZ(ProductionOrderPickingStatus? ps = null, params (string Code, S Status)[] ag) =>
        new(ps, ag.ToDictionary(a => a.Code, a => new FaWorkStepPivotCell(99, a.Status)));

    private static PickingLeitstandItem Haupt(bool glas = false, bool zukauf = false, bool lack = false, bool lackErledigt = false,
        params (string Code, S Status)[] ag) => new()
    {
        Id = 1, HasGlass = glas, HasExternalPurchase = zukauf, HasCoatingParts = lack, IsCoatingDone = lackErledigt,
        WorkSteps = ag.ToDictionary(a => a.Code, a => new FaWorkStepPivotCell(10, a.Status))
    };

    private static PickingLeitstandItem Run(PickingLeitstandItem h, params Sub[] subs)
    {
        LeitstandHauptFaZusammenfassung.Anwenden(h, subs);
        return h;
    }

    [Fact]
    public void NurHauptFa_EigeneWerte_KeinSubFaBestimmt()
    {
        var h = Run(Haupt(glas: true, lack: true, lackErledigt: true, ag: ("VK", S.Fertig)));
        h.HasGlass.Should().BeTrue();
        h.IsCoatingDone.Should().BeTrue();
        h.WorkSteps["VK"].Should().Be(new FaWorkStepPivotCell(10, S.Fertig));
        h.SubFaBestimmt.Should().BeEmpty();
        h.SubFaAnzahl.Should().Be(0);
        h.LackNurSubFa.Should().BeFalse();
    }

    [Fact]
    public void A_LackNurAnSubFa_HauptZeigtLackT_W3Gesperrt()
    {
        var h = Run(Haupt(), SubZ(Ps(lack: true)), SubZ(Ps()));
        h.HasCoatingParts.Should().BeTrue();
        h.IsCoatingDone.Should().BeFalse();
        h.LackNurSubFa.Should().BeTrue();
        h.SubFaBestimmt.Should().NotContain(LeitstandHauptFaZusammenfassung.LackErledigt);
        h.SubFaAnzahl.Should().Be(2);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void B_LackErledigt_AlleZeilenMitLackteilen(bool s1, bool s2, bool erwartet)
        => Run(Haupt(), SubZ(Ps(lack: true, lackErledigt: s1)), SubZ(Ps(lack: true, lackErledigt: s2))).IsCoatingDone.Should().Be(erwartet);

    [Fact]
    public void W3_Aus_SobaldHauptEigeneLackteileHat()
        => Run(Haupt(lack: true), SubZ(Ps(lack: true))).LackNurSubFa.Should().BeFalse();

    [Fact]
    public void W2_HauptErledigt_SubOffen_LeerUndSubFaBestimmt()
    {
        var h = Run(Haupt(lack: true, lackErledigt: true), SubZ(Ps(lack: true)));
        h.IsCoatingDone.Should().BeFalse();
        h.SubFaBestimmt.Should().Contain(LeitstandHauptFaZusammenfassung.LackErledigt);
    }

    [Fact]
    public void K1_LackSubErledigt_HauptBestimmtFrei()
    {
        var h = Run(Haupt(lack: true), SubZ(Ps(lack: true, lackErledigt: true)));
        h.IsCoatingDone.Should().BeFalse();
        h.SubFaBestimmt.Should().BeEmpty();
    }

    [Fact]
    public void C_GlasUndZukaufIrgendeine_W1SubFaBestimmt()
    {
        var h = Run(Haupt(), SubZ(Ps(glas: true)), SubZ(Ps(zukauf: true)));
        h.HasGlass.Should().BeTrue();
        h.HasExternalPurchase.Should().BeTrue();
        h.SubFaBestimmt.Should().Contain(new[] { LeitstandHauptFaZusammenfassung.Glas, LeitstandHauptFaZusammenfassung.Zukauf });
    }

    [Fact]
    public void K2_KeineSubFaMitGlas_NichtBestimmt()
    {
        var h = Run(Haupt(glas: true), SubZ(Ps()));
        h.HasGlass.Should().BeTrue();
        h.SubFaBestimmt.Should().BeEmpty();
    }

    [Theory]
    [InlineData(S.Fertig, S.Offen, S.InBearbeitung)] // Antwort 6: gemischt = In Bearbeitung
    [InlineData(S.InBearbeitung, S.Offen, S.InBearbeitung)]
    [InlineData(S.Fertig, S.Fertig, S.Fertig)]
    [InlineData(S.Offen, S.Offen, S.Offen)]
    public void D_VkRegel_W4OhneHauptArbeitsgang(S s1, S s2, S erwartet)
    {
        var h = Run(Haupt(), SubZ(null, ("VK", s1)), SubZ(null, ("VK", s2)));
        h.WorkSteps["VK"].Should().Be(new FaWorkStepPivotCell(0, erwartet), "W4: kein FaWorkStep an der HauptFA-Zeile -> Id 0/gesperrt");
    }

    [Fact]
    public void D_KeineZeileMitVk_ZelleLeer()
        => Run(Haupt(), SubZ(Ps())).WorkSteps.Should().NotContainKey("VK");

    [Fact]
    public void W5_HauptFertig_SubOffen_InBearbeitung_TraegtHauptId()
    {
        var h = Run(Haupt(ag: ("VK", S.Fertig)), SubZ(null, ("VK", S.Offen)));
        h.WorkSteps["VK"].Should().Be(new FaWorkStepPivotCell(10, S.InBearbeitung));
        h.SubFaBestimmt.Should().Contain("VK");
    }

    [Fact]
    public void E_ErledigteSubFaMitVkFertigZaehltMit()
        // Der Aufrufer liefert nicht stornierte Zeilen inkl. erledigter; stornierte fehlen in subs (GetZeilenIdsJeAuftragAsync).
        => Run(Haupt(ag: ("VK", S.Offen)), SubZ(null, ("VK", S.Fertig))).WorkSteps["VK"].Status.Should().Be(S.InBearbeitung);
}
