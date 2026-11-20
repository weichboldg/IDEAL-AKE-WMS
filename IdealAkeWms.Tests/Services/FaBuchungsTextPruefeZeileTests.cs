using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.47 (Spec [[2026-08-05-wms-bugs-improvements-teil-6-spec]], AK 4/5/18–21/24): Trifft-/Gemischt-Regel je Artikelzeile.</summary>
public class FaBuchungsTextPruefeZeileTests
{
    private static readonly Dictionary<string, string> Ake = new(StringComparer.OrdinalIgnoreCase);
    // IDEAL AK 18: S1 (Ebene 1) und S2 (Ebene 2) gehoeren beide zu Haupt-FA H.
    private static readonly Dictionary<string, string> Ideal = new(StringComparer.OrdinalIgnoreCase) { ["S1"] = "H", ["S2"] = "H" };

    private static HashSet<string> F(string text) => FaBuchungsText.Nummern(text).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Paar nur aus Zugaengen mit FA-Text.</summary>
    private static FaAmPaar Zugang(params string[] nr) => new(nr.ToList(), nr.ToHashSet(StringComparer.OrdinalIgnoreCase), false);

    [Fact]
    public void Nummern_ZerlegtTrimmtUndLaesstLeereWeg() =>
        FaBuchungsText.Nummern(" A; B ,,C ;").Should().Equal("A", "B", "C");

    [Fact]
    public void Exakt_KeinTeilstring_CaseEgal_AK5()
    {
        FaBuchungsText.PruefeZeile(Zugang("1234567"), Ake, F("123")).Trifft.Should().BeFalse();
        FaBuchungsText.PruefeZeile(Zugang("ab12"), Ake, F(" AB12 ")).Trifft.Should().BeTrue();
    }

    [Fact]
    public void PaarNull_TrifftNicht()
    {
        var p = FaBuchungsText.PruefeZeile(null, Ake, F("1"));
        (p.Trifft, p.Fremd.Count, p.OhneFa, p.NurAusAusbuchung).Should().Be((false, 0, false, false));
    }

    [Fact]
    public void NurEigeneZugaenge_NichtGemischt_AK24Gegenprobe()
    {
        var p = FaBuchungsText.PruefeZeile(Zugang("1234567"), Ake, F("1234567"));
        p.Trifft.Should().BeTrue();
        FaBuchungsText.IstGemischt(p).Should().BeFalse();
    }

    [Fact]
    public void G1_FremderFa()
    {
        var p = FaBuchungsText.PruefeZeile(Zugang("111", "222"), Ake, F("111"));
        p.Fremd.Should().Equal("222");
        FaBuchungsText.IstGemischt(p).Should().BeTrue();
        // Beide eingegeben → nicht gemischt (AK 6).
        FaBuchungsText.IstGemischt(FaBuchungsText.PruefeZeile(Zugang("111", "222"), Ake, F("111; 222"))).Should().BeFalse();
    }

    [Fact]
    public void G2_ZugangOhneFa_AK24Z()
    {
        var p = FaBuchungsText.PruefeZeile(new FaAmPaar(["111"], new(["111"]), true), Ake, F("111"));
        (p.Fremd.Count, p.OhneFa, p.NurAusAusbuchung).Should().Be((0, true, false));
        FaBuchungsText.IstGemischt(p).Should().BeTrue();
    }

    [Fact]
    public void G2UndG3_WareneingangOhneFaDannAusbuchungMitFa_AK24Y()
    {
        var p = FaBuchungsText.PruefeZeile(new FaAmPaar(["111"], new(), true), Ake, F("111"));
        (p.Trifft, p.OhneFa, p.NurAusAusbuchung).Should().Be((true, true, true));
    }

    [Fact]
    public void G3_Allein()
    {
        var p = FaBuchungsText.PruefeZeile(new FaAmPaar(["111"], new(), false), Ake, F("111"));
        (p.OhneFa, p.NurAusAusbuchung).Should().Be((false, true));
        FaBuchungsText.IstGemischt(p).Should().BeTrue();
    }

    [Fact]
    public void Ideal_HauptDecktSubJederEbene_AK18()
    {
        FaBuchungsText.IstGemischt(FaBuchungsText.PruefeZeile(Zugang("S1"), Ideal, F("H"))).Should().BeFalse();
        FaBuchungsText.PruefeZeile(Zugang("S1"), Ideal, F("H")).Trifft.Should().BeTrue();
        FaBuchungsText.PruefeZeile(Zugang("S2"), Ideal, F("H")).Trifft.Should().BeTrue();
        FaBuchungsText.PruefeZeile(Zugang("H"), Ideal, F("H")).Trifft.Should().BeTrue();
        FaBuchungsText.PruefeZeile(Zugang("S1", "X"), Ideal, F("H")).Fremd.Should().Equal("X");
    }

    [Fact]
    public void Ideal_SubDecktNurSichSelbst_AK19()
    {
        FaBuchungsText.PruefeZeile(Zugang("S1"), Ideal, F("S1")).Trifft.Should().BeTrue();
        FaBuchungsText.PruefeZeile(Zugang("S2"), Ideal, F("S1")).Trifft.Should().BeFalse();
        FaBuchungsText.PruefeZeile(Zugang("H"), Ideal, F("S1")).Trifft.Should().BeFalse();
    }

    [Fact]
    public void Ake_LeeresMapping_HauptTrifftSubNicht_AK20() =>
        FaBuchungsText.PruefeZeile(Zugang("S1"), Ake, F("H")).Trifft.Should().BeFalse();

    [Fact]
    public void Ideal_OhneSubFaSatz_AK21() =>
        FaBuchungsText.PruefeZeile(Zugang("S9"), Ideal, F("H")).Trifft.Should().BeFalse();
}
