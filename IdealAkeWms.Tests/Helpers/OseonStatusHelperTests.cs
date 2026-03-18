using FluentAssertions;
using IdealAkeWms.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

public class OseonStatusHelperTests
{
    [Theory]
    [InlineData(10, "Unvollständig")]
    [InlineData(20, "Gültig")]
    [InlineData(30, "Freigegeben")]
    [InlineData(60, "In Arbeit")]
    [InlineData(70, "Gesperrt")]
    [InlineData(90, "Fertig")]
    [InlineData(95, "Storniert")]
    public void GetStatusText_ReturnsCorrectText(int status, string expected)
    {
        OseonStatusHelper.GetStatusText(status).Should().Be(expected);
    }

    [Fact]
    public void GetStatusText_UnknownStatus_ReturnsUnbekannt()
    {
        OseonStatusHelper.GetStatusText(99).Should().Contain("Unbekannt");
    }

    [Theory]
    [InlineData(90, "bg-success")]
    [InlineData(70, "bg-danger")]
    [InlineData(60, "bg-primary")]
    [InlineData(95, "bg-dark")]
    public void GetStatusBadgeClass_ReturnsCorrectClass(int status, string expected)
    {
        OseonStatusHelper.GetStatusBadgeClass(status).Should().Be(expected);
    }
}
