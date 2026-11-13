using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>v1.99.10 (Spec 2026-11-12-offene-bestellungen-mahnmail-lieferverzug AK 2, 3, 5, 7, 9–14, 17, 18): Planer, HTML, Lauf, Pickup.
/// Keine Mail verlaesst den Test: Fake-Mailservice bzw. Pickup-Ordner im Temp-Verzeichnis.</summary>
public class LieferverzugMailTests
{
    private static readonly DateTime Jetzt = new(2026, 11, 12, 8, 0, 0); // Donnerstag
    private static readonly DateTime Heute = Jetzt.Date;

    /// <summary>Defaults wie v1.99.10 (AK 11 der Nachbesserung): Kreditor-Schwelle = Sammelliste, keine Altersgrenze.</summary>
    private static LieferverzugEinstellungen E(bool test = true, bool kreditor = true, int max = 30, bool einkauf = true, string[]? einkaufAn = null,
        int tage = 3, int kreditorTage = 3, int maxAlter = 0, bool mitAbrufen = false) =>
        new(einkauf, einkaufAn ?? new[] { "einkauf@test.invalid" }, kreditor, test, tage, 7, 7, max, "einkauf@test.invalid", null, "AKE", "https://wms.test",
            kreditorTage, maxAlter, mitAbrufen);

    private static SagePurchaseOrderLine L(int belPosId, int? terminTageZurueck, string kto = "70001", int? creditorId = 1, decimal offen = 5m,
        bool gesperrt = false, bool abweichend = false) => new()
    {
        BelPosId = belPosId, BelId = belPosId / 10, Belegnummer = $"2027-{belPosId}", LieferantKto = kto, LieferantName = "Sage " + kto,
        CreditorId = creditorId, Artikelnummer = "EK" + belPosId, Bezeichnung1 = "Teil <b>" + belPosId, MengeBestellt = 10m, MengeGeliefert = 10m - offen,
        MengeOffen = offen, MengeBasiseinheit = "Stk", EinheitAbweichend = abweichend, LieferantGesperrt = gesperrt,
        Liefertermin = terminTageZurueck is { } t ? Heute.AddDays(-t) : null, SyncedAt = Jetzt.AddHours(-1),
    };

    private static Dictionary<int, LieferverzugKreditor> K(params LieferverzugKreditor[] k) => k.ToDictionary(x => x.Id);
    private static LieferverzugKreditor Kr(int id = 1, string? mail = "lieferant@test.invalid", string? reminder = null, bool aus = false, bool aktiv = true) =>
        new(id, "Lieferant " + id, mail, reminder, aus, aktiv);
    private static Dictionary<int, decimal> Rest(params SagePurchaseOrderLine[] l) => l.ToDictionary(x => x.BelPosId, x => x.MengeOffen);
    private static readonly Dictionary<(int, DateTime?), DateTime> KeineMail = new();

    private static LieferverzugPlan Plan(SagePurchaseOrderLine[] l, Dictionary<int, LieferverzugKreditor> k, LieferverzugEinstellungen e,
        Dictionary<(int, DateTime?), DateTime>? letzte = null, DateTime? stand = null) =>
        LieferverzugPlaner.Planen(l, Rest(l), k, letzte ?? KeineMail, e, Jetzt, stand ?? Jetzt.AddHours(-1));

    // ---------------------------------------------------------------- Planer

    [Fact] // AK 2, 3
    public void Faelligkeit_AbTerminPlusN_OhneTerminUndRestNullNie()
    {
        var l = new[] { L(1, 2), L(2, 3), L(3, 10), L(4, null), L(5, 10, offen: 0m) };
        var plan = Plan(l, K(Kr()), E());
        plan.Gruppen.SelectMany(g => g.Positionen).Select(p => p.Line.BelPosId).Should().BeEquivalentTo(new[] { 2, 3 });
        plan.Gruppen.Single().Positionen.Single(p => p.Line.BelPosId == 3).TageUeberfaellig.Should().Be(10);
    }

    [Fact] // AK 3/4: Rest aus der Rest-Regel (im WMS erfasst) zaehlt, nicht MengeOffen
    public void Faelligkeit_RestNullDurchWmsErfassung_NichtFaellig()
    {
        var l = new[] { L(1, 5) };
        LieferverzugPlaner.Planen(l, new Dictionary<int, decimal> { [1] = 0m }, K(Kr()), KeineMail, E(), Jetzt, Jetzt)
            .AnzahlFaellig.Should().Be(0);
    }

    [Theory] // F6 / Antwort 3
    [InlineData(2026, 11, 12, 7, true)]
    [InlineData(2026, 11, 12, 6, false)]
    [InlineData(2026, 11, 14, 9, false)] // Samstag
    [InlineData(2026, 11, 15, 9, false)] // Sonntag
    [InlineData(2026, 11, 16, 23, true)]
    public void Zeitfenster_MoBisFrAbStunde(int y, int m, int d, int h, bool erwartet) =>
        LieferverzugPlaner.ImZeitfenster(new DateTime(y, m, d, h, 0, 0), 7).Should().Be(erwartet);

    [Fact] // AK 7
    public void Testbetrieb_KreditorMailAlsTestbetrieb()
    {
        var g = Plan(new[] { L(1, 5) }, K(Kr()), E(test: true)).Gruppen.Single();
        g.Grund.Should().Be(LieferverzugGrund.Testbetrieb);
        g.MailPositionen.Should().HaveCount(1);
        g.Status.Should().Contain("TESTBETRIEB").And.Contain("lieferant@test.invalid");
    }

    [Fact] // AK 8 + Antwort 5: Erinnerungsadresse vor Stamm-Mail, genau seine Positionen
    public void Echtbetrieb_JeKreditorEineMail_ErinnerungsadresseVorrang()
    {
        var l = new[] { L(1, 5), L(2, 6), L(3, 5, kto: "70002", creditorId: 2) };
        var plan = Plan(l, K(Kr(1, reminder: "mahnung@test.invalid"), Kr(2)), E(test: false));
        plan.Gruppen.Should().HaveCount(2).And.OnlyContain(g => g.Grund == LieferverzugGrund.Gesendet);
        plan.Gruppen.Single(g => g.Kto == "70001").Adresse.Should().Be("mahnung@test.invalid");
        plan.Gruppen.Single(g => g.Kto == "70001").MailPositionen!.Select(p => p.Line.BelPosId).Should().BeEquivalentTo(new[] { 1, 2 });
        plan.AnzahlEchteMails.Should().Be(2);
    }

    [Fact] // AK 9
    public void Wiederholung_NachTagen_TerminAenderungGiltAlsNeu()
    {
        var l = new[] { L(1, 5), L(2, 5), L(3, 5) };
        var letzte = new Dictionary<(int, DateTime?), DateTime>
        {
            [(1, Heute.AddDays(-5))] = Heute.AddDays(-3).AddHours(7),   // vor 3 Tagen -> nicht dran
            [(2, Heute.AddDays(-5))] = Heute.AddDays(-7).AddHours(7),   // vor 7 Tagen -> dran
            [(3, Heute.AddDays(-9))] = Heute.AddDays(-1).AddHours(7),   // anderer Termin -> neu
        };
        var g = Plan(l, K(Kr()), E(test: false), letzte).Gruppen.Single();
        g.MailPositionen!.Select(p => p.Line.BelPosId).Should().BeEquivalentTo(new[] { 2, 3 });

        var nurEine = Plan(new[] { L(1, 5) }, K(Kr()), E(test: false), letzte).Gruppen.Single();
        nurEine.Grund.Should().Be(LieferverzugGrund.NichtDran);
        nurEine.Status.Should().Be($"nächste Erinnerung am {Heute.AddDays(4):dd.MM.yyyy}");
    }

    [Fact] // AK 10
    public void Sonderfaelle_KeineKreditorMail_GrundInStatus()
    {
        var l = new[]
        {
            L(1, 5, kto: "A", creditorId: 1), L(2, 5, kto: "B", creditorId: 2), L(3, 5, kto: "C", creditorId: 3, gesperrt: true),
            L(4, 5, kto: "D", creditorId: null), L(5, 5, kto: "E", creditorId: 5), L(6, 5, kto: "F", creditorId: 6),
        };
        var plan = Plan(l, K(Kr(1, mail: null), Kr(2, mail: "abc@"), Kr(3), Kr(5, aus: true), Kr(6, aktiv: false)), E(test: false));
        string S(string kto) => plan.Gruppen.Single(g => g.Kto == kto).Status;
        S("A").Should().Be("keine Adresse");
        S("B").Should().Contain("ungültige Adresse");
        S("C").Should().Be("Lieferant gesperrt");
        S("D").Should().Be("kein Kreditor im WMS");
        S("E").Should().StartWith("abgeschaltet");
        S("F").Should().Be("Kreditor inaktiv");
        plan.MitMail.Should().BeEmpty();
    }

    [Fact]
    public void KreditorMailsAus_NurStatus()
    {
        var g = Plan(new[] { L(1, 5) }, K(Kr()), E(kreditor: false)).Gruppen.Single();
        g.Grund.Should().Be(LieferverzugGrund.KreditorMailsAus);
        g.MailPositionen.Should().BeNull();
    }

    [Fact] // AK 11
    public void StandAelterAls24h_KeineKreditorMail()
    {
        var plan = Plan(new[] { L(1, 5) }, K(Kr()), E(test: false), stand: Jetzt.AddHours(-25));
        plan.StandAlt.Should().BeTrue();
        plan.Gruppen.Single().Grund.Should().Be(LieferverzugGrund.StandAlt);
    }

    [Fact] // AK 12
    public void Grenze_MehrEchteMailsAlsMax_KeineRaus_ImTestbetriebNicht()
    {
        var l = new[] { L(1, 5), L(2, 5, kto: "70002", creditorId: 2) };
        var echt = Plan(l, K(Kr(1), Kr(2)), E(test: false, max: 1));
        echt.GrenzeUeberschritten.Should().BeTrue();
        echt.MitMail.Should().BeEmpty();
        echt.Gruppen.Should().OnlyContain(g => g.Grund == LieferverzugGrund.Grenze);

        Plan(l, K(Kr(1), Kr(2)), E(test: true, max: 1)).MitMail.Should().HaveCount(2);
    }

    // ---------------------------------------------------------------- HTML

    [Fact] // AK 7, 14, 15
    public void KreditorMail_TestbetriebBannerLogoHausfarbeText()
    {
        var plan = Plan(new[] { L(1, 5), L(2, 5, abweichend: true) }, K(Kr()), E());
        var g = plan.Gruppen.Single();
        var m = LieferverzugMailHtml.Kreditor(g, g.MailPositionen!, "AKE", testbetrieb: true);
        m.Betreff.Should().StartWith("TESTBETRIEB – wäre an Lieferant 1, lieferant@test.invalid gegangen – ");
        m.Html.Should().Contain("Teil &lt;b&gt;1").And.NotContain("Teil <b>1");                       // HtmlEncode
        System.Net.WebUtility.HtmlDecode(m.Html).Should().Contain("#ef7d00").And.Contain("TESTBETRIEB – wäre an").And.Contain("cid:" + LieferverzugMailHtml.LogoCid)
            .And.Contain("background:#43a6e2;padding:14px 32px;\"><h1 style=\"margin:0;color:#053153;")  // AKE-Blau nur mit dunkler Schrift
            .And.Contain("Pichl 66, 8984 Bad Mitterndorf").And.Contain("www.ideal-ake.at")
            .And.Contain("siehe Bestellung / see order").And.Contain("Reminder");
        m.Html.Should().NotContain("BelPosId").And.NotContain(">1<"); // keine internen Werte
        m.Text.Should().NotContain("<td").And.NotContain("<p").And.NotContain("<a").And.Contain("https://www.ideal-ake.at").And.StartWith("TESTBETRIEB");

        var ideal = LieferverzugMailHtml.Kreditor(g, g.MailPositionen!, "IDEAL", testbetrieb: false);
        ideal.Betreff.Should().NotContain("TESTBETRIEB");
        System.Net.WebUtility.HtmlDecode(ideal.Html).Should().Contain("background:#053153;padding:14px 32px;\"><h1 style=\"margin:0;color:#ffffff;").And.Contain("4810 Gmunden");
    }

    [Fact] // AK 6, 11, 14
    public void EinkaufMail_GruppiertMitStatusLinkUndHinweis()
    {
        var plan = Plan(new[] { L(1, 5), L(2, 5, kto: "70002", creditorId: null) }, K(Kr()), E(test: false), stand: Jetzt.AddDays(-2));
        var m = LieferverzugMailHtml.Einkauf(plan, "AKE", Jetzt.AddDays(-2), "https://wms.test/", Heute, E());
        System.Net.WebUtility.HtmlDecode(m.Html).Should().Contain("Lieferant 1 (70001)").And.Contain("Kreditor-Mail: ausgesetzt – Bestelldaten veraltet")
            .And.Contain("kein Kreditor im WMS").And.Contain("https://wms.test/PurchaseOrders?nurUeberfaellig=true")
            .And.Contain("Kreditor-Mails ausgesetzt");
        m.Text.Should().NotContain("<a").And.Contain("https://wms.test/PurchaseOrders?nurUeberfaellig=true");
    }

    // ---------------------------------------------------------------- v1.99.19 Nachbesserung (Spec 2026-11-13-lieferverzug-nachbesserung-keyuser)

    private static SagePurchaseOrderLine Abruf(int id, int tage, string kto = "70001", int? creditorId = 1)
    {
        var l = L(id, tage, kto: kto, creditorId: creditorId);
        (l.Belegkennzeichen, l.MengeGeliefert) = ("EBZ", 0m);
        return l;
    }

    [Fact] // AK 2, 4, TS-128.16: Sammelliste ab 1 Tag, Kreditor-Mail ab 3 Tagen, Rest "erst ab" in der Statuszeile
    public void ZweiSchwellen_SammellisteAlle_KreditorNurAbSchwelle()
    {
        var l = new[] { L(1, 1), L(2, 2), L(3, 3), L(4, 10) };
        var g = Plan(l, K(Kr()), E(test: true, tage: 1, kreditorTage: 3)).Gruppen.Single();
        g.Positionen.Select(p => p.Line.BelPosId).Should().BeEquivalentTo(new[] { 1, 2, 3, 4 });
        g.Grund.Should().Be(LieferverzugGrund.Testbetrieb, "Testbetrieb folgt derselben Schwelle");
        g.MailPositionen!.Select(p => p.Line.BelPosId).Should().BeEquivalentTo(new[] { 3, 4 });
        g.Status.Should().EndWith($"– 2 Position(en) erst ab {Heute.AddDays(-2).AddDays(3):dd.MM.yyyy}");
    }

    [Fact] // AK 2, TS-128.17: nur Vorlauf -> keine Kreditor-Mail, Status "Kreditor-Mail ab", am Tag Termin+3 dann in der Mail
    public void NurVorlauf_KeineKreditorMail_StatusAbDatum()
    {
        var g = Plan(new[] { L(1, 1) }, K(Kr()), E(test: false, tage: 1, kreditorTage: 3)).Gruppen.Single();
        g.Grund.Should().Be(LieferverzugGrund.Vorlauf);
        g.MailPositionen.Should().BeNull();
        g.Status.Should().Be($"Kreditor-Mail ab {Heute.AddDays(2):dd.MM.yyyy} (3 Tage nach Liefertermin)");

        Plan(new[] { L(1, 3) }, K(Kr()), E(test: false, tage: 1, kreditorTage: 3)).Gruppen.Single().Grund.Should().Be(LieferverzugGrund.Gesendet);
    }

    [Fact] // AK 3, TS-128.18: wirksam ist das Maximum beider Schwellen
    public void MaximumRegel_SammellisteHoeherAlsKreditor()
    {
        var plan = Plan(new[] { L(1, 4), L(2, 5) }, K(Kr()), E(test: false, tage: 5, kreditorTage: 3));
        plan.Gruppen.Single().Positionen.Select(p => p.Line.BelPosId).Should().Equal(2);
        plan.Gruppen.Single().MailPositionen!.Select(p => p.Line.BelPosId).Should().Equal(2);
    }

    [Fact] // AK 5, TS-128.19: Altersgrenze 180 - heute-181 und Abruf heute-400 zu klaeren, heute-180 normal
    public void Altersgrenze_ZuKlaeren_NieInGruppeOderKreditorMail()
    {
        var l = new[] { L(1, 181), Abruf(2, 400), L(3, 180) };
        var plan = Plan(l, K(Kr()), E(test: false, maxAlter: 180));
        plan.ZuKlaeren.Select(p => p.Line.BelPosId).Should().BeEquivalentTo(new[] { 1, 2 });
        plan.Gruppen.Single().Positionen.Select(p => p.Line.BelPosId).Should().Equal(3);
        plan.Gruppen.Single().MailPositionen!.Select(p => p.Line.BelPosId).Should().Equal(3);
        plan.AnzahlFaellig.Should().Be(3);

        var m = LieferverzugMailHtml.Einkauf(plan, "AKE", Jetzt, null, Heute, E(test: false, maxAlter: 180));
        m.Betreff.Should().Be($"Lieferverzug: 3 Bestellposition(en) bei 1 Lieferant(en), davon 2 zu klären – {Heute:dd.MM.yyyy}");
        var html = System.Net.WebUtility.HtmlDecode(m.Html);
        html.Should().Contain("Zu klären – Liefertermin älter als 180 Tage (keine Erinnerung an Lieferanten)")
            .And.Contain("Bitte in Sage prüfen: geliefert, storniert oder Termin anpassen.").And.Contain(">Rahmenabruf<").And.Contain(">Bestellung<")
            .And.Contain(">Lieferant 1 (70001)<", "QA Q2: derselbe Name wie im Gruppenkopf (WMS-Kreditor)");
        m.Text.Should().Contain("Zu klären – Liefertermin älter als 180 Tage").And.Contain("| Rahmenabruf").And.NotContain("<td");
    }

    [Fact] // AK 6, TS-128.21: Grenze 0 = aus
    public void Altersgrenze_Null_KeinAbschnitt()
    {
        var plan = Plan(new[] { L(1, 400) }, K(Kr()), E(test: false, maxAlter: 0));
        plan.ZuKlaeren.Should().BeEmpty();
        plan.Gruppen.Single().MailPositionen.Should().HaveCount(1);
        var m = LieferverzugMailHtml.Einkauf(plan, "AKE", Jetzt, null, Heute, E(maxAlter: 0));
        m.Html.Should().NotContain("Zu kl");
        m.Betreff.Should().NotContain("zu klären");
    }

    [Fact] // AK 8, TS-128.20: 31 Kreditoren nur mit alten Positionen, Grenze 30 -> keine Grenzen-Warnung
    public void ZuKlaeren_ZaehltNichtFuerGrenze()
    {
        var l = Enumerable.Range(1, 31).Select(i => L(i, 200, kto: "K" + i, creditorId: i)).ToArray();
        var plan = Plan(l, K(Enumerable.Range(1, 31).Select(i => Kr(i)).ToArray()), E(test: false, max: 30, maxAlter: 180));
        plan.GrenzeUeberschritten.Should().BeFalse();
        plan.Gruppen.Should().BeEmpty();
        plan.ZuKlaeren.Should().HaveCount(31);
        plan.AnzahlLieferanten.Should().Be(31);
    }

    [Fact] // AK 10, TS-128.23: Schalter aus -> Abruf nur Sammelliste mit Statuszeile; an -> Kreditor-Mail
    public void Rahmenabruf_SchalterAus_NurSammelliste()
    {
        var aus = Plan(new[] { Abruf(1, 10) }, K(Kr()), E(test: false)).Gruppen.Single();
        aus.Positionen.Should().HaveCount(1);
        aus.Grund.Should().Be(LieferverzugGrund.Rahmenabruf);
        aus.MailPositionen.Should().BeNull();
        aus.Status.Should().Be("1 Rahmenabruf/-abrufe nicht an Lieferanten (Einstellung)");
        aus.AbrufeAusgenommen.Should().Be(1);

        var gemischt = Plan(new[] { Abruf(1, 10), L(2, 10) }, K(Kr()), E(test: false)).Gruppen.Single();
        gemischt.MailPositionen!.Select(p => p.Line.BelPosId).Should().Equal(2);
        gemischt.Status.Should().EndWith("– 1 Rahmenabruf/-abrufe nicht an Lieferanten (Einstellung)");

        var an = Plan(new[] { Abruf(1, 10) }, K(Kr()), E(test: false, mitAbrufen: true)).Gruppen.Single();
        an.Grund.Should().Be(LieferverzugGrund.Gesendet);
        an.MailPositionen.Should().HaveCount(1);
    }

    [Fact] // AK 7, 9: nur Positionen zu klaeren -> Sammelliste geht trotzdem, Protokoll Art = Einkauf je Position, Counts
    public async Task Lauf_NurZuKlaeren_SammellisteMitProtokollUndCounts()
    {
        var (svc, db, mail, log, _) = Svc();
        Seed(db, L(1, 200), Abruf(2, 300, kto: "70002", creditorId: 2));
        await svc.RunAsync(E(test: false, maxAlter: 180), false, Jetzt);

        mail.Gesendet.Should().ContainSingle().Which.Betreff.Should().Contain("davon 2 zu klären");
        db.PurchaseOrderReminders.Where(r => r.Art == PurchaseOrderReminderArt.Einkauf).Select(r => r.BelPosId).Should().BeEquivalentTo(new[] { 1, 2 });
        db.PurchaseOrderReminders.Should().NotContain(r => r.Art == PurchaseOrderReminderArt.Kreditor);
        var c = log.Runs.Single().FinalCounts!;
        c["zu_klaeren"].Should().Be(2);
        c["faellig"].Should().Be(2);
        c["vorlauf"].Should().Be(0);
        c["rahmenabruf_ausgenommen"].Should().Be(0);
    }

    [Fact] // AK 9: DryRun mit vorlauf und rahmenabruf_ausgenommen
    public async Task Lauf_DryRun_CountsVorlaufUndAbrufe()
    {
        var (svc, db, _, log, _) = Svc();
        Seed(db, L(1, 1), Abruf(2, 10, kto: "70002", creditorId: 2));
        await svc.RunAsync(E(test: false, tage: 1, kreditorTage: 3), dryRun: true, Jetzt);
        var run = log.Runs.Single();
        run.FinalMessageSuffix.Should().Be("[DryRun]");
        run.FinalCounts!["vorlauf"].Should().Be(1);
        run.FinalCounts!["rahmenabruf_ausgenommen"].Should().Be(1);
        run.FinalCounts!["kreditor_geplant"].Should().Be(0);
    }

    [Fact] // Freigabe-Nachtrag 2: Lief.-Art.-Nr. je (Artikel, Kreditor) in Kreditor-Mail und Sammelliste, HTML und Text; fehlt = leer
    public void LiefArtNr_InKreditorMailUndSammelliste_FehltLeer()
    {
        var l1 = L(1, 5); l1.ArticleId = 11;
        var l2 = L(2, 5); l2.ArticleId = 12;   // ohne ArticleSupplier
        var l3 = L(3, 400); l3.ArticleId = 11; // zu klaeren
        var nr = new Dictionary<(int, int), string> { [(11, 1)] = "LF-4711" };
        var plan = LieferverzugPlaner.Planen(new[] { l1, l2, l3 }, Rest(l1, l2, l3), K(Kr()), KeineMail, E(test: false, maxAlter: 180), Jetzt, Jetzt.AddHours(-1), nr);
        var g = plan.Gruppen.Single();
        g.Positionen.Single(p => p.Line.BelPosId == 1).LiefArtNr.Should().Be("LF-4711");
        g.Positionen.Single(p => p.Line.BelPosId == 2).LiefArtNr.Should().BeNull();
        plan.ZuKlaeren.Single().LiefArtNr.Should().Be("LF-4711");

        var k = LieferverzugMailHtml.Kreditor(g, g.MailPositionen!, "AKE", testbetrieb: false);
        var kHtml = System.Net.WebUtility.HtmlDecode(k.Html);
        kHtml.Should().Contain("Lief.-Art.-Nr.<br>Supplier item no.").And.Contain(">LF-4711<");
        k.Text.Should().Contain("2027-1 | EK1 | Lief.-Art.-Nr./supplier item no. LF-4711 |").And.Contain("2027-2 | EK2 | Teil");

        var m = LieferverzugMailHtml.Einkauf(plan, "AKE", Jetzt, null, Heute, E(test: false, maxAlter: 180));
        System.Net.WebUtility.HtmlDecode(m.Html).Should().Contain(">Lief.-Art.-Nr.<").And.Contain(">LF-4711<");
        m.Text.Should().Contain("2027-1 | EK1 | Lief.-Art.-Nr. LF-4711 |").And.Contain("2027-2 | EK2 | Teil").And.Contain("2027-3 | EK3 | Lief.-Art.-Nr. LF-4711 |");

        Plan(new[] { l1 }, K(Kr()), E()).Gruppen.Single().Positionen.Single().LiefArtNr.Should().BeNull("ohne Abgleich-Daten leer, kein Fehler");
    }

    [Fact] // QA Q2: ohne WMS-Kreditor gilt der Sage-Name – in Gruppe und Zu klären gleich
    public void ZuKlaeren_LieferantGleicherNameWieGruppe()
    {
        var plan = Plan(new[] { L(1, 5, creditorId: null), L(2, 400, creditorId: null) }, K(), E(maxAlter: 180));
        plan.Gruppen.Single().Name.Should().Be("Sage 70001");
        plan.ZuKlaeren.Single().Lieferant.Should().Be("Sage 70001");

        var mitKreditor = Plan(new[] { L(1, 5), L(2, 400) }, K(Kr()), E(maxAlter: 180));
        mitKreditor.Gruppen.Single().Name.Should().Be("Lieferant 1");
        mitKreditor.ZuKlaeren.Single().Lieferant.Should().Be("Lieferant 1");
    }

    [Fact] // QA Q5: Kreditor-Schwelle in der Einleitung nur bei eingeschalteten Kreditor-Mails
    public void Einleitung_KreditorSchwelleNurBeiKreditorAktiv()
    {
        var plan = Plan(new[] { L(1, 5) }, K(Kr()), E());
        LieferverzugMailHtml.Einkauf(plan, "AKE", Jetzt, null, Heute, E(kreditor: true)).Text.Should().Contain("Kreditor-Mails gehen frühestens 3 Tage");
        LieferverzugMailHtml.Einkauf(plan, "AKE", Jetzt, null, Heute, E(kreditor: false)).Text.Should().NotContain("Kreditor-Mails gehen");
        // QA Nach-QA: ohne Kreditor-Satz kein doppeltes Leerzeichen vor dem Altersgrenzen-Satz
        LieferverzugMailHtml.Einkauf(plan, "AKE", Jetzt, null, Heute, E(kreditor: false, maxAlter: 180)).Text.Should().Contain("Bestellung“). Positionen mit Liefertermin");
    }

    [Fact] // Freigabe-Nachtrag 2: Lauf liest ArticleSuppliers (Artikel + Kreditor der Bestellung), leere Bestellnummer = leer
    public async Task Lauf_LiefArtNrAusArtikelLieferant()
    {
        var (svc, db, mail, _, _) = Svc();
        var l1 = L(1, 5); l1.ArticleId = 11;
        var l2 = L(2, 5); l2.ArticleId = 12;
        Seed(db, l1, l2);
        db.ArticleSuppliers.AddRange(
            new ArticleSupplier { ArticleId = 11, CreditorId = 1, Artikelnummer = "EK1", SageBestellnummer = " LF-4711 ", CreatedAt = Jetzt, CreatedBy = "t", CreatedByWindows = "t" },
            new ArticleSupplier { ArticleId = 11, CreditorId = 2, Artikelnummer = "EK1", SageBestellnummer = "FALSCH", CreatedAt = Jetzt, CreatedBy = "t", CreatedByWindows = "t" },
            new ArticleSupplier { ArticleId = 12, CreditorId = 1, Artikelnummer = "EK2", SageBestellnummer = "", CreatedAt = Jetzt, CreatedBy = "t", CreatedByWindows = "t" });
        db.SaveChanges();

        await svc.RunAsync(E(test: false), false, Jetzt);

        var kreditor = mail.Gesendet.Single(n => n.Empfaenger.Contains("lieferant@test.invalid"));
        kreditor.Text.Should().Contain("EK1 | Lief.-Art.-Nr./supplier item no. LF-4711 |").And.NotContain("FALSCH").And.Contain("2027-2 | EK2 | Teil");
    }

    // ---------------------------------------------------------------- Lauf

    private sealed class FakeMail : IMailService
    {
        public List<MailNachricht> Gesendet { get; } = new();
        public Func<MailNachricht, bool> Antwort { get; set; } = _ => true;
        public Task SendAsync(string subject, string htmlBody, IEnumerable<string> recipients, string? textBody = null, CancellationToken ct = default) =>
            throw new InvalidOperationException("nicht erwartet");
        public Task<bool> SendMitOptionenAsync(MailNachricht n, CancellationToken ct = default) { Gesendet.Add(n); return Task.FromResult(Antwort(n)); }
    }

    private static (LieferverzugMailService svc, ApplicationDbContext db, FakeMail mail, FakeSyncLogger log, Mock<ISyncErrorNotifier> notifier) Svc(
        Dictionary<int, ErfasstJeBelPos>? erfasst = null)
    {
        var db = TestDbContextFactory.Create();
        var receipts = new Mock<IPurchaseOrderReceiptRepository>();
        receipts.Setup(r => r.GetErfasstJeBelPosAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync(erfasst ?? new Dictionary<int, ErfasstJeBelPos>());
        var mail = new FakeMail();
        var log = new FakeSyncLogger();
        var notifier = new Mock<ISyncErrorNotifier>();
        return (new LieferverzugMailService(db, receipts.Object, mail, notifier.Object, NullLogger<LieferverzugMailService>.Instance, log), db, mail, log, notifier);
    }

    private static void Seed(ApplicationDbContext db, params SagePurchaseOrderLine[] l)
    {
        db.Creditors.Add(new Creditor { Id = 1, Name = "Lieferant 1", Email = "lieferant@test.invalid", SageKto = "70001", IsActive = true,
            CreatedAt = Jetzt, CreatedBy = "t", CreatedByWindows = "t" });
        db.Creditors.Add(new Creditor { Id = 2, Name = "Lieferant 2", Email = "zwei@test.invalid", SageKto = "70002", IsActive = true,
            CreatedAt = Jetzt, CreatedBy = "t", CreatedByWindows = "t" });
        db.SagePurchaseOrderLines.AddRange(l);
        db.SaveChanges();
    }

    [Fact] // AK 5, 7: Testbetrieb an den Einkauf, Protokoll, zweiter Lauf am selben Tag ohne Mail
    public async Task Lauf_Testbetrieb_UmgeleitetProtokolliertEinmalAmTag()
    {
        var (svc, db, mail, log, _) = Svc();
        Seed(db, L(1, 5), L(2, 5, kto: "70002", creditorId: 2));

        await svc.RunAsync(E(test: true), dryRun: false, Jetzt);

        mail.Gesendet.Should().HaveCount(3); // 2 Kreditor (umgeleitet) + 1 Einkauf
        mail.Gesendet.SelectMany(n => n.Empfaenger).Should().OnlyContain(a => a == "einkauf@test.invalid");
        mail.Gesendet.Take(2).Should().OnlyContain(n => n.Betreff.StartsWith("TESTBETRIEB") && n.AntwortAn == "einkauf@test.invalid"
            && n.Absender == "einkauf@test.invalid" && n.Text != null);
        db.PurchaseOrderReminders.Count(r => r.Art == PurchaseOrderReminderArt.Kreditor && r.Testbetrieb).Should().Be(2);
        db.PurchaseOrderReminders.Count(r => r.Art == PurchaseOrderReminderArt.Einkauf).Should().Be(2);
        log.Runs.Single().ServiceName.Should().Be(SyncLogServices.LieferverzugMail);
        log.Runs.Single().FinalCounts!["testbetrieb_umgeleitet"].Should().Be(2);
        log.Runs.Single().FinalCounts!["einkauf_gesendet"].Should().Be(1);

        await svc.RunAsync(E(test: true), dryRun: false, Jetzt.AddHours(3)); // "Neustart": Stand steht in der Tabelle
        mail.Gesendet.Should().HaveCount(3);
        log.Runs.Should().HaveCount(1);
    }

    [Fact] // AK 13: Mailservice ueberspringt still -> keine Protokollzeile, Warnung
    public async Task Lauf_MailserviceUeberspringt_KeineZeile_Warnung()
    {
        var (svc, db, mail, log, _) = Svc();
        mail.Antwort = _ => false;
        Seed(db, L(1, 5));
        await svc.RunAsync(E(test: false), false, Jetzt);
        db.PurchaseOrderReminders.Should().BeEmpty();
        log.Runs.Single().Events.Should().Contain(x => x.Level == "Warning" && x.Message.Contains("nicht gesendet"));
    }

    [Fact] // AK 18: Fehler einer Kreditor-Mail bricht den Lauf nicht ab
    public async Task Lauf_FehlerEinerKreditorMail_UebrigeLaufen()
    {
        var (svc, db, mail, log, _) = Svc();
        mail.Antwort = n => n.Empfaenger.Contains("lieferant@test.invalid") ? throw new InvalidOperationException("550 abgelehnt") : true;
        Seed(db, L(1, 5), L(2, 5, kto: "70002", creditorId: 2));
        await svc.RunAsync(E(test: false), false, Jetzt);
        log.Runs.Single().FinalCounts!["fehler"].Should().Be(1);
        log.Runs.Single().FinalCounts!["kreditor_gesendet"].Should().Be(1);
        System.Net.WebUtility.HtmlDecode(mail.Gesendet.Last().Html).Should().Contain("Fehler beim Versand: 550 abgelehnt"); // Einkauf-Liste mit Ergebnis
        db.PurchaseOrderReminders.Count(r => r.Art == PurchaseOrderReminderArt.Kreditor).Should().Be(1);
    }

    [Fact] // AK 17
    public async Task Lauf_DryRun_KeineMailKeineZeile()
    {
        var (svc, db, mail, log, _) = Svc();
        Seed(db, L(1, 5));
        await svc.RunAsync(E(test: false), dryRun: true, Jetzt);
        mail.Gesendet.Should().BeEmpty();
        db.PurchaseOrderReminders.Should().BeEmpty();
        log.Runs.Single().FinalMessageSuffix.Should().Be("[DryRun]");
        log.Runs.Single().FinalCounts!["kreditor_geplant"].Should().Be(1);
    }

    [Fact] // AK 12: Grenze -> keine Kreditor-Mail, Warnung + Fehlermail
    public async Task Lauf_Grenze_KeineKreditorMail_Fehlermail()
    {
        var (svc, db, mail, log, notifier) = Svc();
        Seed(db, L(1, 5), L(2, 5, kto: "70002", creditorId: 2));
        await svc.RunAsync(E(test: false, max: 1), false, Jetzt);
        mail.Gesendet.Should().ContainSingle().Which.Empfaenger.Should().Equal("einkauf@test.invalid");
        notifier.Verify(n => n.NotifyAsync("Lieferverzug-Mails", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
        log.Runs.Single().Events.Should().Contain(x => x.Level == "Warning" && x.Message.Contains("Grenze"));
    }

    [Fact] // AK 3/4: Rest ueber die Rest-Regel des Repositorys
    public async Task Lauf_ImWmsVollErfasst_NichtFaellig_KeinLauf()
    {
        var (svc, db, mail, log, _) = Svc(new Dictionary<int, ErfasstJeBelPos> { [1] = new(5m, Array.Empty<(DateTime, decimal)>()) });
        Seed(db, L(1, 5));
        await svc.RunAsync(E(), false, Jetzt);
        mail.Gesendet.Should().BeEmpty();
        log.Runs.Should().BeEmpty();
    }

    [Fact] // v1.99.15 (Teil 3, AK 11): Abrufe nach Rest-Regel F2 (auch Uebergebenes abgezogen), „Geliefert“ = „–“, in beiden Mails (Freigabe-Antwort 1)
    public async Task Lauf_Rahmenabruf_RestRegelF2_GeliefertStrich_AuchKreditorMail()
    {
        SagePurchaseOrderLine Abruf(int id, decimal menge)
        {
            var l = L(id, 5, offen: menge);
            (l.Belegkennzeichen, l.MengeBestellt, l.MengeGeliefert) = ("EBZ", menge, 0m);
            return l;
        }
        var uebergeben = new List<(DateTime, decimal)> { (Jetzt.AddHours(-2), 4m) };   // vor SyncedAt: bei EBB nicht abgezogen
        var (svc, db, mail, _, _) = Svc(new Dictionary<int, ErfasstJeBelPos> { [3] = new(10m, uebergeben), [4] = new(4m, uebergeben) });
        Seed(db, Abruf(3, 10m), Abruf(4, 10m));

        await svc.RunAsync(E(test: false, mitAbrufen: true), false, Jetzt); // v1.99.19 F3: nur mit Schalter in der Kreditor-Mail

        var einkauf = mail.Gesendet.Single(n => n.Empfaenger.Contains("einkauf@test.invalid"));
        einkauf.Text.Should().Contain("2027-4 |").And.Contain("geliefert –,").And.Contain("Rest 6 Stk").And.NotContain("2027-3 |", "Rest 0 nie");
        System.Net.WebUtility.HtmlDecode(einkauf.Html).Should().Contain("2027-4").And.NotContain("2027-3");
        mail.Gesendet.Should().Contain(n => n.Empfaenger.Contains("lieferant@test.invalid"), "Abrufe auch in der Kreditor-Mail, keine Planer-Bedingung");
    }

    [Fact] // F6: ausserhalb des Zeitfensters kein Lauf; Einkauf-Empfaenger leer -> keine Mail, Warnung
    public async Task Lauf_AusserhalbFenster_Nichts_OhneEmpfaenger_Warnung()
    {
        var (svc, db, mail, log, _) = Svc();
        Seed(db, L(1, 5));
        await svc.RunAsync(E(), false, Jetzt.Date.AddHours(6));
        log.Runs.Should().BeEmpty();

        await svc.RunAsync(E(test: true, einkaufAn: Array.Empty<string>()), false, Jetzt);
        mail.Gesendet.Should().BeEmpty();
        log.Runs.Single().Events.Should().Contain(x => x.Level == "Warning" && x.Message.Contains("Keine Einkauf-Empfänger"));
    }

    [Fact]
    public void Di_LoestAuf_SyncLoggerLetzterParameter_LogoEingebettet()
    {
        typeof(LieferverzugMailService).GetConstructors().Single().GetParameters().Last().ParameterType.Should().Be(typeof(ISyncLogger));
        typeof(LieferverzugMailService).Assembly.GetManifestResourceNames().Should().Contain(LieferverzugMailService.LogoRessource);
        var services = new ServiceCollection();
        services.AddSingleton(TestDbContextFactory.Create());
        services.AddSingleton(new Mock<IPurchaseOrderReceiptRepository>().Object);
        services.AddSingleton(new Mock<IMailService>().Object);
        services.AddSingleton(new Mock<ISyncErrorNotifier>().Object);
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddLogging();
        services.AddScoped<LieferverzugMailService>();
        services.BuildServiceProvider().GetRequiredService<LieferverzugMailService>().Should().NotBeNull();
    }

    // ---------------------------------------------------------------- MailService Pickup (kein SMTP, keine Mail nach aussen)

    [Fact]
    public async Task MailService_Pickup_SchreibtEmlMitReplyToAbsenderUndLogo()
    {
        var ordner = Path.Combine(Path.GetTempPath(), "wms-pickup-" + Guid.NewGuid().ToString("N"));
        try
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailSettings:PickupDirectory"] = ordner, ["MailSettings:FromAddress"] = "wms@test.invalid",
            }).Build();
            var svc = new MailService(config, NullLogger<MailService>.Instance);
            var ok = await svc.SendMitOptionenAsync(new MailNachricht("Betreff", "<p><img src=\"cid:logo-ideal-ake\"></p>", "Text https://x.test",
                new[] { "a@test.invalid" }, "antwort@test.invalid", "einkauf@test.invalid",
                new[] { new MailBild(LieferverzugMailHtml.LogoCid, new byte[] { 1, 2, 3 }, "image/jpeg") }));

            ok.Should().BeTrue();
            var datei = Directory.GetFiles(ordner, "*.eml").Single();
            var msg = await MimeMessage.LoadAsync(datei);
            msg.ReplyTo.Mailboxes.Single().Address.Should().Be("antwort@test.invalid");
            msg.From.Mailboxes.Single().Address.Should().Be("einkauf@test.invalid");
            msg.TextBody.Should().Contain("https://x.test");
            msg.BodyParts.OfType<MimePart>().Should().Contain(p => p.ContentId == LieferverzugMailHtml.LogoCid);

            (await new MailService(new ConfigurationBuilder().Build(), NullLogger<MailService>.Instance)
                .SendMitOptionenAsync(new MailNachricht("x", "y", null, new[] { "a@test.invalid" }))).Should().BeFalse("kein SmtpHost, kein Pickup");
        }
        finally
        {
            if (Directory.Exists(ordner)) Directory.Delete(ordner, true);
        }
    }
}
