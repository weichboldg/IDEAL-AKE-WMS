using FluentAssertions;
using IdealAkeWms.Services;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class OseonDueDateCalculatorTests
{
    private static IBusinessDayService NewBusinessDayService()
    {
        var m = new Mock<IBusinessDayService>();
        m.Setup(x => x.AddBusinessDays(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<HashSet<DateTime>>()))
            .Returns<DateTime, int, HashSet<DateTime>>((date, days, holidays) =>
            {
                var current = date.Date;
                var sign = Math.Sign(days);
                var remaining = Math.Abs(days);
                while (remaining > 0)
                {
                    current = current.AddDays(sign);
                    if (current.DayOfWeek == DayOfWeek.Saturday) continue;
                    if (current.DayOfWeek == DayOfWeek.Sunday) continue;
                    if (holidays.Contains(current.Date)) continue;
                    remaining--;
                }
                return current;
            });
        return m.Object;
    }

    [Fact]
    public void Calculate_OffsetZero_ReturnsBaseDateOnly()
    {
        var bds = NewBusinessDayService();
        var holidays = new HashSet<DateTime>();
        var result = OseonDueDateCalculator.Calculate(new DateTime(2026, 4, 30, 14, 30, 0), 0, bds, holidays);
        result.Should().Be(new DateTime(2026, 4, 30));
    }

    [Fact]
    public void Calculate_PositiveOffset_DelegatesToBusinessDayService()
    {
        var bds = NewBusinessDayService();
        var holidays = new HashSet<DateTime>();
        var result = OseonDueDateCalculator.Calculate(new DateTime(2026, 4, 30), 3, bds, holidays); // Do
        result.Should().Be(new DateTime(2026, 5, 5)); // Di (überspringt Sa/So)
    }

    [Fact]
    public void Calculate_NegativeOffset_DelegatesToBusinessDayService()
    {
        var bds = NewBusinessDayService();
        var holidays = new HashSet<DateTime>();
        var result = OseonDueDateCalculator.Calculate(new DateTime(2026, 5, 5), -3, bds, holidays); // Di
        result.Should().Be(new DateTime(2026, 4, 30)); // Do
    }

    [Fact]
    public void Calculate_OffsetWithHoliday_HolidayIsSkipped()
    {
        var bds = NewBusinessDayService();
        var holidays = new HashSet<DateTime> { new DateTime(2026, 5, 1) }; // Tag der Arbeit, Fr
        var result = OseonDueDateCalculator.Calculate(new DateTime(2026, 4, 30), 1, bds, holidays); // Do +1 Werktag
        result.Should().Be(new DateTime(2026, 5, 4)); // Mo (überspringt Fr-Feiertag + Sa + So)
    }

    [Fact]
    public void Calculate_OffsetZeroIgnoresHolidays()
    {
        var bds = NewBusinessDayService();
        var holidays = new HashSet<DateTime> { new DateTime(2026, 4, 30) };
        var result = OseonDueDateCalculator.Calculate(new DateTime(2026, 4, 30), 0, bds, holidays);
        result.Should().Be(new DateTime(2026, 4, 30)); // bleibt
    }
}
