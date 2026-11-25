using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.74 (Spec 2026-11-25-bde-masken-vereinheitlichung, Etappe 2): Summiert mit Einzelauftraegen und aggregierter Ampel.</summary>
public class BdeTeilelisteSummiertTests
{
    private static readonly DateTime Heute = new(2026, 11, 25);

    private static TeilelisteZeile Zeile(int haupt, int pos, params FaArbeitsschrittSymbol[] symbole)
    {
        var n = new FaHierarchyNode { HauptFA = haupt, VaterFA = 200, SubFA = 0, Position = pos, Artnr = "B1", Matchcode = "Blech", Arbeitsschritte = "KA", Sollmenge = 2 };
        return new TeilelisteZeile { Node = n, Schluessel = TeilSchluessel.Von(n), Arbeitsgaenge = symbole };
    }

    private static FaArbeitsschrittSymbol Ag(string k, FaAgStatus? s, DateTime? soll = null) => new(k, "Kanten") { Status = s, Soll = soll };

    private static FaHierarchyOrderInfo Info(int haupt, DateTime? fe) => new() { HauptFA = haupt, FE_Termin = fe };

    [Fact]
    public void Einzelzeilen_NachFeTerminAufsteigend_OhneTerminZuletzt()
    {
        var zeilen = new[] { Zeile(101, 1), Zeile(102, 1), Zeile(103, 1), Zeile(104, 1) };
        var infos = new[] { Info(101, Heute.AddDays(9)), Info(102, Heute.AddDays(2)), Info(103, null), Info(104, Heute.AddDays(5)) };

        var a = BdeTeilelisteService.Summieren(zeilen, infos, null).Single();

        a.Einzelzeilen.Select(z => z.Node.HauptFA).Should().Equal(102, 104, 101, 103);
    }

    [Fact]
    public void Ampel_EinerUeberfaellig_EinerErledigt_Ueberfaellig()
    {
        var zeilen = new[]
        {
            Zeile(101, 1, Ag("KA", FaAgStatus.Ueberfaellig, Heute.AddDays(-2))),
            Zeile(102, 1, Ag("ka", FaAgStatus.Erledigt, Heute.AddDays(-5))),
        };

        var ag = BdeTeilelisteService.Summieren(zeilen, Array.Empty<FaHierarchyOrderInfo>(), null, heute: Heute).Single().Arbeitsgaenge.Single();

        ag.Status.Should().Be(FaAgStatus.Ueberfaellig);
        ag.Soll.Should().Be(Heute.AddDays(-2), "fruehester Soll der nicht erledigten");
        ag.Kuerzel.Should().Be("KA");
    }

    [Fact]
    public void Ampel_AlleErledigt_Erledigt()
    {
        var zeilen = new[] { Zeile(101, 1, Ag("KA", FaAgStatus.Erledigt)), Zeile(102, 1, Ag("KA", FaAgStatus.Erledigt)) };

        BdeTeilelisteService.Summieren(zeilen, Array.Empty<FaHierarchyOrderInfo>(), null, heute: Heute).Single()
            .Arbeitsgaenge.Single().Status.Should().Be(FaAgStatus.Erledigt);
    }

    [Fact]
    public void Ampel_LaeuftUndUeberfaellig_LaeuftUeberfaellig_JeKuerzel()
    {
        var zeilen = new[]
        {
            Zeile(101, 1, Ag("KA", FaAgStatus.Laeuft, Heute.AddDays(3)), Ag("LA", FaAgStatus.Offen, Heute.AddDays(4))),
            Zeile(102, 1, Ag("KA", FaAgStatus.Ueberfaellig, Heute.AddDays(-1)), Ag("LA", FaAgStatus.KeinBdeArbeitsgang)),
        };

        var ags = BdeTeilelisteService.Summieren(zeilen, Array.Empty<FaHierarchyOrderInfo>(), null, heute: Heute).Single().Arbeitsgaenge;

        ags.Select(a => a.Kuerzel).Should().Equal("KA", "LA");
        ags[0].Status.Should().Be(FaAgStatus.LaeuftUeberfaellig);
        ags[1].Status.Should().Be(FaAgStatus.Offen, "kein BDE-Arbeitsgang zaehlt nicht mit");
    }

    [Fact]
    public void Ampel_NurNeutral_BleibtNeutral()
    {
        var zeilen = new[] { Zeile(101, 1, Ag("KA", null)), Zeile(102, 1, Ag("KA", FaAgStatus.KeinBdeArbeitsgang)) };

        BdeTeilelisteService.Summieren(zeilen, Array.Empty<FaHierarchyOrderInfo>(), null, heute: Heute).Single()
            .Arbeitsgaenge.Single().Status.Should().Be(FaAgStatus.KeinBdeArbeitsgang);
    }
}
