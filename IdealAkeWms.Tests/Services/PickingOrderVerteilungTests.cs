using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.89 (Spec 2026-11-26 Teil 3, K8, AK 5–7): Verteilung der Ist-Menge einer Summenzeile auf ihre offenen Zeilen.</summary>
public class PickingOrderVerteilungTests
{
    private static PickingOrderLine Z(int id, string? pos, decimal soll) => new()
    {
        Id = id, ArticleNumber = "A", BomPosition = pos, SollMenge = soll, CreatedBy = "t", CreatedByWindows = "t",
    };

    private static List<(int Id, decimal Menge)> V(decimal ist, params PickingOrderLine[] zeilen)
        => PickingOrderVerteilung.Verteile(zeilen, ist).Select(a => (a.Zeile.Id, a.Menge)).ToList();

    [Fact]
    public void Genau_AK5() => V(5m, Z(2, "20", 2), Z(1, "10", 3)).Should().Equal((1, 3m), (2, 2m));

    [Fact]
    public void Unterdeckung_MittenInZweiterZeile_AK6()
    {
        V(4m, Z(1, "10", 3), Z(2, "20", 2)).Should().Equal((1, 3m), (2, 1m));
        V(2m, Z(1, "10", 3), Z(2, "20", 2)).Should().Equal((1, 2m));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(0)]
    [InlineData(-1)]
    public void UeberdeckungOderNichtPositiv_Abgelehnt_AK7(decimal ist)
        => FluentActions.Invoking(() => V(ist, Z(1, "10", 3), Z(2, "20", 2))).Should().Throw<InvalidOperationException>()
            .WithMessage(ist > 0 ? "*größer als offenes Soll 5*" : "*größer als 0*");

    [Fact]
    public void Positionsreihenfolge_Natuerlich_OhnePositionZuletzt_H1()
        => V(4m, Z(1, null, 1), Z(2, "10.1", 1), Z(3, "10", 1), Z(4, "9", 1)).Select(a => a.Id).Should().Equal(4, 3, 2, 1);
}
