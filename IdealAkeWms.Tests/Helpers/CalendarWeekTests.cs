using System.Globalization;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// Testet die ISO 8601 Kalenderwochenberechnung die in der Werkstattauftraege-View verwendet wird.
/// Die View nutzt System.Globalization.ISOWeek.GetWeekOfYear() — diese Tests verifizieren
/// die erwarteten Ergebnisse fuer Randfaelle (Jahreswechsel, KW53, KW1).
/// </summary>
public class CalendarWeekTests
{
    [Theory]
    [InlineData(2026, 1, 1, 1)]    // Donnerstag 01.01.2026 = KW1
    [InlineData(2026, 1, 4, 1)]    // Sonntag 04.01.2026 = KW1
    [InlineData(2026, 1, 5, 2)]    // Montag 05.01.2026 = KW2
    [InlineData(2026, 3, 30, 14)]  // Montag 30.03.2026 = KW14
    [InlineData(2026, 12, 28, 53)] // Montag 28.12.2026 = KW53
    [InlineData(2026, 12, 31, 53)] // Donnerstag 31.12.2026 = KW53
    [InlineData(2027, 1, 1, 53)]   // Freitag 01.01.2027 = immer noch KW53 von 2026!
    [InlineData(2027, 1, 4, 1)]    // Montag 04.01.2027 = KW1 von 2027
    [InlineData(2025, 12, 29, 1)]  // Montag 29.12.2025 = KW1 von 2026!
    [InlineData(2025, 12, 31, 1)]  // Mittwoch 31.12.2025 = KW1 von 2026
    public void GetWeekOfYear_ReturnsCorrectIsoWeek(int year, int month, int day, int expectedKw)
    {
        var date = new DateTime(year, month, day);
        var kw = ISOWeek.GetWeekOfYear(date);
        kw.Should().Be(expectedKw,
            $"{date:dd.MM.yyyy} ({date:dddd}) sollte KW{expectedKw} sein");
    }

    [Fact]
    public void FormatDateWithKw_MatchesExpectedPattern()
    {
        // Simuliert die Formatierung aus der View
        var date = new DateTime(2026, 1, 7); // Mittwoch KW2
        var kw = ISOWeek.GetWeekOfYear(date);
        var formatted = $"{date:dd.MM.yyyy} KW{kw}";
        formatted.Should().Be("07.01.2026 KW2");
    }

    [Fact]
    public void FormatDateWithKw_KW53_HasNoLeadingZero()
    {
        var date = new DateTime(2026, 12, 28); // KW53
        var kw = ISOWeek.GetWeekOfYear(date);
        var formatted = $"KW{kw}";
        formatted.Should().Be("KW53");
        formatted.Should().NotContain("KW053", "KW soll keine fuehrende Null haben");
    }

    [Fact]
    public void AllDatesInWeek_HaveSameKw()
    {
        // KW14 2026: 30.03 (Mo) bis 05.04 (So)
        var monday = new DateTime(2026, 3, 30);
        for (int i = 0; i < 7; i++)
        {
            var date = monday.AddDays(i);
            ISOWeek.GetWeekOfYear(date).Should().Be(14,
                $"{date:dd.MM.yyyy} ({date:dddd}) muss in KW14 sein");
        }
    }
}
