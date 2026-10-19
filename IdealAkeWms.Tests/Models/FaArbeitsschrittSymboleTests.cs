using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Models;

/// <summary>
/// Arbeitsgänge als Symbole, Teil 1 (Spec 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1, v1.52.0): Quelle S =
/// Sage-Kürzel aus <see cref="FaHierarchyNode.Arbeitsschritte"/> der Zeile selbst, tokenisiert wie die Struktur-Erkennung
/// (nur Leerzeichen, leere Teile weg, getrimmt), Feld-Reihenfolge, Ausschlussliste NICHT gefiltert; Werkbank-Name nur bei
/// eindeutigem Katalog-Treffer.
/// </summary>
public class FaArbeitsschrittSymboleTests
{
    [Fact]
    public void Tokenize_FeldReihenfolge_AK11()
        => FaArbeitsschrittSymbole.Tokenize("LS EG KA PG").Should().Equal("LS", "EG", "KA", "PG");

    [Theory]
    [InlineData("  LS   KA  ")]
    [InlineData("LS  KA")]
    public void Tokenize_DoppelteUndRandLeerzeichen_KeineLeerenSymbole(string feld)
        => FaArbeitsschrittSymbole.Tokenize(feld).Should().Equal("LS", "KA");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Tokenize_LeerOderNull_KeineSymbole(string? feld)
        => FaArbeitsschrittSymbole.Tokenize(feld).Should().BeEmpty();

    [Fact]
    public void Tokenize_AusschlusslisteUndUnbekanntBleiben_AK12()
        => FaArbeitsschrittSymbole.Tokenize("STO KA XXX ZZ9").Should().Equal("STO", "KA", "XXX", "ZZ9");

    [Fact]
    public void Tokenize_DoppeltesKuerzel_BleibtWieSage()
        => FaArbeitsschrittSymbole.Tokenize("KA LS KA").Should().Equal("KA", "LS", "KA");

    private static ProductionWorkplace Wp(string? code, string name) => new() { ArbeitsschrittCode = code, Name = name };

    [Fact]
    public void NamenLookup_TrimUndCaseInsensitiv()
    {
        var lookup = FaArbeitsschrittSymbole.NamenLookup(new[] { Wp(" ka ", "Kantenanleimen"), Wp(null, "ohne Code"), Wp("", "leer") });

        lookup["KA"].Should().Be("Kantenanleimen");
        lookup.Should().ContainSingle();
    }

    [Fact]
    public void NamenLookup_Mehrdeutig_OhneNamen()
        => FaArbeitsschrittSymbole.NamenLookup(new[] { Wp("PG", "Packen 1"), Wp("pg", "Packen 2"), Wp("LS", "Laser") })
            .Keys.Should().BeEquivalentTo(new[] { "LS" }, "ein Kuerzel auf mehreren Werkbaenken bekommt keinen erfundenen Namen");

    // v1.63.0 (FA-b10, AK 17): eine gekennzeichnete Werkbank zaehlt nicht — EG hat wieder genau einen Namen.
    [Fact]
    public void NamenLookup_GekennzeichneteWerkbankZaehltNicht()
    {
        var alt = Wp("EG", "Flächen entgr beendet");
        alt.SageMissingSince = new DateTime(2026, 10, 19);
        FaArbeitsschrittSymbole.NamenLookup(new[] { alt, Wp("EG", "Entgraten Fläche") })["EG"].Should().Be("Entgraten Fläche");
    }

    [Fact]
    public void Zuordnen_JeZeileNurEigeneKuerzel_AuchBlatt_AK13()
    {
        // Sub-FA "Korpus" (PG) mit Blatt-Kind (LS KA) und Kind-Sub-FA (VM); Wurzel ohne Arbeitsschritte.
        FaHierarchyTreeNode T(int subFa, string? schritte, params FaHierarchyTreeNode[] kinder) =>
            new() { Node = new FaHierarchyNode { SubFA = subFa, Arbeitsschritte = schritte }, Children = kinder.ToList() };
        var blatt = T(0, "LS KA");
        var kindSubFa = T(30, "VM");
        var korpus = T(20, "PG", blatt, kindSubFa);
        var wurzel = T(10, null, korpus);
        var structure = new FaHierarchyStructure { Roots = new List<FaHierarchyTreeNode> { wurzel } };

        FaArbeitsschrittSymbole.Zuordnen(new[] { structure },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["KA"] = "Kantenanleimen" });

        wurzel.Arbeitsgaenge.Should().BeEmpty();
        korpus.Arbeitsgaenge.Select(a => a.Kuerzel).Should().Equal("PG");
        kindSubFa.Arbeitsgaenge.Select(a => a.Kuerzel).Should().Equal("VM");
        blatt.Arbeitsgaenge.Should().Equal(new FaArbeitsschrittSymbol("LS", null), new FaArbeitsschrittSymbol("KA", "Kantenanleimen"));
    }

    [Theory]
    [InlineData("Kantenanleimen", "KA – Kantenanleimen")]
    [InlineData(null, "KA")]
    public void Beschriftung_NameOderKuerzel(string? name, string erwartet)
        => new FaArbeitsschrittSymbol("KA", name).Beschriftung.Should().Be(erwartet);
}
