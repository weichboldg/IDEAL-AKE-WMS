using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.Beschichtung;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>Spec 2026-11-16-beschichtungslogik-je-standort E1 (AK 4, 5, 6, 8, 13, 17): Matrix Master × Kennzeichen-Quelle × Termin-Quelle.</summary>
public class BeschichtungResolverTests
{
    private static readonly BusinessDayService Days = new();
    private static readonly HashSet<DayOfWeek> DiDo = Days.ParsePickupDays("Dienstag,Donnerstag");
    private static readonly HashSet<DateTime> KeineFeiertage = new();

    private static BeschichtungKontext Kontext(bool hierarchisch, string? kennzeichen = null, string? termin = null,
        bool kategorieGesetzt = true, int tage = 10) =>
        new(BeschichtungResolver.ResolveQuellen(kennzeichen, termin, hierarchisch), kategorieGesetzt, tage, KeineFeiertage, DiDo, Days);

    [Theory]
    [InlineData(false, null, null, BeschichtungKennzeichenQuelle.Artikelkategorie, BeschichtungTerminQuelle.Berechnet)]
    [InlineData(false, "Automatisch", "automatisch", BeschichtungKennzeichenQuelle.Artikelkategorie, BeschichtungTerminQuelle.Berechnet)]
    [InlineData(true, null, null, BeschichtungKennzeichenQuelle.FaStruktur, BeschichtungTerminQuelle.View)]
    [InlineData(true, "Automatisch", "Automatisch", BeschichtungKennzeichenQuelle.FaStruktur, BeschichtungTerminQuelle.View)]
    [InlineData(true, " FaStruktur ", "Berechnet", BeschichtungKennzeichenQuelle.FaStruktur, BeschichtungTerminQuelle.Berechnet)]
    [InlineData(false, "artikelkategorie", "Berechnet", BeschichtungKennzeichenQuelle.Artikelkategorie, BeschichtungTerminQuelle.Berechnet)]
    [InlineData(true, "", "", BeschichtungKennzeichenQuelle.FaStruktur, BeschichtungTerminQuelle.View)]
    public void ResolveQuellen_GueltigeWerte_OhneWarnung(bool hierarchisch, string? k, string? t,
        BeschichtungKennzeichenQuelle erwK, BeschichtungTerminQuelle erwT)
    {
        var q = BeschichtungResolver.ResolveQuellen(k, t, hierarchisch);

        (q.Kennzeichen, q.Termin).Should().Be((erwK, erwT));
        q.Warnungen.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, "FaStruktur", null, BeschichtungKennzeichenQuelle.Artikelkategorie, BeschichtungTerminQuelle.Berechnet)]
    [InlineData(true, "Artikelkategorie", null, BeschichtungKennzeichenQuelle.FaStruktur, BeschichtungTerminQuelle.View)]
    [InlineData(false, null, "View", BeschichtungKennzeichenQuelle.Artikelkategorie, BeschichtungTerminQuelle.Berechnet)]
    [InlineData(false, "SData", null, BeschichtungKennzeichenQuelle.Artikelkategorie, BeschichtungTerminQuelle.Berechnet)]
    [InlineData(true, null, "SData", BeschichtungKennzeichenQuelle.FaStruktur, BeschichtungTerminQuelle.View)]
    [InlineData(true, "1", null, BeschichtungKennzeichenQuelle.FaStruktur, BeschichtungTerminQuelle.View)]
    public void ResolveQuellen_UngueltigOderUnpassend_WieAutomatisch_MitWarnung_A8(bool hierarchisch, string? k, string? t,
        BeschichtungKennzeichenQuelle erwK, BeschichtungTerminQuelle erwT)
    {
        var q = BeschichtungResolver.ResolveQuellen(k, t, hierarchisch);

        (q.Kennzeichen, q.Termin).Should().Be((erwK, erwT));
        q.Warnungen.Should().ContainSingle().Which.Should().Contain("Automatisch");
    }

    [Fact]
    public void Berechnet_Flach_IdentischZurAltenInlineFormel_AK5()
    {
        // Alte Inline-Formel der FA-Liste (ProductionOrdersController, flacher Zweig) und CoatingDateCalculator von Leitstand/Arbeitsliste.
        var vk = new DateTime(2026, 12, 18); // Freitag
        var raw = Days.SubtractBusinessDays(vk, 10, KeineFeiertage);
        var alt = Days.FindPreviousPickupDay(raw, DiDo);

        var info = BeschichtungResolver.Bestimmen(Kontext(false), hasCoatingParts: true, vk, null);

        info.Termin.Should().Be(alt);
        info.TerminQuelle.Should().Be(BeschichtungTerminQuelle.Berechnet);
        info.Beschichtet.Should().BeTrue();
        info.Aktiv.Should().BeTrue();
    }

    [Fact]
    public void Berechnet_Flach_KategorieLeer_TerminFuerAlle_AktivFalse()
    {
        // fallstricke: LackierteilKategorieName leer = Termin fuer alle (heutiges Verhalten).
        var info = BeschichtungResolver.Bestimmen(Kontext(false, kategorieGesetzt: false), hasCoatingParts: false, new DateTime(2026, 12, 18), null);

        info.Aktiv.Should().BeFalse();
        info.Termin.Should().NotBeNull();
    }

    [Fact]
    public void Berechnet_Flach_KategorieGesetzt_OhneLack_KeinTermin()
    {
        var info = BeschichtungResolver.Bestimmen(Kontext(false), hasCoatingParts: false, new DateTime(2026, 12, 18), null);

        info.Termin.Should().BeNull();
        info.Aktiv.Should().BeTrue();
    }

    [Fact]
    public void Berechnet_Hierarchisch_AusKopfFeTermin_AK8()
    {
        // Der Aufrufer rechnet den Vorkommissioniertermin aus dem Kopf-FE_Termin (wie die FA-Liste); der Resolver nimmt ihn als Basis.
        var vk = new DateTime(2026, 12, 18);
        var kopf = new FaHierarchyOrderInfo { HauptFA = 1, FE_Termin = new DateTime(2026, 12, 24), Start_Beschichtung = new DateTime(2026, 1, 1) };

        var info = BeschichtungResolver.Bestimmen(Kontext(true, termin: "Berechnet"), hasCoatingParts: true, vk, kopf);

        info.Termin.Should().Be(Days.FindPreviousPickupDay(Days.SubtractBusinessDays(vk, 10, KeineFeiertage), DiDo));
        info.TerminQuelle.Should().Be(BeschichtungTerminQuelle.Berechnet);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void View_EindeutigerKopf_StartBeschichtungDatumsgleich_OhneWmsBerechnung_AK6(int tage)
    {
        // Mittwoch = kein Abholtag: weder BeschichtungTage noch der Abholtag duerfen den Wert verschieben (Freigabe-Antwort 4).
        var kopf = new FaHierarchyOrderInfo { HauptFA = 1, Start_Beschichtung = new DateTime(2026, 12, 2) };

        var info = BeschichtungResolver.Bestimmen(Kontext(true, tage: tage), hasCoatingParts: true, new DateTime(2026, 12, 18), kopf);

        info.Termin.Should().Be(new DateTime(2026, 12, 2));
        info.TerminQuelle.Should().Be(BeschichtungTerminQuelle.View);
    }

    [Fact]
    public void View_Kombigeraet_KeinTermin()
    {
        var info = BeschichtungResolver.Bestimmen(Kontext(true), hasCoatingParts: true, new DateTime(2026, 12, 18), eindeutigerKopf: null);

        info.Termin.Should().BeNull("kein Ersatztermin (TS-71.11b)");
    }

    [Fact]
    public void View_FeldLeer_KeinErsatztermin()
    {
        var kopf = new FaHierarchyOrderInfo { HauptFA = 1, FE_Termin = new DateTime(2026, 12, 24), Start_Beschichtung = null };

        var info = BeschichtungResolver.Bestimmen(Kontext(true), hasCoatingParts: true, new DateTime(2026, 12, 18), kopf);

        info.Termin.Should().BeNull();
    }

    [Fact]
    public void View_OhneLackFlag_KeinTermin()
    {
        var kopf = new FaHierarchyOrderInfo { HauptFA = 1, Start_Beschichtung = new DateTime(2026, 12, 2) };

        BeschichtungResolver.Bestimmen(Kontext(true), hasCoatingParts: false, null, kopf).Termin.Should().BeNull();
    }

    [Fact]
    public void View_AnDerAke_UnpassendWirktWieBerechnet()
    {
        var k = Kontext(false, termin: "View");

        k.Quellen.Termin.Should().Be(BeschichtungTerminQuelle.Berechnet);
        k.Quellen.Warnungen.Should().ContainSingle();
        BeschichtungResolver.Bestimmen(k, true, new DateTime(2026, 12, 18), null).Termin.Should().NotBeNull();
    }

    [Theory]
    [InlineData(true, false, true)]   // FaStruktur: immer aktiv, unabhaengig von LackierteilKategorieName (AK 13)
    [InlineData(true, true, true)]
    [InlineData(false, true, true)]   // Artikelkategorie: aktiv = Kategorie gesetzt (A6)
    [InlineData(false, false, false)]
    public void Aktiv_JeQuelle(bool hierarchisch, bool kategorieGesetzt, bool aktiv)
    {
        BeschichtungResolver.Bestimmen(Kontext(hierarchisch, kategorieGesetzt: kategorieGesetzt), true, null, null)
            .Aktiv.Should().Be(aktiv);
    }
}
