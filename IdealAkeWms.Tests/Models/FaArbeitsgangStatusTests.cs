using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Models;

/// <summary>
/// Arbeitsgänge als Symbole, Teil 2 (Spec 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2, v1.55.0): reine Status-Ableitung
/// — Zuordnung FA 9 a / W-2 / W-3, Werkbank-Gate FA 6, Zusammenfassung FA 7b / W-4, Soll-Termin aus frühestem FE_Termin +
/// Offset-Lookup (Werktage über den echten <see cref="OseonDueDateCalculator"/>).
/// </summary>
public class FaArbeitsgangStatusTests
{
    private const int Werkbank = 7, InaktiveWerkbank = 8;
    private static readonly DateTime Fe = new(2026, 4, 30); // Do; 01.05. Feiertag → Offset 1 = Mo 04.05. (AK 2)
    private static readonly HashSet<DateTime> Feiertage = new() { new DateTime(2026, 5, 1) };
    private static readonly Func<DateTime, int, DateTime> Soll =
        (d, o) => OseonDueDateCalculator.Calculate(d, o, new BusinessDayService(), Feiertage);

    private static FaHierarchyTreeNode N(int subFa, int? vaterFa, string? schritte, params FaHierarchyTreeNode[] kinder) => new()
    {
        Node = new FaHierarchyNode { HauptFA = 100, SubFA = subFa, VaterFA = vaterFa, Arbeitsschritte = schritte },
        Children = kinder.ToList()
    };

    private static FaHierarchyStructure S(FaHierarchyTreeNode root, params DateTime?[] feTermine)
    {
        var s = new FaHierarchyStructure { HauptFA = 100, Roots = { root } };
        foreach (var fe in feTermine.DefaultIfEmpty(Fe)) s.OrderInfos.Add(new FaHierarchyOrderInfo { HauptFA = 100, FE_Termin = fe });
        FaArbeitsschrittSymbole.Zuordnen(new[] { s }, new Dictionary<string, string>());
        return s;
    }

    private static FaStatusWorkOperation Op(int id, int sub, string kuerzel, int? werkbank = Werkbank, string order = "100")
        => new(id, order, sub.ToString(), kuerzel, werkbank);

    private static FaAgStatusErgebnis Run(FaHierarchyStructure s, IEnumerable<FaStatusWorkOperation> ops, DateTime heute,
        int[]? erledigt = null, int[]? laeuft = null, Dictionary<string, int>? offsets = null)
        => FaArbeitsgangStatus.Zuordnen(new[] { s }, ops, new HashSet<int> { Werkbank },
            new FaBdeStatus(new HashSet<int>(erledigt ?? Array.Empty<int>()), new HashSet<int>(laeuft ?? Array.Empty<int>())),
            offsets ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["KA"] = 1 }, Soll, heute);

    private static FaArbeitsschrittSymbol Sym(FaHierarchyStructure s, int subFa, string? schritte = null)
    {
        IEnumerable<FaHierarchyTreeNode> Flat(FaHierarchyTreeNode t) => new[] { t }.Concat(t.Children.SelectMany(Flat));
        return Flat(s.Roots[0]).First(t => t.Node.SubFA == subFa && (schritte == null || t.Node.Arbeitsschritte == schritte)).Arbeitsgaenge[0];
    }

    // ===== AK 10 / AK 2 / AK 3 / AK 5: Status-Tabelle =====

    [Theory]
    //          erledigt laeuft heute(Mai)  erwartet
    [InlineData(true,  false, 5, FaAgStatus.Erledigt)]            // erledigt + Soll gestern → ohne Zusatz
    [InlineData(true,  true,  5, FaAgStatus.Erledigt)]            // erledigt vor laeuft
    [InlineData(false, true,  4, FaAgStatus.Laeuft)]              // Soll heute = nicht ueberfaellig
    [InlineData(false, true,  5, FaAgStatus.LaeuftUeberfaellig)]  // Antwort 10
    [InlineData(false, false, 5, FaAgStatus.Ueberfaellig)]        // AK 2: heute 05.05.
    [InlineData(false, false, 4, FaAgStatus.Offen)]               // AK 2: heute 04.05.
    public void Status_Tabelle_AK10(bool erledigt, bool laeuft, int heuteMai, FaAgStatus erwartet)
    {
        var s = S(N(101, null, "KA"));
        Run(s, new[] { Op(1, 101, "KA") }, new DateTime(2026, 5, heuteMai, 15, 0, 0),
            erledigt ? new[] { 1 } : null, laeuft ? new[] { 1 } : null);

        var sym = Sym(s, 101);
        sym.Status.Should().Be(erwartet);
        sym.Soll.Should().Be(new DateTime(2026, 5, 4));
    }

    [Fact]
    public void Texte_AriaLabel_BeschriftungPlusStatus()
    {
        var s = S(N(101, null, "KA"));
        Run(s, new[] { Op(1, 101, "KA") }, new DateTime(2026, 5, 5), laeuft: new[] { 1 });
        (Sym(s, 101) with { Name = "Kanten" }).AriaLabel.Should().Be("KA – Kanten – läuft, überfällig (Soll 04.05.2026)");
        new FaArbeitsschrittSymbol("STO", null) { Status = FaAgStatus.KeinBdeArbeitsgang }.AriaLabel.Should().Be("STO – kein BDE-Arbeitsgang");
        new FaArbeitsschrittSymbol("KA", "Kanten").AriaLabel.Should().Be("KA – Kanten", "ohne Status wie Teil 1");
    }

    [Fact]
    public void OhneFeTermin_OderOhneKopfzeile_NieUeberfaellig_AK5()
    {
        var s = S(N(101, null, "KA"), (DateTime?)null);
        Run(s, new[] { Op(1, 101, "KA") }, new DateTime(2030, 1, 1));
        Sym(s, 101).Status.Should().Be(FaAgStatus.Offen);
        Sym(s, 101).Soll.Should().BeNull();

        var ohneKopf = S(N(101, null, "KA"));
        ohneKopf.OrderInfos.Clear();
        Run(ohneKopf, new[] { Op(1, 101, "KA") }, new DateTime(2030, 1, 1));
        Sym(ohneKopf, 101).Status.Should().Be(FaAgStatus.Offen);
    }

    // ===== AK 6: Werkbank-Gate =====

    [Fact]
    public void WerkbankNichtAktiv_OderOhneWerkbank_Neutral_GemischteStruktur_AK6()
    {
        var s = S(N(101, null, "KA SW PG"));
        Run(s, new[] { Op(1, 101, "KA"), Op(2, 101, "SW", InaktiveWerkbank), Op(3, 101, "PG", werkbank: null) }, new DateTime(2026, 5, 4));

        var syms = s.Roots[0].Arbeitsgaenge;
        syms[0].Status.Should().Be(FaAgStatus.Offen);
        syms[1].Status.Should().BeNull();
        syms[2].Status.Should().BeNull("Werkbank ohne Zuordnung gilt als nicht BDE-aktiv");
    }

    // ===== AK 10a: Offset-Aufloesung + Sammelhinweis =====

    [Fact]
    public void Offset_CaseInsensitiv_FehlendInListe_NullOhneHinweis_AK10a()
    {
        var s = S(N(101, null, "ka SW PG"));
        var offsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["KA"] = 1, ["PG"] = 0 };
        var r = Run(s, new[] { Op(1, 101, "KA"), Op(2, 101, "SW"), Op(3, 101, "PG") }, new DateTime(2026, 5, 4), offsets: offsets);

        var syms = s.Roots[0].Arbeitsgaenge;
        syms[0].Soll.Should().Be(new DateTime(2026, 5, 4), "ka trifft Eintrag KA (Offset 1)");
        syms[1].Soll.Should().Be(Fe, "ohne Eintrag Offset 0");
        syms[2].Soll.Should().Be(Fe);
        r.NichtKonfiguriert.Should().Equal("SW");
    }

    [Fact]
    public void Sammelhinweis_NurFuerKuerzelMitStatus_JedesEinmal_AK10c()
    {
        var s = S(N(101, null, "SW SW XX", N(0, 101, "SW")));
        var r = Run(s, new[] { Op(1, 101, "SW"), Op(2, 101, "XX", InaktiveWerkbank) }, new DateTime(2026, 5, 4));
        r.NichtKonfiguriert.Should().Equal(new[] { "SW" }, "XX ist neutral (Werkbank inaktiv) und zaehlt nicht");
    }

    // ===== AK 10b: Kombigeraet =====

    [Theory]
    [InlineData("2026-05-10", "2026-05-05", "2026-05-05")]
    [InlineData(null, "2026-05-07", "2026-05-07")]
    [InlineData(null, null, null)]
    public void Kombigeraet_FruehesterNichtLeererFeTermin_AK10b(string? a, string? b, string? bezug)
    {
        DateTime? P(string? x) => x == null ? null : DateTime.Parse(x);
        var s = S(N(101, null, "SW"), P(a), P(b));
        Run(s, new[] { Op(1, 101, "SW") }, new DateTime(2030, 1, 1));
        Sym(s, 101).Soll.Should().Be(P(bezug));
        Sym(s, 101).Status.Should().Be(bezug == null ? FaAgStatus.Offen : FaAgStatus.Ueberfaellig);
    }

    // ===== AK 11: Zuordnung =====

    [Fact]
    public void Zuordnung_EigenerAuftrag_BlattZumVater_WurzelOhneSubFa_FremdeHauptFa_AK11()
    {
        // Wurzel 101 (KA) → Sub-FA 102 (KA) mit Blatt (ka); Vater 101 hat ebenfalls KA.
        var s = S(N(101, null, "KA", N(102, 101, "KA", N(0, 102, "ka")), N(0, 101, "KA")));
        Run(s, new[] { Op(1, 101, "KA"), Op(2, 102, "KA"), Op(3, 102, "KA", order: "999") }, new DateTime(2026, 5, 4),
            erledigt: new[] { 1 }, laeuft: new[] { 2, 3 });

        Sym(s, 102).Status.Should().Be(FaAgStatus.Laeuft, "eigener Auftrag 102, nicht der Vater 101; fremde HauptFA 999 zaehlt nicht doppelt");
        Sym(s, 0, "ka").Status.Should().Be(FaAgStatus.Laeuft, "Blatt → Auftrag des VaterFA 102, case-insensitiv");
        Sym(s, 0, "KA").Status.Should().Be(FaAgStatus.Erledigt, "Blatt unter der Wurzel → Wurzel-Auftrag 101");

        var wurzelOhneSub = S(N(0, null, "KA"));
        Run(wurzelOhneSub, new[] { Op(1, 0, "KA") }, new DateTime(2026, 5, 4));
        wurzelOhneSub.Roots[0].Arbeitsgaenge[0].Status.Should().Be(FaAgStatus.KeinBdeArbeitsgang);

        var fremd = S(N(101, null, "KA"));
        Run(fremd, new[] { Op(1, 101, "KA", order: "999") }, new DateTime(2026, 5, 4));
        Sym(fremd, 101).Status.Should().Be(FaAgStatus.KeinBdeArbeitsgang);
    }

    // ===== AK 12 / AK 14: ohne WorkOperation / WorkOperation ohne Kuerzel =====

    [Fact]
    public void KuerzelOhneWorkOperation_KeinBde_WorkOperationOhneKuerzel_KeinSymbol_AK12_AK14()
    {
        var s = S(N(101, null, "KA STO"));
        Run(s, new[] { Op(1, 101, "01"), Op(2, 101, "KA") }, new DateTime(2030, 1, 1));

        var syms = s.Roots[0].Arbeitsgaenge;
        syms.Select(x => x.Kuerzel).Should().Equal("KA", "STO");
        syms[1].Status.Should().Be(FaAgStatus.KeinBdeArbeitsgang);
        syms[1].Soll.Should().BeNull("nie ueberfaellig");
    }

    // ===== AK 15: W-2, W-3, W-4 =====

    [Fact]
    public void W2_SubFaOhneEigeneWorkOperation_KeinVaterRueckfall()
    {
        var s = S(N(101, null, "KA", N(102, 101, "KA")));
        Run(s, new[] { Op(1, 101, "KA") }, new DateTime(2026, 5, 4), erledigt: new[] { 1 });
        Sym(s, 102).Status.Should().Be(FaAgStatus.KeinBdeArbeitsgang);
    }

    [Fact]
    public void W3_WaisenBlatt_AlteVaterWorkOperationWirdNichtVerwendet()
    {
        // Waisen-Pseudowurzel: Blatt mit VaterFA 555, kein Knoten 555 in der Struktur, aber Auftrag 555 mit KA existiert.
        var s = S(N(0, 555, "KA"));
        Run(s, new[] { Op(1, 555, "KA") }, new DateTime(2026, 5, 4));
        s.Roots[0].Arbeitsgaenge[0].Status.Should().Be(FaAgStatus.KeinBdeArbeitsgang);
    }

    [Theory]
    //          e1     l1     e2     l2     heute  erwartet
    [InlineData(false, true,  false, false, 4, FaAgStatus.Laeuft)]
    [InlineData(true,  false, false, false, 4, FaAgStatus.Offen)]
    [InlineData(true,  false, false, false, 5, FaAgStatus.Ueberfaellig)]
    [InlineData(true,  false, true,  false, 5, FaAgStatus.Erledigt)]
    [InlineData(false, true,  true,  false, 4, FaAgStatus.Laeuft)]
    [InlineData(false, true,  false, false, 5, FaAgStatus.LaeuftUeberfaellig)]
    public void W4_Zusammenfassung(bool e1, bool l1, bool e2, bool l2, int heuteMai, FaAgStatus erwartet)
    {
        var s = S(N(101, null, "KA"));
        int[] Ids(bool a, bool b) => new[] { a ? 1 : 0, b ? 2 : 0 }.Where(i => i > 0).ToArray();
        var r = Run(s, new[] { Op(1, 101, "KA"), Op(2, 101, "ka") }, new DateTime(2026, 5, heuteMai), Ids(e1, e2), Ids(l1, l2));

        Sym(s, 101).Status.Should().Be(erwartet);
        r.Mehrfach.Should().ContainSingle().Which.Should().Contain("100/101").And.Contain("KA").And.Contain("2×").And.Contain("1, 2");
    }

    [Fact]
    public void W4_NurAktiveWerkbankZaehlt_KeinMehrfach_OhneDoppel_AK17()
    {
        var s = S(N(101, null, "KA", N(0, 101, "KA")));
        var r = Run(s, new[] { Op(1, 101, "KA"), Op(2, 101, "KA", InaktiveWerkbank) }, new DateTime(2026, 5, 4), erledigt: new[] { 1 });
        Sym(s, 101).Status.Should().Be(FaAgStatus.Erledigt, "nur die aktive zaehlt");
        r.Mehrfach.Should().ContainSingle("Blatt und Vater zeigen dasselbe Paar — einmal je Seitenaufbau");

        var einfach = S(N(101, null, "KA"));
        Run(einfach, new[] { Op(1, 101, "KA") }, new DateTime(2026, 5, 4)).Mehrfach.Should().BeEmpty();
    }
}
