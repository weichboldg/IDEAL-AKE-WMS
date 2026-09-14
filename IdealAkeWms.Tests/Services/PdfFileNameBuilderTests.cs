using FluentAssertions;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class PdfFileNameBuilderTests
{
    [Theory]
    [InlineData("Kommissionierliste", 104857, "Kommissionierliste_104857_20260906-1430.pdf")]
    [InlineData("Beschichtungsauftrag", 104857, "Beschichtungsauftrag_104857_20260906-1430.pdf")]
    [InlineData("Kommissionierliste", 7, "Kommissionierliste_7_20260906-1430.pdf")]
    public void Build_FollowsSchema(string art, int hauptFa, string expected)
        => PdfFileNameBuilder.Build(art, hauptFa, new DateTime(2026, 9, 6, 14, 30, 59)).Should().Be(expected);

    [Fact]
    public void Build_PadsMonthDayHourMinute()
        => PdfFileNameBuilder.Build("Kommissionierliste", 1, new DateTime(2026, 1, 2, 3, 4, 0))
            .Should().Be("Kommissionierliste_1_20260102-0304.pdf");

    [Theory]
    [InlineData("Kommissionierliste")]
    [InlineData("Beschichtungsauftrag")]
    public void Build_RealDocumentArts_AreAsciiOnly(string art)
        => PdfFileNameBuilder.Build(art, 1, DateTime.Now).Should().MatchRegex("^[A-Za-z0-9_.-]+$");
}
