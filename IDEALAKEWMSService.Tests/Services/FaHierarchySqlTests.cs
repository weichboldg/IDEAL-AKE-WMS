using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class FaHierarchySqlTests
{
    // ---- Positivfaelle: die REALEN Produktions-View-Namen (mit Bindestrich) MUESSEN durchgehen.
    //      Genau dieser Testfall haette den urspruenglichen ^[A-Za-z0-9_]+$-Fehler (N-1) in
    //      Sekunden gefunden.
    [Theory]
    [InlineData("[vw_IDEAL-AKE_Kommissionierung_FAListe]", "[vw_IDEAL-AKE_Kommissionierung_FAListe]")]
    [InlineData("[vw_IDEAL-AKE_Kommissionierung_FAInfos]", "[vw_IDEAL-AKE_Kommissionierung_FAInfos]")]
    [InlineData("vw_IDEAL-AKE_Kommissionierung_FAListe", "[vw_IDEAL-AKE_Kommissionierung_FAListe]")]
    [InlineData("dbo.vw_IDEAL-AKE_Kommissionierung_FAListe", "[dbo].[vw_IDEAL-AKE_Kommissionierung_FAListe]")]
    [InlineData("[dbo].[vw_IDEAL-AKE_Kommissionierung_FAInfos]", "[dbo].[vw_IDEAL-AKE_Kommissionierung_FAInfos]")]
    [InlineData("Simple_Name", "[Simple_Name]")]
    public void ValidateAndQuote_valid_names_are_accepted_and_quoted(string input, string expected)
    {
        FaHierarchySql.ValidateAndQuote(input).Should().Be(expected);
    }

    // ---- Negativfaelle: Injection-/Homoglyph-/Kommentar-Varianten MUESSEN abgelehnt werden.
    [Theory]
    [InlineData(null)]                                        // leer/nicht konfiguriert
    [InlineData("")]                                          // leer
    [InlineData("   ")]                                       // nur Whitespace
    [InlineData("vw_Foo; DROP TABLE x")]                      // Semikolon + Leerzeichen
    [InlineData("vw Foo")]                                    // Leerzeichen
    [InlineData("vw_Foo--kommentar")]                         // SQL-Kommentar-Einleiter --
    [InlineData("vw_Foo/*bla*/")]                             // Blockkommentar
    [InlineData("[vw_Foo")]                                   // unausgeglichene Klammer
    [InlineData("vw_Foo]")]                                   // unausgeglichene Klammer
    [InlineData("vw[Foo]bar")]                                // Klammer mitten im Segment
    [InlineData("dbo.schema.vw_Foo")]                         // zu viele Segmente
    [InlineData("vw_IDEАL")]                                  // kyrillisches 'А' (Homoglyph, Nicht-ASCII)
    [InlineData("vw_Foo'")]                                   // Apostroph
    public void ValidateAndQuote_invalid_names_are_rejected(string? input)
    {
        var act = () => FaHierarchySql.ValidateAndQuote(input);
        act.Should().Throw<FaHierarchyViewNameException>();
    }

    [Fact]
    public void BuildNodeSelectSql_uses_EXISTS_not_row_join()
    {
        var sql = FaHierarchySql.BuildNodeSelectSql(
            "[vw_IDEAL-AKE_Kommissionierung_FAListe]",
            "[vw_IDEAL-AKE_Kommissionierung_FAInfos]");

        sql.Should().Contain("WHERE EXISTS");
        sql.Should().Contain("i.[HauptFA] = f.[HauptFA]");
        // Kein fan-out-anfaelliger Zeilen-Join.
        sql.Should().NotContain("INNER JOIN");
    }

    [Fact]
    public void BuildInfosSelectSql_maps_montage_abteilung_column()
    {
        var sql = FaHierarchySql.BuildInfosSelectSql("[vw_IDEAL-AKE_Kommissionierung_FAInfos]");
        sql.Should().Contain("[Montage-Abteilung] AS [MontageAbteilung]");
    }

    [Fact]
    public void BuildCountSql_targets_the_quoted_view()
    {
        FaHierarchySql.BuildCountSql("[vw_X]").Should().Be("SELECT COUNT_BIG(*) FROM dbo.[vw_X]");
    }
}
