using FluentAssertions;
using IdealAkeWms.Controllers;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>QA v1.99.35 F1: Menge wird kulturunabhaengig gelesen (Default-Binder de-DE machte aus "40.000" 40000, aus "1.5" 15).</summary>
public class BfAuftraegeMengeTests
{
    [Theory]
    [InlineData("40.000", 40)]
    [InlineData("40", 40)]
    [InlineData("1.5", 1.5)]
    [InlineData("1,5", 1.5)]
    [InlineData(" 20 ", 20)]
    [InlineData("abc", 0)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void Menge_liest_Punkt_und_Komma_als_Dezimaltrenner(string? text, double erwartet) =>
        BfAuftraegeController.Menge(text).Should().Be((decimal)erwartet);
}
