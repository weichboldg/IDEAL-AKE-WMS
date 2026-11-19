using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// BF-Auftraege (v1.99.35, Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv): Mengenregel, Nummer, Status-Werte, Artikelsuche
/// (nur Lagerartikel BG/FT, in SQL), Warenkorb (Hinweis ohne Materialnummer), Abschicken (Gruppe Pflicht, Nummer), Rechte am Entwurf.
/// EF InMemory: Serializable-Transaktion und RowVersion sind Manual-UAT (TS-157).
/// </summary>
public class BfAuftragWorkflowTests
{
    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class Fx
    {
        public ApplicationDbContext Db { get; } = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        public Mock<IAppSettingRepository> Settings { get; } = new();
        public Mock<IOrderRecipientRepository> Groups { get; } = new();
        public Mock<IStorageLocationRepository> Lp { get; } = new();
        public Mock<ICurrentUserService> User { get; } = new();
        public System.Collections.Generic.HashSet<DateTime> Feiertage { get; } = new();
        public BfAuftragRepository Repo { get; }
        public BfAuftragWorkflow Wf { get; }

        public Fx(int gruppe = 3, int userId = 7)
        {
            Repo = new BfAuftragRepository(Db);
            // v1.99.42: Besteller mit Kuerzel (Freigabe liest es frisch vom Benutzer)
            Db.Users.Add(new User { Id = userId, Name = "Tester", Kuerzel = "TST", CreatedBy = "t", CreatedByWindows = "t" });
            Db.SaveChanges();
            Settings.Setup(s => s.GetValueAsync(AppSettingKeys.BfStatuswerte)).ReturnsAsync("EILT,Muster");
            Settings.Setup(s => s.GetIntValueAsync(AppSettingKeys.BfEmpfaengergruppeId, 0)).ReturnsAsync(gruppe);
            Groups.Setup(g => g.GetGroupByIdAsync(3)).ReturnsAsync(new OrderRecipientGroup { Id = 3, Name = "Blech" });
            User.Setup(u => u.GetCurrentAppUserId()).Returns(userId);
            User.Setup(u => u.GetDisplayName()).Returns("Tester");
            User.Setup(u => u.GetWindowsUserName()).Returns("DOM\\tester");
            var holidays = new Mock<IHolidayRepository>();
            holidays.Setup(h => h.GetHolidayDatesAsync()).ReturnsAsync(() => Feiertage);
            Settings.Setup(s => s.GetIntValueAsync(AppSettingKeys.BfSchachtelterminArbeitstage, 1)).ReturnsAsync(1);
            Settings.Setup(s => s.GetIntValueAsync(AppSettingKeys.BfFeTerminArbeitstage, 10)).ReturnsAsync(10);
            Wf = new BfAuftragWorkflow(Repo, Settings.Object, Groups.Object, Lp.Object, User.Object, holidays.Object, new BusinessDayService(),
                new FixedTime(new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.Zero)), NullLogger<BfAuftragWorkflow>.Instance);
        }

        private ArticleAttributeDefinition Def(string feld)
        {
            var d = Db.ArticleAttributeDefinitions.Local.FirstOrDefault(x => x.SyncFieldName == feld);
            if (d is not null) return d;
            d = new ArticleAttributeDefinition { Name = feld, AttributeType = AttributeType.Text, SyncSource = ArticleAttributeDefinition.SyncSourceSage, SyncFieldName = feld };
            Db.ArticleAttributeDefinitions.Add(d);
            return d;
        }

        public void Artikel(string nr, string? ist = "1", string? typ = "BG", string? materialnummer = "11", string? bestellmenge = null)
        {
            var a = new Article { ArticleNumber = nr, Description = "Teil " + nr, Matchcode = "MC " + nr, SagePrimaryStorageLocation = "L-1" };
            void W(string f, string? v) { if (v is not null) a.AttributeValues.Add(new ArticleAttributeValue { ArticleAttributeDefinition = Def(f), TextValue = v }); }
            W(BfArtikelFelder.IstLagerartikel, ist);
            W(BfArtikelFelder.Artikeltyp, typ);
            W(BfArtikelFelder.Material, "Stahl");
            W(BfArtikelFelder.Materialnummer, materialnummer);
            W(BfArtikelFelder.Bestellmenge, bestellmenge);
            Db.Articles.Add(a);
            Db.SaveChanges();
        }
    }

    [Theory]
    [InlineData(5, null, true)]
    [InlineData(0, null, false)]
    [InlineData(-1, null, false)]
    [InlineData(10, 5.0, true)]
    [InlineData(7, 5.0, false)]
    [InlineData(7, 0.0, true)] // 0 = frei
    [InlineData(2.5, 0.5, true)]
    public void Mengenregel_VielfachesNurBeiBestellmenge(decimal menge, double? bestellmenge, bool ok) =>
        (BfAuftragWorkflow.MengenFehler(menge, (decimal?)bestellmenge) is null).Should().Be(ok);

    [Fact]
    public void Nummer_BfPlusAchtZiffern() => BfAuftragWorkflow.Nummer(42).Should().Be("BF00000042");

    [Theory]
    [InlineData(null, new[] { "EILT" })]
    [InlineData("", new string[0])]
    [InlineData(" EILT , Muster,EILT", new[] { "EILT", "Muster" })]
    public void Statuswerte_AusEinstellung(string? setting, string[] erwartet) =>
        BfAuftragWorkflow.Statuswerte(setting).Should().Equal(erwartet);

    [Fact]
    public async Task Artikelsuche_NurLagerartikelMitTeileartBgOderFt()
    {
        var fx = new Fx();
        fx.Artikel("BG1");
        fx.Artikel("FT1", typ: "FT");
        fx.Artikel("ZK1", typ: "ZK");
        fx.Artikel("NL1", ist: "0");
        fx.Artikel("OHNE", ist: null);

        var treffer = await fx.Repo.SucheArtikelAsync(null, 50);

        treffer.Select(t => t.ArticleNumber).Should().BeEquivalentTo("BG1", "FT1");
        treffer.Single(t => t.ArticleNumber == "BG1").Should().Match<BfArtikel>(t => t.Materialnummer == "11" && t.Hauptlagerplatz == "L-1" && t.Teileart == "BG");
    }

    [Fact]
    public async Task Artikelsuche_MitSuchbegriff_NurLagerartikelBgFt()
    {
        var fx = new Fx();
        fx.Artikel("BG1");
        fx.Artikel("FT1", typ: "FT");
        fx.Artikel("ZK1", typ: "ZK");
        fx.Artikel("NL1", ist: "0");
        fx.Artikel("OHNE", ist: null);

        (await fx.Repo.SucheArtikelAsync("Teil", 50)).Select(t => t.ArticleNumber).Should().BeEquivalentTo("BG1", "FT1");
        (await fx.Repo.SucheArtikelAsync("MC ZK1", 50)).Should().BeEmpty();
    }

    /// <summary>v1.99.45: Type-ahead-Endpunkt liefert dieselben Artikel wie die Server-Suche (ein Filter, SucheArtikelAsync).</summary>
    [Fact]
    public async Task ApiArtikel_LiefertDieselbenArtikelWieDieServerSuche()
    {
        var fx = new Fx();
        fx.Artikel("BG1");
        fx.Artikel("FT1", typ: "FT");
        fx.Artikel("ZK1", typ: "ZK");
        fx.Artikel("NL1", ist: "0");

        var ok = (await new IdealAkeWms.Controllers.Api.BfAuftraegeApiController(fx.Repo).Artikel("Teil", 20))
            .Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>().Subject;
        var api = ((System.Collections.IEnumerable)ok.Value!).Cast<object>()
            .Select(o => (string)o.GetType().GetProperty("ArticleNumber")!.GetValue(o)!).ToList();

        api.Should().BeEquivalentTo((await fx.Repo.SucheArtikelAsync("Teil", 20)).Select(t => t.ArticleNumber));
        api.Should().BeEquivalentTo("BG1", "FT1");
    }

    /// <summary>v1.99.45: Wächter – Endpunkt und Warenkorb-Seite tragen dieselben Zugriffsfilter auf Klassenebene.</summary>
    [Theory]
    [InlineData(typeof(IdealAkeWms.Filters.RequireBfBestellenAccessAttribute))]
    [InlineData(typeof(IdealAkeWms.Filters.RequireBlechfertigungAktivAttribute))]
    public void ApiArtikel_GleicheZugriffsfilterWieDieSeite(Type filter)
    {
        typeof(IdealAkeWms.Controllers.Api.BfAuftraegeApiController).IsDefined(filter, false).Should().BeTrue();
        typeof(IdealAkeWms.Controllers.BfAuftraegeController).IsDefined(filter, false).Should().BeTrue();
    }

    [Fact]
    public async Task Hinzufuegen_OhneMaterialnummer_NurHinweis()
    {
        var fx = new Fx();
        fx.Artikel("BG1", materialnummer: null);

        var e = await fx.Wf.HinzufuegenAsync("BG1", 2, "EILT", null);

        e.Ok.Should().BeTrue();
        e.Text.Should().Contain("nicht an WiCAM übergeben");
        var p = fx.Db.BfAuftragPositionen.Single();
        p.Uebergeben.Should().BeFalse();
        p.Hauptlagerplatz.Should().Be("L-1");
        p.CreatedBy.Should().Be("Tester");
    }

    [Fact]
    public async Task Hinzufuegen_MengenregelUndStatusWerdenGeprueft()
    {
        var fx = new Fx();
        fx.Artikel("BG1", bestellmenge: "5");

        (await fx.Wf.HinzufuegenAsync("BG1", 7, null, null)).Ok.Should().BeFalse();
        (await fx.Wf.HinzufuegenAsync("BG1", 10, "Unbekannt", null)).Ok.Should().BeFalse();
        (await fx.Wf.HinzufuegenAsync("ZK9", 1, null, null)).Ok.Should().BeFalse();
        (await fx.Wf.HinzufuegenAsync("BG1", 10, null, null)).Ok.Should().BeTrue();
        fx.Db.BfAuftragPositionen.Should().ContainSingle();
    }

    [Fact]
    public async Task Abschicken_OhneGruppe_Gesperrt()
    {
        var fx = new Fx(gruppe: 0);
        fx.Artikel("BG1");
        var id = (await fx.Wf.HinzufuegenAsync("BG1", 1, null, null)).AuftragId!.Value;

        var e = await fx.Wf.AbschickenAsync(id, null);

        e.Ok.Should().BeFalse();
        e.Text.Should().Contain("Empfängergruppe");
        fx.Db.BfAuftraege.Single().Status.Should().Be(BfAuftragStatus.Entwurf);
    }

    [Fact]
    public async Task Abschicken_VergibtFortlaufendeNummer_UndGruppe()
    {
        var fx = new Fx();
        fx.Artikel("BG1");
        fx.Db.BfAuftraege.Add(new BfAuftrag { Nummer = "BF00000041", Status = BfAuftragStatus.Fertig, BestellerName = "x" });
        fx.Db.SaveChanges();
        var id = (await fx.Wf.HinzufuegenAsync("BG1", 1, null, null)).AuftragId!.Value;

        var e = await fx.Wf.AbschickenAsync(id, null);

        e.Ok.Should().BeTrue();
        var a = fx.Db.BfAuftraege.Single(x => x.Id == id);
        a.Nummer.Should().Be("BF00000042");
        a.Status.Should().Be(BfAuftragStatus.Bestellt);
        a.OrderRecipientGroupId.Should().Be(3);
        a.EmailSentAt.Should().BeNull();
    }

    [Fact]
    public async Task FremderWarenkorb_NichtAenderbar_SchachtlerNurBeiBestellt()
    {
        var fx = new Fx();
        fx.Artikel("BG1");
        var id = (await fx.Wf.HinzufuegenAsync("BG1", 1, null, null)).AuftragId!.Value;
        var posId = fx.Db.BfAuftragPositionen.Single().Id;

        fx.User.Setup(u => u.GetCurrentAppUserId()).Returns(99);
        (await fx.Wf.PositionEntfernenAsync(id, posId, null)).Ok.Should().BeFalse();
        (await fx.Wf.StreichenAsync(id, posId, true, null)).Ok.Should().BeFalse("Entwurf ist nicht Sache des Schachtlers");

        fx.User.Setup(u => u.GetCurrentAppUserId()).Returns(7);
        (await fx.Wf.AbschickenAsync(id, null)).Ok.Should().BeTrue();
        (await fx.Wf.PositionEntfernenAsync(id, posId, null)).Ok.Should().BeFalse("abgeschickt");
        (await fx.Wf.StreichenAsync(id, posId, true, null)).Ok.Should().BeTrue();
        fx.Db.BfAuftragPositionen.Single().Uebergeben.Should().BeFalse();
    }

    // ===================== Freigabe (Schritt C) =====================

    private static async Task<int> BestelltAsync(Fx fx, params (string Nr, string? MatNr)[] artikel)
    {
        foreach (var (nr, mat) in artikel)
        {
            fx.Artikel(nr, materialnummer: mat);
            (await fx.Wf.HinzufuegenAsync(nr, 1, nr == artikel[0].Nr ? "EILT" : null, null)).Ok.Should().BeTrue();
        }
        var id = fx.Db.BfAuftraege.Single().Id;
        (await fx.Wf.AbschickenAsync(id, null)).Ok.Should().BeTrue();
        return id;
    }

    [Fact]
    public async Task Freigabe_TermineMitFeiertag_BemerkungUndCsvOffen()
    {
        var fx = new Fx();   // Freigabetag Di 17.11.2026
        fx.Feiertage.Add(new DateTime(2026, 11, 18));
        var id = await BestelltAsync(fx, ("BG1", "11"));

        var e = await fx.Wf.FreigebenAsync(id, null);

        e.Ok.Should().BeTrue(e.Text);
        var a = fx.Db.BfAuftraege.Single();
        a.Schachteltermin.Should().Be(new DateTime(2026, 11, 19), "1 Arbeitstag, Mi 18.11. ist Feiertag");
        a.FeTermin.Should().Be(new DateTime(2026, 12, 2), "10 Arbeitstage ohne Wochenenden und Feiertag");
        (a.Status, a.CsvStatus, a.FreigegebenVon).Should().Be((BfAuftragStatus.Freigegeben, BfCsvStatus.Offen, "Tester"));
        fx.Db.BfAuftragPositionen.Single().Bemerkung.Should().Be("EILT - 17.11 - 1", "Status vorne, Bezugstag, Position bei BG");
    }

    [Fact]
    public async Task Freigabe_OhneMaterialnummer_NurHinweis_AlleOhne_Gesperrt()
    {
        var fx = new Fx();
        var id = await BestelltAsync(fx, ("BG1", "11"), ("BG2", null));

        var e = await fx.Wf.FreigebenAsync(id, null);
        e.Ok.Should().BeTrue();
        e.Text.Should().Contain("keine Materialnummer").And.Contain("Pos 2");

        var fx2 = new Fx();
        var id2 = await BestelltAsync(fx2, ("BG3", null));
        var e2 = await fx2.Wf.FreigebenAsync(id2, null);
        e2.Ok.Should().BeFalse();
        e2.Text.Should().Contain("keine Position wird an WiCAM übergeben");
        fx2.Db.BfAuftraege.Single().Status.Should().Be(BfAuftragStatus.Bestellt);
    }

    [Theory]
    [InlineData("Matchcode", "A\"B", "Anführungszeichen")]
    [InlineData("FertigungsInfoPN", "A\nB", "Zeilenumbruch")]
    [InlineData("Materialnummer", "Ł1", "nicht Windows-1252")]
    [InlineData("Hauptlagerplatz", " ", "Hauptlagerplatz fehlt")]
    [InlineData("Matchcode", "\t ", "Matchcode fehlt")]
    public void FreigabeFehler_MeldetPositionUndFeld(string feld, string wert, string erwartet)
    {
        var p = new BfAuftragPosition { Position = 3, Matchcode = "MC", Materialnummer = "1", Hauptlagerplatz = "L", Menge = 1 };
        typeof(BfAuftragPosition).GetProperty(feld)!.SetValue(p, wert);
        var a = new BfAuftrag { BestellerName = "Tester", BestellerKuerzel = "TST", Positionen = { p } };

        var fehler = BfAuftragWorkflow.FreigabeFehler(a);

        fehler.Should().ContainSingle(f => f.Contains("Pos 3") && f.Contains(erwartet));
    }

    /// <summary>v1.99.42 (P-1 (2), ersetzt B-1 Variante c): <c>;</c> nur im Lagerplatz erlaubt (wird zu <c>,</c>), in allen anderen Feldern Sperre;
    /// Materialnummer muss ganzzahlig sein.</summary>
    [Fact]
    public void FreigabeFehler_SemikolonNurImLagerplatz_MaterialnummerGanzzahlig()
    {
        BfAuftrag A(BfAuftragPosition p) => new() { BestellerName = "Tester", BestellerKuerzel = "TST", Positionen = { p } };
        BfAuftragWorkflow.FreigabeFehler(A(new BfAuftragPosition { Position = 1, Matchcode = "MC", Materialnummer = "1", Hauptlagerplatz = "S-03;0;0;0", Menge = 1 }))
            .Should().BeEmpty();
        BfAuftragWorkflow.FreigabeFehler(A(new BfAuftragPosition { Position = 1, Matchcode = "A;B", Materialnummer = "1", Hauptlagerplatz = "L", Menge = 1 }))
            .Should().ContainSingle(f => f.Contains("Pos 1 Matchcode") && f.Contains("Semikolon"));
        BfAuftragWorkflow.FreigabeFehler(A(new BfAuftragPosition { Position = 1, Matchcode = "MC", Materialnummer = "1", Hauptlagerplatz = "L", Menge = 1, Arbeitsschritte = "S;01" }))
            .Should().ContainSingle(f => f.Contains("Arbeitsschritte"));
        BfAuftragWorkflow.FreigabeFehler(A(new BfAuftragPosition { Position = 1, Matchcode = "MC", Materialnummer = "1.5", Hauptlagerplatz = "L", Menge = 1 }))
            .Should().ContainSingle(f => f.Contains("nicht ganzzahlig"));
    }

    [Fact]
    public async Task KopfAsync_LeerUndUngueltigGebenDefault()
    {
        var werte = new Dictionary<string, string?> { [BfCsv.OrderPriorityKey] = "x", [BfCsv.Remark1Key] = " R1 ", [BfCsv.CreateNewOrderKey] = "  " };
        var k = await BfCsv.KopfAsync(key => Task.FromResult(werte.GetValueOrDefault(key)));
        k.Should().Be(new BfCsvKopf(Remark1: "R1"));
        (await BfCsv.KopfAsync(key => Task.FromResult<string?>(key == BfCsv.OrderPriorityKey ? "3" : null))).Priority.Should().Be(3);
    }

    /// <summary>v1.99.42: ohne Besteller-Kuerzel (ORDER_REMARK_4) keine Freigabe; die Meldung sagt, wo es gepflegt wird.</summary>
    [Fact]
    public async Task Freigabe_OhneKuerzel_Gesperrt_MitKuerzel_Festgeschrieben()
    {
        var a = new BfAuftrag
        {
            BestellerName = "Tester", BestellerKuerzel = null,
            Positionen = { new BfAuftragPosition { Position = 1, Matchcode = "MC", Materialnummer = "1", Hauptlagerplatz = "L", Menge = 1 } },
        };
        BfAuftragWorkflow.FreigabeFehler(a).Should().ContainSingle(f => f.Contains("kein Kürzel") && f.Contains("Stammdaten › Benutzer"));

        var fx = new Fx();
        var id = await BestelltAsync(fx, ("BG1", "11"));
        fx.Db.Users.Single().Kuerzel = null;
        fx.Db.SaveChanges();
        var r = await fx.Wf.FreigebenAsync(id, null);
        r.Ok.Should().BeFalse();
        r.Text.Should().Contain("kein Kürzel");
        fx.Db.Users.Single().Kuerzel = "WAK";
        fx.Db.SaveChanges();
        (await fx.Wf.FreigebenAsync(id, null)).Ok.Should().BeTrue();
        fx.Db.BfAuftraege.Single().BestellerKuerzel.Should().Be("WAK");
    }

    [Fact]
    public void FreigabeFehler_MengenregelUndGestricheneZaehltNicht()
    {
        var a = new BfAuftrag
        {
            BestellerName = "Tester", BestellerKuerzel = "TST",
            Positionen =
            {
                new BfAuftragPosition { Position = 1, Matchcode = "MC", Materialnummer = "1", Hauptlagerplatz = "L", Menge = 7, SageBestellmenge = 5 },
                new BfAuftragPosition { Position = 2, Matchcode = "A;B", Materialnummer = "1", Hauptlagerplatz = "L", Menge = 1, Gestrichen = true },
            },
        };
        BfAuftragWorkflow.FreigabeFehler(a).Should().ContainSingle("gestrichene Pos 2 mit ; sperrt nicht")
            .Which.Should().StartWith("Pos 1").And.Contain("Vielfaches");
    }

    [Fact]
    public async Task CsvErneutSchreiben_NurNachFehler()
    {
        var fx = new Fx();
        var id = await BestelltAsync(fx, ("BG1", "11"));
        (await fx.Wf.FreigebenAsync(id, null)).Ok.Should().BeTrue();

        (await fx.Wf.CsvErneutSchreibenAsync(id, null)).Ok.Should().BeFalse("CSV ist noch offen");
        var a = fx.Db.BfAuftraege.Single();
        a.CsvStatus = BfCsvStatus.Fehler; a.CsvVersuche = 4; a.CsvFehler = BfCsv.KollisionText;
        fx.Db.SaveChanges();

        (await fx.Wf.CsvErneutSchreibenAsync(id, null)).Ok.Should().BeTrue();
        (a.CsvStatus, a.CsvVersuche, a.CsvFehler).Should().Be((BfCsvStatus.Offen, 0, (string?)null));
    }

    // ===================== Fertig / Storno (Schritt D) =====================

    [Fact]
    public async Task FertigMelden_NurFreigegebenMitGeschriebenerCsv_RestGemeldet()
    {
        var fx = new Fx();
        var id = await BestelltAsync(fx, ("BG1", "11"));
        (await fx.Wf.FreigebenAsync(id, null)).Ok.Should().BeTrue();

        var e = await fx.Wf.FertigMeldenAsync(new[] { id });
        e.Ok.Should().BeFalse("CSV noch offen");
        e.Text.Should().Contain("nicht geschrieben");

        var a = fx.Db.BfAuftraege.Single();
        a.CsvStatus = BfCsvStatus.Geschrieben;
        fx.Db.BfAuftraege.Add(new BfAuftrag { Nummer = "BF00000099", Status = BfAuftragStatus.Bestellt, BestellerName = "x" });
        fx.Db.SaveChanges();
        var andere = fx.Db.BfAuftraege.Single(x => x.Nummer == "BF00000099").Id;

        var e2 = await fx.Wf.FertigMeldenAsync(new[] { id, andere, id });
        e2.Text.Should().Contain("Fertig gemeldet: BF00000001").And.Contain("BF00000099 ist bestellt");
        (a.Status, a.FertigVon).Should().Be((BfAuftragStatus.Fertig, "Tester"));
        fx.Db.StockMovements.Should().BeEmpty("nur Status, keine Lagerbuchung");
    }

    [Fact]
    public async Task Storno_BestellerBisFreigabe_SchachtlerBisFertig_MitPoolHinweis()
    {
        var fx = new Fx();
        var id = await BestelltAsync(fx, ("BG1", "11"));
        (await fx.Wf.FreigebenAsync(id, null)).Ok.Should().BeTrue();
        var a = fx.Db.BfAuftraege.Single();
        a.CsvStatus = BfCsvStatus.Geschrieben; a.CsvDatei = @"C:\pool\BF00000001.csv";
        fx.Db.SaveChanges();

        (await fx.Wf.StornierenAsync(id, "doppelt", alsSchachtler: false, null)).Ok.Should().BeFalse("Besteller nur bis zur Freigabe");

        var e = await fx.Wf.StornierenAsync(id, " doppelt ", alsSchachtler: true, null);
        e.Ok.Should().BeTrue(e.Text);
        e.Text.Should().Contain("CSV liegt bereits im Pool").And.Contain("BF00000001.csv").And.Contain("in WiCAM löschen");
        (a.Status, a.StornoGrund, a.StorniertVon).Should().Be((BfAuftragStatus.Storniert, "doppelt", "Tester"));

        (await fx.Wf.StornierenAsync(id, null, alsSchachtler: true, null)).Ok.Should().BeFalse("schon storniert");
    }

    [Fact]
    public async Task Storno_BestellerBestellt_Ok_FremderNicht_WarenkorbNicht()
    {
        var fx = new Fx();
        var id = await BestelltAsync(fx, ("BG1", "11"));
        fx.User.Setup(u => u.GetCurrentAppUserId()).Returns(99);
        (await fx.Wf.StornierenAsync(id, null, alsSchachtler: false, null)).Ok.Should().BeFalse("fremder Auftrag");
        fx.User.Setup(u => u.GetCurrentAppUserId()).Returns(7);
        (await fx.Wf.StornierenAsync(id, null, alsSchachtler: false, null)).Ok.Should().BeTrue();

        fx.Artikel("FT9", typ: "FT");
        var entwurf = (await fx.Wf.HinzufuegenAsync("FT9", 1, null, null)).AuftragId!.Value;
        (await fx.Wf.StornierenAsync(entwurf, null, alsSchachtler: false, null)).Text.Should().Contain("Warenkorb wird nicht storniert");
    }

    // ===================== BfCsv-Regeln =====================

    [Theory]
    [InlineData("  blech  winkel\t 4301 ", "BLECH WINKEL 4301")]
    [InlineData("a\u0001b", "AB")]
    [InlineData(null, "")]
    public void Matchcode_WieExcelTrimCleanUpper(string? roh, string erwartet) => BfCsv.MatchcodeBereinigen(roh).Should().Be(erwartet);

    [Theory]
    [InlineData("EILT", "Laser", "BG", 4, "EILT - Laser - 17.11 - 4")]
    [InlineData(null, null, "FT", 4, "17.11")]
    [InlineData(null, "Laser", "bg", 2, "Laser - 17.11 - 2")]
    public void Bemerkung_StatusVorne_PositionNurBeiBg(string? status, string? info, string teileart, int pos, string erwartet) =>
        BfCsv.Bemerkung(status, info, new DateTime(2026, 11, 17), teileart, pos).Should().Be(erwartet);

    [Theory]
    [InlineData(10, "10")]
    [InlineData(2.5, "2,5")]
    [InlineData(0.125, "0,125")]
    public void Menge_GanzzahligSonstDezimalkomma(decimal m, string erwartet) => BfCsv.Menge(m).Should().Be(erwartet);

    [Fact]
    public void Kodierung_Cp1252_OhneStillesErsetzen()
    {
        BfCsv.Kodierung().GetBytes("Ä€").Should().Equal(new byte[] { 0xC4, 0x80 });
        FluentActions.Invoking(() => BfCsv.Kodierung().GetBytes("Ł")).Should().Throw<System.Text.EncoderFallbackException>();
    }
}
