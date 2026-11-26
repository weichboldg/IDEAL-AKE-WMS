using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.73 (Spec 2026-11-25-bde-masken-vereinheitlichung, Etappe 1): AG-Status je Token, Kinder als Zeilen, Filter trifft Kinder.</summary>
public class BdeTeilelisteAgStatusTests
{
    private static readonly DateTime Heute = new(2026, 11, 25);
    private static readonly Dictionary<string, int> KeineOffsets = new(StringComparer.OrdinalIgnoreCase);
    private static DateTime Rechner(DateTime d, int o) => d.AddDays(o);

    private static FaHierarchyNode Node(int vater, int sub, int pos, string artnr, string matchcode, string? schritte) =>
        new() { HauptFA = 100, VaterFA = vater, SubFA = sub, Position = pos, Artnr = artnr, Matchcode = matchcode, Arbeitsschritte = schritte, Sollmenge = 1 };

    private static (FaHierarchyStructure S, FaHierarchyTreeNode T) Struktur(FaHierarchyNode n, DateTime? feTermin, params FaArbeitsschrittSymbol[] symbole)
    {
        var t = new FaHierarchyTreeNode { Node = n, Arbeitsgaenge = symbole.ToList() };
        var s = new FaHierarchyStructure { HauptFA = n.HauptFA, Roots = { t }, OrderInfos = { new FaHierarchyOrderInfo { HauptFA = n.HauptFA, FE_Termin = feTermin } } };
        return (s, t);
    }

    [Fact]
    public void Teilmeldung_OhneWo_Erledigt()
    {
        var n = Node(200, 0, 1, "B1", "Blech", "KA LA");
        var (s, t) = Struktur(n, Heute.AddDays(5),
            new FaArbeitsschrittSymbol("KA", "Kanten") { Status = FaAgStatus.KeinBdeArbeitsgang },
            new FaArbeitsschrittSymbol("LA", "Laser") { Status = FaAgStatus.Laeuft });
        var gemeldet = new HashSet<(TeilSchluessel, string)> { (TeilSchluessel.Von(n), "KA"), (TeilSchluessel.Von(n), "LA") };

        BdeTeilelisteService.TeilmeldungenAnwenden(new[] { s }, gemeldet, new HashSet<string>(), KeineOffsets, Rechner, Heute);

        t.Arbeitsgaenge.Select(a => a.Status).Should().Equal(FaAgStatus.Erledigt, FaAgStatus.Erledigt);
    }

    [Fact]
    public void KeinWo_TeilelisteKuerzel_OffenSonstNeutral()
    {
        var n = Node(200, 0, 1, "B1", "Blech", "ka LA ST");
        var (s, t) = Struktur(n, Heute.AddDays(-3),
            new FaArbeitsschrittSymbol("ka", null) { Status = FaAgStatus.KeinBdeArbeitsgang },
            new FaArbeitsschrittSymbol("LA", null) { Status = FaAgStatus.KeinBdeArbeitsgang },
            new FaArbeitsschrittSymbol("ST", null));

        BdeTeilelisteService.TeilmeldungenAnwenden(new[] { s }, new HashSet<(TeilSchluessel, string)>(), new HashSet<string> { "KA" },
            KeineOffsets, Rechner, Heute);

        t.Arbeitsgaenge[0].Status.Should().Be(FaAgStatus.Ueberfaellig, "Teilmeldung moeglich, Soll ueberschritten");
        t.Arbeitsgaenge[0].Soll.Should().Be(Heute.AddDays(-3));
        t.Arbeitsgaenge[1].Status.Should().Be(FaAgStatus.KeinBdeArbeitsgang, "weder WO noch Teilmeldung moeglich");
        t.Arbeitsgaenge[2].Status.Should().BeNull("neutral bleibt neutral ohne Teileliste-Kuerzel");
    }

    private static readonly Dictionary<string, bool> KeineAuftraege = new();

    private static FaHierarchyNode[] Baugruppe() => new[]
    {
        Node(100, 200, 1, "BG", "Baugruppe", "KA"),
        Node(200, 0, 1, "K1", "Kind eins", "LA"),
        Node(200, 0, 2, "K2", "Kind zwei", "KA"),
    };

    [Fact]
    public void Kinder_SindZeilenMitSymbolen()
    {
        var nodes = Baugruppe();
        var status = nodes.ToDictionary(n => n, n => (IReadOnlyList<FaArbeitsschrittSymbol>)FaArbeitsschrittSymbole.Tokenize(n.Arbeitsschritte)
            .Select(k => new FaArbeitsschrittSymbol(k, null) { Status = FaAgStatus.Offen }).ToList());

        var d = BdeTeilelisteService.Bauen(nodes, Array.Empty<FaHierarchyOrderInfo>(), "KA", Array.Empty<BdeTeilMeldung>(), KeineAuftraege,
            null, false, null, agStatus: status);

        var bg = d.Zeilen.Single(z => z.Node.Matchcode == "Baugruppe");
        d.Zeilen.Select(z => z.Node.Matchcode).Should().Equal(new[] { "Baugruppe", "Kind zwei" }, "Kind zwei traegt KA selbst");
        bg.Kinder.Select(k => k.Node.Matchcode).Should().Equal("Kind eins", "Kind zwei");
        bg.Kinder[0].Arbeitsgaenge.Single().Status.Should().Be(FaAgStatus.Offen);
        bg.Aufgeklappt.Should().BeFalse();
        bg.Kinder.Should().OnlyContain(k => !k.Treffer);
        BdeTeilelisteService.SpaltenZeile(new Dictionary<int, VormontageService.KopfWerte>(), null, null)["ag-status"](bg)
            .Should().Be("KA – offen");
    }

    [Fact]
    public void Filter_KindTreffer_BehaeltElternAufgeklappt()
    {
        var colf = new Dictionary<string, string> { ["matchcode"] = "Kind eins" };

        var d = BdeTeilelisteService.Bauen(Baugruppe(), Array.Empty<FaHierarchyOrderInfo>(), "KA", Array.Empty<BdeTeilMeldung>(), KeineAuftraege,
            null, false, colf);

        var bg = d.Zeilen.Should().ContainSingle().Which;
        bg.Node.Matchcode.Should().Be("Baugruppe");
        bg.Aufgeklappt.Should().BeTrue();
        // v1.99.81 (Task 4b): nur das passende Kind bleibt, KinderGesamt fuer „1 von 2“
        bg.Kinder.Should().ContainSingle().Which.Node.Matchcode.Should().Be("Kind eins");
        bg.Kinder.Single().Treffer.Should().BeTrue();
        bg.KinderGesamt.Should().Be(2);
    }

    // v1.99.81 (Task 4b, Gerald: Arbeitsbereich OG-01 an Baugruppe → darunter auch nur OG-01): Eltern passen, Kinder werden trotzdem gefiltert
    [Fact]
    public void Filter_ElternTreffer_NurPassendeKinder_NichtAufgeklappt()
    {
        var d = BdeTeilelisteService.Bauen(Baugruppe(), Array.Empty<FaHierarchyOrderInfo>(), "KA", Array.Empty<BdeTeilMeldung>(), KeineAuftraege,
            null, false, new Dictionary<string, string> { ["arbeitsschritte"] = "KA" });

        var bg = d.Zeilen.Single(z => z.Node.Matchcode == "Baugruppe");
        bg.Kinder.Should().ContainSingle().Which.Node.Matchcode.Should().Be("Kind zwei", "Kind eins hat nur LA");
        bg.KinderGesamt.Should().Be(2);
        bg.Aufgeklappt.Should().BeFalse("die Baugruppe passt selbst, Kinder sind keine Treffer");
    }

    [Fact]
    public void Filter_ElternTreffer_KeinKindPasst_KinderLeer()
    {
        var d = BdeTeilelisteService.Bauen(Baugruppe(), Array.Empty<FaHierarchyOrderInfo>(), "KA", Array.Empty<BdeTeilMeldung>(), KeineAuftraege,
            null, false, new Dictionary<string, string> { ["matchcode"] = "Baugruppe" });

        var bg = d.Zeilen.Should().ContainSingle().Which;
        bg.Kinder.Should().BeEmpty();
        bg.KinderGesamt.Should().Be(2, "die View meldet „0 von 2 passen zum Filter“");
    }

    // QA v1.99.81: Haupt-FA-Filter trifft Eltern und alle Kinder (gleicher Haupt-FA) – alle Teile bleiben, keine Treffer-Markierung, zugeklappt
    [Fact]
    public void Filter_HauptFa_AlleKinderBleiben_KeinTreffer()
    {
        var d = BdeTeilelisteService.Bauen(Baugruppe(), Array.Empty<FaHierarchyOrderInfo>(), "KA", Array.Empty<BdeTeilMeldung>(), KeineAuftraege,
            null, false, new Dictionary<string, string> { ["hauptfa"] = "100" });

        var bg = d.Zeilen.Single(z => z.Node.Matchcode == "Baugruppe");
        bg.Kinder.Should().HaveCount(2).And.OnlyContain(k => !k.Treffer);
        bg.KinderGesamt.Should().Be(2);
        bg.Aufgeklappt.Should().BeFalse();
    }
}
