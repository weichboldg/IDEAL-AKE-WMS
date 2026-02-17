using AKEBDELight.Services;
using FluentAssertions;

namespace AKEBDELight.Tests.Services;

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
}
