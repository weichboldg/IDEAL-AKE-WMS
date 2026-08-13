using System;
using System.Collections.Generic;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Tests fuer <see cref="IsoWeekRange"/> — die Aufloesung der KW-Zeitraum-Eingabe
/// (<c>YYYY-Www</c>) in einen ISO-Wochen-Datumsbereich Montag–Sonntag (Nachtrag 2026-08-13).
/// </summary>
public class IsoWeekRangeTests
{
    [Theory]
    [InlineData("2026-W10", 2026, 10)]
    [InlineData("2026-W01", 2026, 1)]
    [InlineData(" 2026-W53 ", 2026, 53)] // 2026 hat 53 KW; Trim
    public void Parse_ValidValues(string input, int year, int week)
    {
        var parsed = IsoWeekRange.Parse(input);
        parsed.Should().NotBeNull();
        parsed!.Value.Year.Should().Be(year);
        parsed.Value.Week.Should().Be(week);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2026")]
    [InlineData("2026-10")]
    [InlineData("2026-W")]
    [InlineData("2026-W00")]   // KW 0 gibt es nicht
    [InlineData("2025-W53")]   // 2025 hat nur 52 KW
    [InlineData("abc-Wxy")]
    public void Parse_InvalidValues_ReturnNull(string? input)
    {
        IsoWeekRange.Parse(input).Should().BeNull();
    }

    [Fact]
    public void Resolve_SingleWeek_IsMondayToSundayInclusive()
    {
        // ISO-KW 10/2026: Montag 2026-03-02, Sonntag 2026-03-08.
        var range = IsoWeekRange.Resolve("2026-W10", "2026-W10");
        range.Should().NotBeNull();
        range!.Value.StartInclusive.Should().Be(new DateTime(2026, 3, 2));   // Montag
        range.Value.EndExclusive.Should().Be(new DateTime(2026, 3, 9));      // Montag danach (exklusiv)

        // Sonntag 23:59 der KW liegt noch drin, der folgende Montag 00:00 nicht mehr.
        var (start, endEx) = range.Value;
        bool InRange(DateTime d) => d >= start && d < endEx;
        InRange(new DateTime(2026, 3, 8, 23, 59, 0)).Should().BeTrue();  // Sonntag der KW
        InRange(new DateTime(2026, 3, 9, 0, 0, 0)).Should().BeFalse();   // Montag danach
        InRange(new DateTime(2026, 3, 1, 23, 59, 0)).Should().BeFalse(); // Sonntag davor
    }

    [Fact]
    public void Resolve_Range_SpansVonMondayToBisSunday()
    {
        // KW 10 bis KW 11: Montag 2026-03-02 bis Sonntag 2026-03-15 (endExclusive = 2026-03-16).
        var range = IsoWeekRange.Resolve("2026-W10", "2026-W11");
        range!.Value.StartInclusive.Should().Be(new DateTime(2026, 3, 2));
        range.Value.EndExclusive.Should().Be(new DateTime(2026, 3, 16));
    }

    [Fact]
    public void Resolve_OnlyOneField_UsesItForBoth()
    {
        var vonOnly = IsoWeekRange.Resolve("2026-W10", null);
        var bisOnly = IsoWeekRange.Resolve(null, "2026-W10");
        var both = IsoWeekRange.Resolve("2026-W10", "2026-W10");

        vonOnly.Should().Be(both);
        bisOnly.Should().Be(both);
    }

    [Fact]
    public void Resolve_ReversedOrder_IsSwapped()
    {
        var normal = IsoWeekRange.Resolve("2026-W10", "2026-W11");
        var reversed = IsoWeekRange.Resolve("2026-W11", "2026-W10");
        reversed.Should().Be(normal);
    }

    [Fact]
    public void Resolve_BothEmpty_ReturnsNull()
    {
        IsoWeekRange.Resolve(null, null).Should().BeNull();
        IsoWeekRange.Resolve("", "  ").Should().BeNull();
    }

    [Fact]
    public void HauptFasInRange_SelectsByDateField_FanOutFree()
    {
        var range = IsoWeekRange.Resolve("2026-W10", "2026-W10")!.Value;
        var infos = new List<FaHierarchyOrderInfo>
        {
            new() { HauptFA = 1, KO_Termin = new DateTime(2026, 3, 4) },   // KW 10 -> drin
            new() { HauptFA = 1, KO_Termin = new DateTime(2026, 3, 11) },  // zweite Kopfzeile, KW 11
            new() { HauptFA = 2, KO_Termin = new DateTime(2026, 3, 11) },  // KW 11 -> draussen
            new() { HauptFA = 3, KO_Termin = null },                       // kein Termin -> draussen
        };

        var result = IsoWeekRange.HauptFasInRange(infos, o => o.KO_Termin, range);

        // HauptFA 1 ist enthalten, weil EINE seiner Kopfzeilen in der KW liegt (fan-out-frei).
        result.Should().BeEquivalentTo(new[] { 1 });
    }
}
