using IdealAkeWms.Services;
using FluentAssertions;

namespace IdealAkeWms.Tests.Services;

public class BusinessDayServiceTests
{
    private readonly BusinessDayService _sut = new();
    private readonly HashSet<DateTime> _noHolidays = new();

    [Fact]
    public void SubtractBusinessDays_NormalWeekday()
    {
        // Wednesday 2026-02-18 minus 1 = Tuesday 2026-02-17
        var result = _sut.SubtractBusinessDays(new DateTime(2026, 2, 18), 1, _noHolidays);
        result.Should().Be(new DateTime(2026, 2, 17));
    }

    [Fact]
    public void SubtractBusinessDays_SkipsWeekend()
    {
        // Monday 2026-02-16 minus 1 = Friday 2026-02-13
        var result = _sut.SubtractBusinessDays(new DateTime(2026, 2, 16), 1, _noHolidays);
        result.Should().Be(new DateTime(2026, 2, 13));
    }

    [Fact]
    public void SubtractBusinessDays_SkipsHoliday()
    {
        // Wednesday 2026-02-18, Tuesday 2026-02-17 is holiday
        // minus 1 should skip Tuesday and land on Monday 2026-02-16
        var holidays = new HashSet<DateTime> { new DateTime(2026, 2, 17) };
        var result = _sut.SubtractBusinessDays(new DateTime(2026, 2, 18), 1, holidays);
        result.Should().Be(new DateTime(2026, 2, 16));
    }

    [Fact]
    public void SubtractBusinessDays_SkipsWeekendAndHoliday()
    {
        // Monday 2026-02-16, Friday 2026-02-13 is holiday
        // minus 1 should skip weekend (Sat+Sun) AND Friday holiday → Thursday 2026-02-12
        var holidays = new HashSet<DateTime> { new DateTime(2026, 2, 13) };
        var result = _sut.SubtractBusinessDays(new DateTime(2026, 2, 16), 1, holidays);
        result.Should().Be(new DateTime(2026, 2, 12));
    }

    [Fact]
    public void SubtractBusinessDays_ZeroDays_ReturnsSameDate()
    {
        var date = new DateTime(2026, 2, 18);
        var result = _sut.SubtractBusinessDays(date, 0, _noHolidays);
        result.Should().Be(date);
    }

    [Fact]
    public void SubtractBusinessDays_MultipleDays()
    {
        // Friday 2026-02-20 minus 4 = Monday 2026-02-16
        // (Thu 19, Wed 18, Tue 17, Mon 16)
        var result = _sut.SubtractBusinessDays(new DateTime(2026, 2, 20), 4, _noHolidays);
        result.Should().Be(new DateTime(2026, 2, 16));
    }

    [Fact]
    public void SubtractBusinessDays_NegativeDays_TreatedAsPositive()
    {
        // Math.Abs(-1) = 1
        var result = _sut.SubtractBusinessDays(new DateTime(2026, 2, 18), -1, _noHolidays);
        result.Should().Be(new DateTime(2026, 2, 17));
    }

    // === AddBusinessDays Tests ===

    [Fact]
    public void AddBusinessDays_ZeroDays_ReturnsSameDate()
    {
        var date = new DateTime(2026, 2, 18);
        var result = _sut.AddBusinessDays(date, 0, _noHolidays);
        result.Should().Be(date);
    }

    [Fact]
    public void AddBusinessDays_PositiveDays_Forward()
    {
        // Wednesday 2026-02-18 plus 2 = Friday 2026-02-20
        var result = _sut.AddBusinessDays(new DateTime(2026, 2, 18), 2, _noHolidays);
        result.Should().Be(new DateTime(2026, 2, 20));
    }

    [Fact]
    public void AddBusinessDays_SkipsWeekend_Forward()
    {
        // Friday 2026-02-20 plus 1 = Monday 2026-02-23
        var result = _sut.AddBusinessDays(new DateTime(2026, 2, 20), 1, _noHolidays);
        result.Should().Be(new DateTime(2026, 2, 23));
    }

    [Fact]
    public void AddBusinessDays_SkipsHoliday_Forward()
    {
        // Wednesday 2026-02-18 plus 1, Thursday 2026-02-19 is holiday
        // should land on Friday 2026-02-20
        var holidays = new HashSet<DateTime> { new DateTime(2026, 2, 19) };
        var result = _sut.AddBusinessDays(new DateTime(2026, 2, 18), 1, holidays);
        result.Should().Be(new DateTime(2026, 2, 20));
    }

    [Fact]
    public void AddBusinessDays_NegativeDays_Backward()
    {
        // Wednesday 2026-02-18 minus 1 = Tuesday 2026-02-17
        var result = _sut.AddBusinessDays(new DateTime(2026, 2, 18), -1, _noHolidays);
        result.Should().Be(new DateTime(2026, 2, 17));
    }

    [Fact]
    public void AddBusinessDays_NegativeDays_SkipsWeekend()
    {
        // Monday 2026-02-23 minus 1 = Friday 2026-02-20
        var result = _sut.AddBusinessDays(new DateTime(2026, 2, 23), -1, _noHolidays);
        result.Should().Be(new DateTime(2026, 2, 20));
    }

    [Fact]
    public void AddBusinessDays_MultipleDays_SkipsWeekendAndHoliday()
    {
        // Wednesday 2026-02-18 plus 4
        // Thu 19 (holiday) skip, Fri 20 (1), Mon 23 (2), Tue 24 (3), Wed 25 (4)
        var holidays = new HashSet<DateTime> { new DateTime(2026, 2, 19) };
        var result = _sut.AddBusinessDays(new DateTime(2026, 2, 18), 4, holidays);
        result.Should().Be(new DateTime(2026, 2, 25));
    }

    [Fact]
    public void AddBusinessDays_StanzterminOffset_Minus1()
    {
        // Stanztermin 22.01.2026 (Thu), B offset = -1 AT
        // Result: Wednesday 21.01.2026
        var result = _sut.AddBusinessDays(new DateTime(2026, 1, 22), -1, _noHolidays);
        result.Should().Be(new DateTime(2026, 1, 21));
    }

    [Fact]
    public void AddBusinessDays_StanzterminOffset_Plus2()
    {
        // Stanztermin 22.01.2026 (Thu), BG offset = +2 AT
        // Fri 23 (1), Mon 26 (2)
        var result = _sut.AddBusinessDays(new DateTime(2026, 1, 22), 2, _noHolidays);
        result.Should().Be(new DateTime(2026, 1, 26));
    }

    [Fact]
    public void AddBusinessDays_StanzterminOffset_Plus5()
    {
        // Stanztermin 22.01.2026 (Thu), SL/RE offset = +5 AT
        // Fri 23 (1), Mon 26 (2), Tue 27 (3), Wed 28 (4), Thu 29 (5)
        var result = _sut.AddBusinessDays(new DateTime(2026, 1, 22), 5, _noHolidays);
        result.Should().Be(new DateTime(2026, 1, 29));
    }
}
