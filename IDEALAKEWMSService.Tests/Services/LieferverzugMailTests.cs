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

    private static LieferverzugEinstellungen E(bool test = true, bool kreditor = true, int max = 30, bool einkauf = true, string[]? einkaufAn = null) =>
        new(einkauf, einkaufAn ?? new[] { "einkauf@test.invalid" }, kreditor, test, 3, 7, 7, max, "einkauf@test.invalid", null, "AKE", "https://wms.test");

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
        var m = LieferverzugMailHtml.Einkauf(plan, "AKE", Jetzt.AddDays(-2), "https://wms.test/", Heute, 3);
        System.Net.WebUtility.HtmlDecode(m.Html).Should().Contain("Lieferant 1 (70001)").And.Contain("Kreditor-Mail: ausgesetzt – Bestelldaten veraltet")
            .And.Contain("kein Kreditor im WMS").And.Contain("https://wms.test/PurchaseOrders?nurUeberfaellig=true")
            .And.Contain("Kreditor-Mails ausgesetzt");
        m.Text.Should().NotContain("<a").And.Contain("https://wms.test/PurchaseOrders?nurUeberfaellig=true");
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
