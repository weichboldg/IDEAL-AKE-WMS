using FluentAssertions;
using IdealAkeWms.Services;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.20 Spec 2026-11-08-einkauf-bestellvorschlaege F2/F3 (Rueckfragen 11, 13).</summary>
public class PurchaseProposalCalculatorTests
{
    private static PurchaseProposalResult R(decimal bestand, decimal meldebestand, decimal offen = 0m, decimal bedarfe = 0m, decimal? l = null,
        string? anderer = null) => PurchaseProposalCalculator.Berechne(new PurchaseProposalInput(bestand, offen, bedarfe, meldebestand, l, anderer));

    [Fact]
    public void UeberMeldebestand_KeinVorschlag()
    {
        var r = R(86m, 85m);
        r.IstVorschlag.Should().BeFalse();
        r.Dringlichkeit.Should().Be(PurchaseProposalDringlichkeit.KeinBedarf);
        r.Vorschlag.Should().BeNull();
        r.Verfuegbar.Should().Be(86m);
    }

    [Fact]
    public void Projektion_OffenBestelltPlus_BedarfeMinus_GleichMeldebestandIstVorschlag()
    {
        var r = R(80m, 85m, offen: 10m, bedarfe: 5m);
        r.Verfuegbar.Should().Be(85m);
        r.Dringlichkeit.Should().Be(PurchaseProposalDringlichkeit.Nachbestellen);
    }

    [Fact]
    public void Kritisch_NurBeiVerfuegbarUnterNull()
    {
        R(0m, 85m).Dringlichkeit.Should().Be(PurchaseProposalDringlichkeit.Nachbestellen);
        R(3m, 85m, bedarfe: 5m).Dringlichkeit.Should().Be(PurchaseProposalDringlichkeit.Kritisch);
    }

    [Fact]
    public void OhneLetzteBestellung_BisUeberMeldebestand_F6()
    {
        var r = R(60m, 85m);
        r.Vorschlag.Should().Be(26m);             // M − v + 1
        r.Fehlmenge.Should().Be(25m);
        r.Grund.Should().Be("keine letzte Bestellung – Menge prüfen");
        R(85m, 85m).Vorschlag.Should().Be(1m);    // genau am Meldebestand: eine Lagereinheit darueber
    }

    /// <summary>v1.99.30 F6 Mengenregel A (Antwort 3, AK 13): L reicht → L; reicht nicht (v + L ≤ M) → M − v + 1; nie ein Vielfaches von L (EK015744).</summary>
    [Theory]
    [InlineData(60, 400, 400, "letzte Bestellmenge")]                                   // 60 + 400 > 85: L reicht
    [InlineData(-315, 400, 401, "letzte Bestellmenge reicht nicht – bis über Meldebestand")]   // alt 800
    [InlineData(-915, 400, 1001, "letzte Bestellmenge reicht nicht – bis über Meldebestand")]  // alt 1200
    [InlineData(65, 20, 21, "letzte Bestellmenge reicht nicht – bis über Meldebestand")]       // 65 + 20 = 85 ≤ M: reicht nicht
    public void Mengenregel_A_AK13(decimal bestand, decimal l, decimal erwartet, string grund)
    {
        var r = R(bestand, 85m, l: l);
        (r.Vorschlag, r.Grund).Should().Be(((decimal?)erwartet, grund));
    }

    [Theory]
    [InlineData(null, 2.5, 4)]     // ohne Dezimalstellen: ganze Einheit, aufgerundet
    [InlineData(2, 2.5, 3.5)]
    [InlineData(1, 2.55, 3.5)]     // 3,45 aufwaerts auf 3,5
    public void Mengenregel_A_AufDezimalstellenLagerAufgerundet(int? dez, decimal verfuegbar, decimal erwartet) =>
        PurchaseProposalCalculator.Berechne(new PurchaseProposalInput(verfuegbar, 0m, 0m, 5m, DezimalstellenLager: dez)).Vorschlag.Should().Be(erwartet);

    [Fact]
    public void Mengenregel_A_MindestbestellmengeGroesser_EK000051() =>
        // L 200, MBM 300 (gleiche Einheit, Faktor 1): bleibt 300 (Till: „so lassen“)
        PurchaseProposalCalculator.Berechne(new PurchaseProposalInput(60m, 0m, 0m, 85m, 200m, null, 300m, "Stk", 1m, "Stk")).Vorschlag.Should().Be(300m);

    private static PurchaseProposalResult M(decimal mindest, string? ek, decimal? faktor, string? unit = "Stk", decimal? l = null) =>
        PurchaseProposalCalculator.Berechne(new PurchaseProposalInput(60m, 0m, 0m, 85m, l, null, mindest, ek, faktor, unit));

    [Fact]
    public void Mindestbestellmenge_GleicheEinheitFaktor1_Aufgerundet_E4()
    {
        var r = M(200m, "stk ", 1m);
        r.Vorschlag.Should().Be(200m);
        r.Grund.Should().Be("keine letzte Bestellung – Menge prüfen, auf Mindestbestellmenge aufgerundet");
        M(200m, null, 1m).Vorschlag.Should().Be(200m);          // EK-Einheit leer
        M(10m, "Stk", 1m).Vorschlag.Should().Be(26m);           // schon groesser: keine Rundung
        M(10m, "Stk", 1m).Grund.Should().NotContain("Mindestbestellmenge");
        M(500m, "Stk", 1m, l: 400m).Vorschlag.Should().Be(500m);
    }

    [Theory]
    [InlineData("PKG", 3)]     // EK002400: andere Einheit
    [InlineData("Stk", 5)]     // EK007623: gleiche Einheit, Faktor ≠ 1 (A2)
    [InlineData("Stk", 0)]     // EK003375: Faktor 0
    public void Mindestbestellmenge_EinheitOderFaktorAbweichend_KeineRundung_Hinweis_A2(string ek, decimal faktor)
    {
        var r = M(200m, ek, faktor);
        r.Vorschlag.Should().Be(26m);
        r.Hinweise.Should().ContainSingle().Which.Should().Contain("nicht aufgerundet");
    }

    [Fact]
    public void Mindestbestellmenge_FaktorUnbekannt_KeineRundung() => M(200m, "Stk", null).Vorschlag.Should().Be(26m);

    [Fact]
    public void LetzteBestellungAndererLieferant_Hinweis_Rueckfrage13a()
    {
        R(60m, 85m, l: 50m, anderer: "Muster GmbH").Hinweise.Should().Equal("letzte Bestellung bei Muster GmbH");
        R(60m, 85m, l: 50m).Hinweise.Should().BeEmpty();
    }

    [Fact]
    public void Fallback_SageBestellmenge_NurOhneL_MengenregelA_Hinweis_Nachtrag2()
    {
        static PurchaseProposalResult F(decimal bestand, decimal? l, decimal? sage) =>
            PurchaseProposalCalculator.Berechne(new PurchaseProposalInput(bestand, 0m, 0m, 85m, l, FallbackSageBestellmenge: sage));
        var r = F(60m, null, 100m);
        (r.Vorschlag, r.Grund, r.Fallback).Should().Be(((decimal?)100m, PurchaseProposalCalculator.GrundSageBestellmenge, true));
        r.Hinweise.Should().Equal(PurchaseProposalCalculator.HinweisFallback);
        PurchaseProposalCalculator.HinweisFallback.Should().Be("Keine Bestellung in 3 Jahren – Sage-Bestellmenge");
        var hoch = F(0m, null, 40m); // 0 + 40 ≤ 85 → M − v + 1 = 86 (alt 3 × 40 = 120)
        (hoch.Vorschlag, hoch.Grund).Should().Be(((decimal?)86m, PurchaseProposalCalculator.GrundSageBestellmenge + " " + PurchaseProposalCalculator.GrundBisUeberMeldebestand));
        F(60m, 50m, 100m).Should().Match<PurchaseProposalResult>(x => x.Vorschlag == 50m && !x.Fallback, "L hat Vorrang");
        F(60m, null, 0m).Should().Match<PurchaseProposalResult>(x => x.Vorschlag == 26m && !x.Fallback && x.Grund == PurchaseProposalCalculator.GrundOhneLetzteBestellung);
    }
}
