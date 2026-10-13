using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// FA-Struktur Teil 3 (Spec 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3, v1.57.0): Fertigungstermin-Regel (FA 3, Antworten
/// 7/9), Sortierung (FA 4), Status je Arbeitsgang ueber die gemeinsame <see cref="ArbeitsgangStatusRegel"/> (AK 3, 4, 6, 13, 14).
/// </summary>
public class BdeTerminalAuftraegeTests
{
    private static readonly HashSet<DateTime> Feiertage = new() { new DateTime(2026, 5, 1) };
    private static readonly Func<DateTime, int, DateTime> Rechner =
        (d, o) => OseonDueDateCalculator.Calculate(d, o, new BusinessDayService(), Feiertage);
    private static readonly Dictionary<string, int> Offsets = new(StringComparer.OrdinalIgnoreCase) { ["KA"] = 1 };
    private static readonly ILookup<int, FaHierarchyOrderInfo> KeinKopf = Array.Empty<FaHierarchyOrderInfo>().ToLookup(o => o.HauptFA);

    private static int _id;
    private static ProductionOrder Po(string order, DateTime? productionDate = null, string? sub = null)
        => new() { Id = ++_id, OrderNumber = order, SubOrderNumber = sub, ProductionDate = productionDate };
    private static WorkOperation Op(ProductionOrder po, string kuerzel, int seq = 10, string name = "Kanten")
        => new() { Id = ++_id, ProductionOrder = po, ProductionOrderId = po.Id, OperationNumber = kuerzel, Name = name, Sequence = seq };
    private static ILookup<int, FaHierarchyOrderInfo> Kopf(int hauptFa, params DateTime?[] fe)
        => fe.Select(f => new FaHierarchyOrderInfo { HauptFA = hauptFa, FE_Termin = f }).ToLookup(o => o.HauptFA);

    // ===== AK 4: Fertigungstermin =====

    [Fact]
    public void Termin_EineKopfzeile_FeTermin_Kombigeraet_Fruehester()
    {
        BdeTerminalAuftraege.Fertigungstermin(Po("100", new DateTime(2026, 1, 1)), Kopf(100, new DateTime(2026, 5, 10)))
            .Should().Be(new DateTime(2026, 5, 10), "FE_Termin geht vor ProductionDate");
        BdeTerminalAuftraege.Fertigungstermin(Po("100"), Kopf(100, new DateTime(2026, 5, 10), new DateTime(2026, 5, 5), null))
            .Should().Be(new DateTime(2026, 5, 5));
    }

    [Fact]
    public void Termin_OhneKopfzeile_Ake_NurLeereFeTermin_RueckfallProductionDate_SonstOhneTermin()
    {
        var pd = new DateTime(2026, 4, 30);
        BdeTerminalAuftraege.Fertigungstermin(Po("100", pd), KeinKopf).Should().Be(pd, "IDEAL ohne Kopfzeile");
        BdeTerminalAuftraege.Fertigungstermin(Po("AKE-4711", pd), KeinKopf).Should().Be(pd, "AKE: nicht numerisch, ProductionDate (Antwort 7)");
        BdeTerminalAuftraege.Fertigungstermin(Po("100", pd), Kopf(100, null, null)).Should().Be(pd, "Antwort 9");
        BdeTerminalAuftraege.Fertigungstermin(Po("100"), Kopf(100, (DateTime?)null)).Should().BeNull("ohne Termin");
    }

    // ===== AK 3: Sortierung =====

    [Fact]
    public void Sortierung_TerminAufsteigend_OhneTerminAmEnde_GleichstandOrderSub_InnerhalbSequence()
    {
        var spaet = Po("300", new DateTime(2026, 6, 1));
        var frueh = Po("200", new DateTime(2026, 5, 1, 14, 0, 0));
        var gleichA = Po("100", new DateTime(2026, 5, 1), "101");
        var gleichB = Po("100", new DateTime(2026, 5, 1), "100");
        var ohne = Po("050");
        var ops = new[]
        {
            Op(spaet, "SW"), Op(ohne, "KA"), Op(frueh, "PG", 20), Op(frueh, "LS", 10), Op(gleichA, "KA"), Op(gleichB, "KA")
        };

        var r = BdeTerminalAuftraege.Ordnen(ops, new HashSet<int>(), KeinKopf, Offsets, Rechner, new DateTime(2026, 4, 1));

        r.Select(a => $"{a.Op.ProductionOrder.OrderNumber}/{a.Op.ProductionOrder.SubOrderNumber}/{a.Op.OperationNumber}")
            .Should().Equal("100/100/KA", "100/101/KA", "200//LS", "200//PG", "300//SW", "050//KA");
    }

    // ===== AK 6 / AK 5 / Antwort 10: Status =====

    [Theory]
    //          laeuft heute(Mai) erwartet
    [InlineData(false, 4, FaAgStatus.Offen)]               // Soll = heute → nicht ueberfaellig
    [InlineData(false, 5, FaAgStatus.Ueberfaellig)]
    [InlineData(true, 4, FaAgStatus.Laeuft)]
    [InlineData(true, 5, FaAgStatus.LaeuftUeberfaellig)]
    public void Status_SollTerminPlusOffsetWerktage_Feiertag_AK6(bool laeuft, int heuteMai, FaAgStatus erwartet)
    {
        var op = Op(Po("AKE-1", new DateTime(2026, 4, 30)), "ka");
        var r = BdeTerminalAuftraege.Ordnen(new[] { op }, laeuft ? new HashSet<int> { op.Id } : new HashSet<int>(),
            KeinKopf, Offsets, Rechner, new DateTime(2026, 5, heuteMai, 9, 0, 0)).Single();

        r.Symbol.Soll.Should().Be(new DateTime(2026, 5, 4), "ka trifft Eintrag KA (Offset 1), 01.05. Feiertag");
        r.Symbol.Status.Should().Be(erwartet);
    }

    [Fact]
    public void Status_OhneOffsetEintrag_SollGleichTermin_OhneTermin_NieUeberfaellig_AK14()
    {
        var pd = new DateTime(2026, 4, 30);
        var ake01 = Op(Po("AKE-2", pd), "01");
        var ohne = Op(Po("AKE-3"), "KA");
        var r = BdeTerminalAuftraege.Ordnen(new[] { ake01, ohne }, new HashSet<int> { ohne.Id }, KeinKopf, Offsets, Rechner,
            new DateTime(2030, 1, 1));

        r[0].Symbol.Soll.Should().Be(pd);
        r[0].Symbol.Status.Should().Be(FaAgStatus.Ueberfaellig);
        r[1].Symbol.Status.Should().Be(FaAgStatus.Laeuft, "ohne Termin nie ueberfaellig, auch nicht bei laeuft");
    }

    [Fact]
    public void ZweiArbeitsgaengeGleichesKuerzel_ZweiKnoepfe_EigenerStatus_KeinW4_AK13()
    {
        var po = Po("AKE-4", new DateTime(2026, 6, 1));
        var a = Op(po, "KA", 10);
        var b = Op(po, "ka", 20);
        var r = BdeTerminalAuftraege.Ordnen(new[] { a, b }, new HashSet<int> { a.Id }, KeinKopf, Offsets, Rechner, new DateTime(2026, 5, 1));

        r.Select(x => x.Symbol.Status).Should().Equal(FaAgStatus.Laeuft, FaAgStatus.Offen);
    }

    [Fact]
    public void AriaLabel_KuerzelNameStatus_AK5()
    {
        var op = Op(Po("AKE-5", new DateTime(2026, 4, 30)), " KA ", name: "Kanten");
        var r = BdeTerminalAuftraege.Ordnen(new[] { op }, new HashSet<int> { op.Id }, KeinKopf, Offsets, Rechner, new DateTime(2026, 5, 5)).Single();

        r.Symbol.AriaLabel.Should().Be("KA – Kanten – läuft, überfällig (Soll 04.05.2026)");
        FaAgStatusDarstellung.Fuer(r.Symbol.Status).Css.Should().Be("fa-ag-laeuft fa-ag-laeuft-ueberfaellig");
    }

    [Fact]
    public void Regel_ErledigtVorLaeuft_EinzigerOrt()
        => ArbeitsgangStatusRegel.Ableiten(true, true, new DateTime(2020, 1, 1), new DateTime(2026, 1, 1)).Should().Be(FaAgStatus.Erledigt);
}
