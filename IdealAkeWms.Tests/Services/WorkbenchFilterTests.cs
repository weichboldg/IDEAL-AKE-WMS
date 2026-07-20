using FluentAssertions;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class WorkbenchFilterTests
{
    [Theory]
    [InlineData(null, null, true)]            // kein Filter -> alle
    [InlineData("Werkbank 1", null, true)]    // leerer Filter -> alle
    [InlineData("Werkbank 1", "", true)]      // leerer Filter -> alle
    [InlineData("Werkbank 1", "   ", true)]   // nur Whitespace -> alle
    [InlineData(null, "Werkbank 1", false)]   // Name fehlt, Filter gesetzt -> kein Treffer
    public void Matches_EmptyCases(string? name, string? filter, bool expected)
        => WorkbenchFilter.Matches(name, filter).Should().Be(expected);

    [Theory]
    [InlineData("Werkbank 1", "Werkbank 1", true)]
    [InlineData("Werkbank 2", "Werkbank 1", false)]
    [InlineData("WB-A2", "WB-A", true)]                 // Contains (Teilstring)
    [InlineData("WB-A", "wb-a", true)]                  // case-insensitiv
    [InlineData("Halle 3", "WB-A,Halle 3", true)]       // Komma-OR
    [InlineData("WB-B", "WB-A,Halle 3", false)]
    [InlineData("WB-A", " WB-A , WB-B ", true)]         // Tokens getrimmt
    public void Matches_ContainsSemantics(string? name, string? filter, bool expected)
        => WorkbenchFilter.Matches(name, filter).Should().Be(expected);
}
