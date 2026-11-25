using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Xunit;
using static IdealAkeWms.Services.PickingOrderRestmengen;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.70 (Spec 2026-11-25-kommissionierauftraege-nachtrag-nachts F8, TS-191): „Stückliste kleiner als offene Kommissionierzeilen“ –
/// eine Regel <see cref="PickingOrderRestmengen.Ueberdeckt"/> fuer Nachtlauf, Leitstand-Marker und Stuecklisten-Hinweis.</summary>
public class PickingOrderUeberdeckungTests
{
    private static Soll S(string art, string pos, decimal menge) => new(art, pos, menge, false, null);

    private static PickingOrderLine Z(string art, string pos, decimal soll, PickingOrderLineStatus status = PickingOrderLineStatus.Offen,
        decimal? ist = null, PickingOrderStatus kopf = PickingOrderStatus.Offen) =>
        new() { ArticleNumber = art, BomPosition = pos, SollMenge = soll, IstMenge = ist, Status = status, PickingOrder = new PickingOrder { Status = kopf } };

    [Fact]
    public void MengeGesenkt_OffeneZeile_Treffer()
    {
        var u = Ueberdeckt(new[] { S("A1", "10", 2m) }, new[] { Z("A1", "10", 5m) });
        u.Should().Equal(new Ueberdeckung("A1", "10", 2m, 5m));
        Text(u).Should().Be("A1 Pos 10: Stückliste 2, beauftragt 5");
    }

    [Fact]
    public void PositionEntfallen_SollNull_Treffer()
    {
        Ueberdeckt(Array.Empty<Soll>(), new[] { Z("A1", "10", 1.5m, PickingOrderLineStatus.Gepickt) })
            .Should().Equal(new Ueberdeckung("A1", "10", 0m, 1.5m));
    }

    [Fact]
    public void OhneOffeneZeile_KeinTreffer()
    {
        // umgebucht + uebertragen: nichts mehr zu tun → keine Warnung
        Ueberdeckt(new[] { S("A1", "10", 1m) }, new[]
        {
            Z("A1", "10", 5m, PickingOrderLineStatus.Umgebucht, ist: 5m),
            Z("A1", "10", 3m, PickingOrderLineStatus.Uebertragen),
        }).Should().BeEmpty();
    }

    [Fact]
    public void VollstaendigAbgeschlossen_UndOffenerRestPasst_KeinTreffer()
    {
        // Auftrag 1 „vollstaendig“ (zaehlt Soll 3), offene Zeile 2 → beauftragt 5 = Soll 5
        Ueberdeckt(new[] { S("A1", "10", 5m) }, new[]
        {
            Z("A1", "10", 3m, PickingOrderLineStatus.Umgebucht, ist: 1m, kopf: PickingOrderStatus.Abgeschlossen),
            Z("A1", "10", 2m),
        }).Should().BeEmpty();
    }

    [Fact]
    public void DoppelterSchluessel_SollSummiert_GrossKleinUndLeerzeichenEgal()
    {
        Ueberdeckt(new[] { S("A1", "10", 1m), S("a1 ", "10", 1m) }, new[] { Z("A1", "10", 2m) }).Should().BeEmpty();
        Ueberdeckt(new[] { S("A1", "10", 1m), S("a1 ", "10", 1m) }, new[] { Z("A1", "10", 3m) }).Single().Soll.Should().Be(2m);
    }

    [Fact]
    public void Text_Leer_Null_Und_Gekuerzt()
    {
        Text(new List<Ueberdeckung>()).Should().BeNull();
        var viele = Enumerable.Range(1, 100).Select(i => new Ueberdeckung($"ARTIKEL{i:000}", $"{i}", 0m, 1m)).ToList();
        var t = Text(viele)!;
        t.Length.Should().Be(TextMaxLaenge);
        t.Should().EndWith("…");
    }
}
