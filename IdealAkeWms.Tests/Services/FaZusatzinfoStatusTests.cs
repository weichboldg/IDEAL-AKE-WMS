using FluentAssertions;
using IdealAkeWms.Services;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Fold 2 (v1.26.0, Spec §10.6): gemeinsame Status-Wahrheit fuer "verpackt"/"abgeholt" —
/// genutzt vom FaZusatzinfoSyncService (Auto-Erledigt) UND der BDE-Buchungs-Sperre.
/// </summary>
public class FaZusatzinfoStatusTests
{
    [Theory]
    [InlineData("verpackt")]
    [InlineData("abgeholt")]
    [InlineData("Verpackt")]
    [InlineData("ABGEHOLT")]
    [InlineData(" Abgeholt ")]
    [InlineData("  verpackt  ")]
    public void IstVerpacktOderAbgeholt_Matches(string status)
    {
        FaZusatzinfoStatus.IstVerpacktOderAbgeholt(status).Should().BeTrue();
    }

    [Theory]
    [InlineData("in Produktion")]
    [InlineData("offen")]
    [InlineData("verpackt und mehr")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IstVerpacktOderAbgeholt_NoMatch(string? status)
    {
        FaZusatzinfoStatus.IstVerpacktOderAbgeholt(status).Should().BeFalse();
    }

    [Fact]
    public void Konstanten_SindLowercase_FuerInlineEfVergleiche()
    {
        // EF-Listen-Filter vergleichen .Trim().ToLower() gegen die Konstanten —
        // die Konstanten MUESSEN deshalb lowercase sein.
        FaZusatzinfoStatus.Verpackt.Should().Be("verpackt");
        FaZusatzinfoStatus.Abgeholt.Should().Be("abgeholt");
    }
}
