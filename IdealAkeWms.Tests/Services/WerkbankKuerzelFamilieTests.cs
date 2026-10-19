using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class WerkbankKuerzelFamilieTests
{
    private static ProductionWorkplace Wp(int id, string? code, bool bde = true, bool missing = false, bool nurAg = false) => new()
    {
        Id = id, Name = $"WB {id}", ArbeitsschrittCode = code, BdeAktiv = bde, BdeNurAgFertigmeldung = nurAg,
        SageMissingSince = missing ? new DateTime(2026, 10, 19) : null,
    };

    [Fact]
    public void FamilieIds_SameCode_TrimmedCaseInsensitive_IncludesMarkedAndInactive()
    {
        var w65 = Wp(65, "EG");
        var alle = new[] { w65, Wp(19, " eg ", bde: false, missing: true), Wp(70, "EG", bde: false), Wp(63, "ST") };

        WerkbankKuerzelFamilie.FamilieIds(w65, alle).Should().BeEquivalentTo(new[] { 65, 19, 70 });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FamilieIds_NoCode_OnlySelf_Ake(string? code)
    {
        var w = Wp(1, code);
        WerkbankKuerzelFamilie.FamilieIds(w, new[] { w, Wp(2, code), Wp(3, "EG") }).Should().BeEquivalentTo(new[] { 1 });
    }

    [Fact]
    public void FamilieIds_MarkedWorkplace_OnlySelf()
    {
        var w19 = Wp(19, "EG", missing: true);
        WerkbankKuerzelFamilie.FamilieIds(w19, new[] { w19, Wp(65, "EG") }).Should().BeEquivalentTo(new[] { 19 });
    }

    [Fact]
    public void BdeRelevanteIds_ActivePlusSameCodeAsActiveUnmarked()
    {
        var alle = new[]
        {
            Wp(65, "EG"),                          // aktiv
            Wp(19, "EG", bde: false, missing: true), // Kuerzel an aktiver Werkbank -> relevant
            Wp(63, "ST", bde: false),              // ST nirgends aktiv -> nicht relevant
            Wp(80, "KA", missing: true),           // aktiv (gekennzeichnet zaehlt nur nicht als Kuerzel-Quelle)
            Wp(81, "KA", bde: false),              // KA nur an gekennzeichneter aktiver -> nicht relevant
            Wp(5, null),                           // AKE aktiv ohne Kuerzel
        };

        WerkbankKuerzelFamilie.BdeRelevanteIds(alle).Should().BeEquivalentTo(new[] { 65, 19, 80, 5 });
    }

    [Fact]
    public void Bevorzugt_BdeActiveFirst_ThenSmallestId()
    {
        WerkbankKuerzelFamilie.Bevorzugt(new[] { Wp(19, "EG", bde: false), Wp(70, "EG"), Wp(65, "EG") }).Id.Should().Be(65);
        WerkbankKuerzelFamilie.Bevorzugt(new[] { Wp(70, "EG", bde: false), Wp(19, "EG", bde: false) }).Id.Should().Be(19);
    }

    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]   // NurFA global gewinnt (Antwort 6)
    [InlineData(false, true, false, false)] // BDE aus
    [InlineData(true, false, false, false)] // Option aus (Default, AKE unveraendert)
    public void NurAgFertigmeldungWirksam(bool bde, bool option, bool nurFa, bool expected) =>
        WerkbankKuerzelFamilie.NurAgFertigmeldungWirksam(Wp(1, null, bde: bde, nurAg: option), nurFa).Should().Be(expected);
}
