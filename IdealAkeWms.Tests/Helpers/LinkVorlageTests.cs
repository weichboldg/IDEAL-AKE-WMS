using FluentAssertions;
using IdealAkeWms.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>Vault-/enaio-Links (v1.67.0): die EINE Regel fuer Schalter, Vorlage, leeren Wert und URL-Kodierung.</summary>
public class LinkVorlageTests
{
    private const string Vault = "http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search={Artikelnummer}&searchContext=0";
    private const string Enaio = "http://akeosapp.ake.at/oscontentviewer/viewer/{ObjektId}/?pagecount=1";

    [Fact]
    public void AkeVorlagen_ergeben_die_bisherigen_Adressen()
    {
        LinkVorlage.Build("true", Vault, LinkVorlage.PlatzhalterArtikelnummer, "10-2034-01")
            .Should().Be("http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search=10-2034-01&searchContext=0");
        LinkVorlage.Build("true", Enaio, LinkVorlage.PlatzhalterObjektId, "123456")
            .Should().Be("http://akeosapp.ake.at/oscontentviewer/viewer/123456/?pagecount=1");
    }

    [Theory]
    [InlineData("false")]
    [InlineData("FALSE")]
    [InlineData("False")]
    [InlineData("  false ")]
    public void Nur_false_schaltet_aus(string aktiv) =>
        LinkVorlage.Build(aktiv, Vault, LinkVorlage.PlatzhalterArtikelnummer, "4711").Should().BeNull();

    [Theory]
    [InlineData(null)]   // Zeile fehlt
    [InlineData("true")]
    [InlineData("nein")] // Default-an-Lesart: nur "false" sperrt
    [InlineData("")]
    public void Alles_ausser_false_laesst_an(string? aktiv) =>
        LinkVorlage.Build(aktiv, Vault, LinkVorlage.PlatzhalterArtikelnummer, "4711").Should().NotBeNull();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Leere_Vorlage_ergibt_keinen_Link(string? vorlage) =>
        LinkVorlage.Build("true", vorlage, LinkVorlage.PlatzhalterArtikelnummer, "4711").Should().BeNull();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Leerer_Wert_ergibt_keinen_Link(string? wert) =>
        LinkVorlage.Build("true", Vault, LinkVorlage.PlatzhalterArtikelnummer, wert).Should().BeNull();

    [Theory]
    [InlineData("A B", "A%20B")]
    [InlineData("A&B", "A%26B")]
    [InlineData("A+B", "A%2BB")]
    [InlineData("A#B", "A%23B")]
    [InlineData(" 10-20 ", "10-20")]
    public void Wert_wird_url_kodiert_eingesetzt(string wert, string erwartet) =>
        LinkVorlage.Build("true", "https://test.example/s?q={Artikelnummer}&x=1", LinkVorlage.PlatzhalterArtikelnummer, wert)
            .Should().Be($"https://test.example/s?q={erwartet}&x=1");

    [Fact]
    public void Ohne_Platzhalter_wird_angehaengt_Platzhalter_exakt()
    {
        LinkVorlage.Build("true", " https://test.example/?q= ", LinkVorlage.PlatzhalterObjektId, "99").Should().Be("https://test.example/?q=99");
        LinkVorlage.Build("true", "https://test.example/view?id={ObjektId}", LinkVorlage.PlatzhalterObjektId, "99")
            .Should().Be("https://test.example/view?id=99");
        // Ordinal: {objektid} ist nicht der Platzhalter -> anhaengen
        LinkVorlage.Build("true", "https://t/{objektid}", LinkVorlage.PlatzhalterObjektId, "99").Should().Be("https://t/{objektid}99");
    }
}
