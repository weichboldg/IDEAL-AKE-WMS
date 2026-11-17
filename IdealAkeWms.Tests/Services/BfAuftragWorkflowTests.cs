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
        public BfAuftragRepository Repo { get; }
        public BfAuftragWorkflow Wf { get; }

        public Fx(int gruppe = 3, int userId = 7)
        {
            Repo = new BfAuftragRepository(Db);
            Settings.Setup(s => s.GetValueAsync(AppSettingKeys.BfStatuswerte)).ReturnsAsync("EILT,Muster");
            Settings.Setup(s => s.GetIntValueAsync(AppSettingKeys.BfEmpfaengergruppeId, 0)).ReturnsAsync(gruppe);
            Groups.Setup(g => g.GetGroupByIdAsync(3)).ReturnsAsync(new OrderRecipientGroup { Id = 3, Name = "Blech" });
            User.Setup(u => u.GetCurrentAppUserId()).Returns(userId);
            User.Setup(u => u.GetDisplayName()).Returns("Tester");
            User.Setup(u => u.GetWindowsUserName()).Returns("DOM\\tester");
            Wf = new BfAuftragWorkflow(Repo, Settings.Object, Groups.Object, Lp.Object, User.Object,
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

        public void Artikel(string nr, string? ist = "1", string? typ = "BG", string? materialnummer = "M-1", string? bestellmenge = null)
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
        treffer.Single(t => t.ArticleNumber == "BG1").Should().Match<BfArtikel>(t => t.Materialnummer == "M-1" && t.Hauptlagerplatz == "L-1" && t.Teileart == "BG");
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
}
