using IdealAkeWms.Services;
using Xunit;
using FluentAssertions;

namespace IdealAkeWms.Tests.Services;

public class WindowsAccountHelperTests
{
    [Theory]
    [InlineData("AKE\\jmuster", "jmuster")]
    [InlineData("ake\\JMuster", "JMuster")]
    [InlineData("jmuster", "jmuster")]
    [InlineData("jmuster@ake.at", "jmuster")]
    [InlineData("DOMAIN\\sam@upn", "sam@upn")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void ExtractSam_ParsesIdentityName(string? input, string? expected)
    {
        WindowsAccountHelper.ExtractSam(input).Should().Be(expected);
    }
}
