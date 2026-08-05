using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Tests.Services;

public class SageTlsPolicyTests
{
    [Theory]
    [InlineData(null)]        // fehlt
    [InlineData("")]          // leer
    [InlineData("   ")]       // whitespace
    [InlineData("yes")]       // nicht parsebar
    [InlineData("1")]         // nicht parsebar (bool.TryParse akzeptiert nur true/false)
    [InlineData("tru")]       // Tippfehler
    public void ShouldVerifyCertificate_MissingOrUnparsable_ReturnsTrue_FailSafe(string? raw)
    {
        SageTlsPolicy.ShouldVerifyCertificate(raw).Should().BeTrue();
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData(" false ")]   // bool.TryParse trimmt
    public void ShouldVerifyCertificate_ExplicitFalse_ReturnsFalse(string raw)
    {
        SageTlsPolicy.ShouldVerifyCertificate(raw).Should().BeFalse();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    public void ShouldVerifyCertificate_ExplicitTrue_ReturnsTrue(string raw)
    {
        SageTlsPolicy.ShouldVerifyCertificate(raw).Should().BeTrue();
    }

    [Fact]
    public void Catalog_HasKey_WithDefaultTrue()
    {
        var def = ServiceSettingDefinitions.All.SingleOrDefault(d => d.Key == SageTlsPolicy.SettingKey);
        def.Should().NotBeNull();
        def!.Type.Should().Be(ServiceSettingType.Bool);
        def.DefaultValue.Should().Be("true");
        // Der Katalog-Default muss selbst die fail-safe-Invariante erfuellen.
        SageTlsPolicy.ShouldVerifyCertificate(def.DefaultValue).Should().BeTrue();
    }
}
