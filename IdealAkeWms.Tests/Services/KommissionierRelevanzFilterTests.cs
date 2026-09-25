using FluentAssertions;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class KommissionierRelevanzFilterTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("KA-02", true)]
    public void IsRelevant(string? value, bool expected) =>
        KommissionierRelevanzFilter.IsRelevant(value).Should().Be(expected);
}
