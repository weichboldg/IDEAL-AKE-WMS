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

    [Fact]
    public void LetzteBestellungAndererLieferant_Hinweis_Rueckfrage13a()
    {
        R(60m, 85m, l: 50m, anderer: "Muster GmbH").Hinweise.Should().Equal("letzte Bestellung bei Muster GmbH");
        R(60m, 85m, l: 50m).Hinweise.Should().BeEmpty();
    }
}
