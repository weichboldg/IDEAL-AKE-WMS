using FluentAssertions;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class KommissionierzielFilterWertTests
{
    [Theory]
    [InlineData("!(leer),KA-02", "!(leer)", true)]   // Einzelziel verworfen -> Hinweis
    [InlineData("KA-02, !(LEER)", "!(leer)", true)]
    [InlineData("!(LEER)", "!(leer)", false)]         // nur Schreibweise -> KEIN Hinweis
    [InlineData(" !(leer) ", "!(leer)", false)]
    [InlineData("!(leer)", "!(leer)", false)]
    [InlineData(" KA-02 ", "KA-02", false)]
    [InlineData("KA-02,KA-03", "KA-02,KA-03", false)]
    [InlineData("   ", null, false)]
    [InlineData(null, null, false)]
    public void Normalize(string? input, string? expected, bool dropped)
    {
        var (value, droppedTargets) = KommissionierzielFilterWert.Normalize(input);
        value.Should().Be(expected);
        droppedTargets.Should().Be(dropped);
    }
}
