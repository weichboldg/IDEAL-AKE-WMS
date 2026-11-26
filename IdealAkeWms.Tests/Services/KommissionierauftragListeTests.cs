using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.89 (Spec 2026-11-26 Teil 3, K4/K4a–c/K11, H-1/H-2): Summieren der Maske „Liste“ des Kommissionierauftrags.</summary>
public class KommissionierauftragListeTests
{
    private static PickingOrderLine Z(int id, string art, string? pos, decimal soll, PickingOrderLineStatus status = PickingOrderLineStatus.Offen,
        decimal? ist = null, bool bg = false) => new()
    {
        Id = id, ArticleNumber = art, BomPosition = pos, SollMenge = soll, Status = status, IstMenge = ist, IsBaugruppe = bg,
        CreatedBy = "t", CreatedByWindows = "t",
    };

    private static BomItemViewModel I(int? zeile, string art, string? pos, string? ziel = "K-01", string? hlp = "H1", bool bg = false,
        string? arbeitsbereich = null) => new()
    {
        PickingItemId = zeile, Ressourcenummer = art, Position = pos, Kommissionieren = ziel, Hauptlagerplatz = hlp, IsBaugruppe = bg,
        Arbeitsbereich = arbeitsbereich, Bezeichnung1 = "Bez " + art,
    };

    private static List<AuftragSummenzeile> Summe(IEnumerable<BomItemViewModel> items, IEnumerable<PickingOrderLine> zeilen,
        IEnumerable<PickingOrderLine>? ohne = null, bool offen = true)
        => KommissionierauftragListe.Summiere(items, zeilen, ohne ?? Array.Empty<PickingOrderLine>(), "Montage 1", offen);

    [Fact]
    public void Schluessel_GrossKleinLeerzeichen_Summiert_PositionenNatuerlich()
    {
        var zeilen = new[] { Z(1, "a1", "10", 3), Z(2, "A1 ", "9", 2) };
        var s = Summe(new[] { I(1, "a1", "10"), I(2, "A1 ", "9", ziel: " K-01 ") }, zeilen).Should().ContainSingle().Subject;

        s.Ressourcenummer.Should().Be("A1");
        s.Kommissionieren.Should().Be("K-01");
        s.SollSumme.Should().Be(5m);
        s.OffenSoll.Should().Be(5m);
        s.PositionenText.Should().Be("9, 10");
        s.Abteilung.Should().Be("Montage 1");
        s.Erledigt.Should().BeFalse();
        s.GebuchtIst.Should().BeNull();
    }

    [Fact]
    public void BaugruppeUndZiel_GetrennteZeilen_ArbeitsbereichMehrwertig()
    {
        var zeilen = new[] { Z(1, "A", "1", 1), Z(2, "A", "2", 1, bg: true), Z(3, "A", "3", 1), Z(4, "A", "4", 1) };
        var r = Summe(new[]
        {
            I(1, "A", "1", arbeitsbereich: "Schweissen"), I(2, "A", "2", bg: true), I(3, "A", "3", ziel: "K-02"),
            I(4, "A", "4", arbeitsbereich: "Biegen"),
        }, zeilen);

        r.Should().HaveCount(3);
        r.Single(s => s.Kommissionieren == "K-01" && !s.IsBaugruppe).Arbeitsbereich.Should().Be("Biegen, Schweissen");
        r.Should().ContainSingle(s => s.IsBaugruppe);
    }

    [Fact]
    public void Sortierung_ZielHauptlagerplatzPositionArtikel_LeereZuletzt()
    {
        var zeilen = new[] { Z(1, "B", "5", 1), Z(2, "A", "5", 1), Z(3, "C", null, 1), Z(4, "D", "1", 1), Z(5, "E", "2", 1), Z(6, "F", "10", 1) };
        var r = Summe(new[]
        {
            I(1, "B", "5"), I(2, "A", "5"),                // gleiches Ziel, Platz, Position -> Artikel
            I(3, "C", null),                               // leere Position zuletzt im Ziel/Platz
            I(4, "D", "1", ziel: null),                    // leeres Ziel ganz hinten
            I(5, "E", "2", hlp: null),                     // leerer Hauptlagerplatz nach H1
            I(6, "F", "10"),
        }, zeilen);

        r.Select(s => s.Ressourcenummer).Should().Equal("A", "B", "F", "C", "E", "D");
    }

    [Fact]
    public void GemischterStatus_GebuchtUndRestHinweis_K4b()
    {
        var zeilen = new[] { Z(1, "A", "10", 3, PickingOrderLineStatus.Umgebucht, ist: 2), Z(2, "A", "20", 2) };
        var s = Summe(new[] { I(1, "A", "10"), I(2, "A", "20") }, zeilen).Single();

        s.Erledigt.Should().BeFalse();
        s.OffenSoll.Should().Be(2m);
        s.GebuchtIst.Should().Be(2m);
        s.StatusText.Should().Be("teilweise umgebucht (1 von 2 Pos.)");
        s.RestHinweis.Should().Be("Rest 1 aus Pos. 10 geht beim Abschluss in den Folgeauftrag");

        Summe(new[] { I(1, "A", "10"), I(2, "A", "20") }, zeilen, offen: false).Single().RestHinweis
            .Should().BeNull("abgeschlossener Auftrag: der Rest liegt schon im Folgeauftrag");
    }

    [Fact]
    public void AlleErledigt_StatusText()
    {
        var zeilen = new[] { Z(1, "A", "1", 1, PickingOrderLineStatus.Umgebucht, ist: 1), Z(2, "A", "2", 1, PickingOrderLineStatus.Umgebucht, ist: 1) };
        var s = Summe(new[] { I(1, "A", "1"), I(2, "A", "2") }, zeilen).Single();
        s.Erledigt.Should().BeTrue();
        s.StatusText.Should().Be("gepickt");
        s.GebuchtIst.Should().Be(2m);
        s.RestHinweis.Should().BeNull();
    }

    [Fact]
    public void DoppelteZeilenId_NurEinmalGezaehlt_H2()
    {
        var zeilen = new[] { Z(1, "A", "1", 4) };
        var s = Summe(new[] { I(1, "A", "1"), I(1, "A", "1") }, zeilen).Single();
        s.SollSumme.Should().Be(4m);
        s.Zeilen.Should().ContainSingle();
    }

    [Fact]
    public void ZeileOhneStuecklistenposition_EigeneSummenzeile_K4c()
    {
        var zeilen = new List<PickingOrderLine> { Z(1, "A", "1", 1), Z(2, "X", "7", 2) };
        var items = new[] { I(1, "A", "1"), I(null, "Y", "8") };
        var ohne = KommissionierauftragListe.OhneStuecklistenposition(zeilen, items);
        ohne.Select(z => z.Id).Should().Equal(2);

        var r = Summe(items, zeilen, ohne);
        var x = r.Single(s => s.Ressourcenummer == "X");
        x.OhneStuecklistenposition.Should().BeTrue();
        x.Kommissionieren.Should().BeNull();
        x.Bezeichnung1.Should().BeNull();
        x.SollSumme.Should().Be(2m);
        x.PositionenText.Should().Be("7");
        r.Last().Should().BeSameAs(x, "leeres Ziel zuletzt");
    }

    [Fact]
    public void NachReihenfolge_ErsteGenanntePosition_FehlendeAmEnde()
    {
        var zeilen = new[] { Z(1, "A", "1", 1), Z(2, "A", "5", 1), Z(3, "B", "2", 1), Z(4, "C", "3", 1) };
        var r = Summe(new[] { I(1, "A", "1"), I(2, "A", "5"), I(3, "B", "2"), I(4, "C", "3") }, zeilen);

        KommissionierauftragListe.NachReihenfolge(r, "2,5,1").Select(s => s.Ressourcenummer).Should().Equal("B", "A", "C");
        KommissionierauftragListe.NachReihenfolge(r, null).Should().Equal(r);
    }
}
