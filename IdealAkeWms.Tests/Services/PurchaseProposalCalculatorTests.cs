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
    public void OhneLetzteBestellung_Fehlmenge_Bei0Meldebestand_Rueckfrage13b()
    {
        var r = R(60m, 85m);
        r.Vorschlag.Should().Be(25m);
        r.Fehlmenge.Should().Be(25m);
        r.Grund.Should().Be("keine letzte Bestellung – Menge prüfen");
        R(85m, 85m).Vorschlag.Should().Be(85m);
    }

    [Fact]
    public void LetzteBestellmenge_Regelfall_UndVielfachesAbFehlmengeGleichL_Rueckfrage11()
    {
        var r = R(60m, 85m, l: 400m);
        r.Vorschlag.Should().Be(400m);
        r.Grund.Should().Be("letzte Bestellmenge");

        // F = 400 = L → (⌊400/400⌋ + 1) × 400 = 800
        var gleich = R(-315m, 85m, l: 400m);
        gleich.Vorschlag.Should().Be(800m);
        gleich.Grund.Should().Be("2 × letzte Bestellmenge");

        // F = 1000, L = 400 → (2 + 1) × 400 = 1200
        R(-915m, 85m, l: 400m).Vorschlag.Should().Be(1200m);
    }

    private static PurchaseProposalResult M(decimal mindest, string? ek, decimal? faktor, string? unit = "Stk", decimal? l = null) =>
        PurchaseProposalCalculator.Berechne(new PurchaseProposalInput(60m, 0m, 0m, 85m, l, null, mindest, ek, faktor, unit));

    [Fact]
    public void Mindestbestellmenge_GleicheEinheitFaktor1_Aufgerundet_E4()
    {
        var r = M(200m, "stk ", 1m);
        r.Vorschlag.Should().Be(200m);
        r.Grund.Should().Be("keine letzte Bestellung – Menge prüfen, auf Mindestbestellmenge aufgerundet");
        M(200m, null, 1m).Vorschlag.Should().Be(200m);          // EK-Einheit leer
        M(10m, "Stk", 1m).Vorschlag.Should().Be(25m);           // schon groesser: keine Rundung
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
        r.Vorschlag.Should().Be(25m);
        r.Hinweise.Should().ContainSingle().Which.Should().Contain("nicht aufgerundet");
    }

    [Fact]
    public void Mindestbestellmenge_FaktorUnbekannt_KeineRundung() => M(200m, "Stk", null).Vorschlag.Should().Be(25m);

    [Fact]
    public void LetzteBestellungAndererLieferant_Hinweis_Rueckfrage13a()
    {
        R(60m, 85m, l: 50m, anderer: "Muster GmbH").Hinweise.Should().Equal("letzte Bestellung bei Muster GmbH");
        R(60m, 85m, l: 50m).Hinweise.Should().BeEmpty();
    }

    [Fact]
    public void Fallback_SageBestellmenge_NurOhneL_MitErhoehungUndHinweis_Nachtrag2()
    {
        static PurchaseProposalResult F(decimal bestand, decimal? l, decimal? sage) =>
            PurchaseProposalCalculator.Berechne(new PurchaseProposalInput(bestand, 0m, 0m, 85m, l, FallbackSageBestellmenge: sage));
        var r = F(60m, null, 100m);
        (r.Vorschlag, r.Grund, r.Fallback).Should().Be(((decimal?)100m, PurchaseProposalCalculator.GrundSageBestellmenge, true));
        r.Hinweise.Should().Equal(PurchaseProposalCalculator.HinweisFallback);
        PurchaseProposalCalculator.HinweisFallback.Should().Be("Fallback: Sage-Bestellmenge (keine Bestellung in 3 Jahren)");
        var hoch = F(0m, null, 40m); // F = 85 -> (⌊85/40⌋+1)×40 = 120
        (hoch.Vorschlag, hoch.Grund).Should().Be(((decimal?)120m, "3 × " + PurchaseProposalCalculator.GrundSageBestellmenge));
        F(60m, 50m, 100m).Should().Match<PurchaseProposalResult>(x => x.Vorschlag == 50m && !x.Fallback, "L hat Vorrang");
        F(60m, null, 0m).Should().Match<PurchaseProposalResult>(x => x.Vorschlag == 25m && !x.Fallback && x.Grund == PurchaseProposalCalculator.GrundOhneLetzteBestellung);
    }
}
