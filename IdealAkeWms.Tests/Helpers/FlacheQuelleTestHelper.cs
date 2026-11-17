using IdealAkeWms.Services.Beschichtung;
using Moq;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>v1.99.34: Beschichtungs-Positionsquelle fuer Tests der IDEAL-Wege — immer hierarchisch (FA-Struktur bleibt Quelle).</summary>
public static class FlacheQuelleTestHelper
{
    public static BeschichtungFlacheQuelle Hierarchisch()
    {
        var m = new Mock<BeschichtungFlacheQuelle>(null!, null!, null!, null!, null!, null!, null!);
        m.Setup(q => q.IstFlachAsync()).ReturnsAsync(false);
        return m.Object;
    }
}
