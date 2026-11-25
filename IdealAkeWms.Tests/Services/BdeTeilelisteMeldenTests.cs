using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.32 (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung, Etappe 3): Melden einzeln/Block, Doppelschutz, Baugruppen-AG.</summary>
public class BdeTeilelisteMeldenTests
{
    internal sealed record Umgebung(ApplicationDbContext Ctx, BdeTeilelisteService Svc, BdeBookingTestSeed.Ids Ids);

    internal static async Task<Umgebung> AufbauAsync(bool option = true, bool nurFa = false, DateTime? schichtende = null)
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wp = (await ctx.ProductionWorkplaces.FindAsync(ids.WorkplaceId))!;
        wp.ArbeitsschrittCode = "KA";
        wp.BdeTeileliste = option;
        ctx.FaHierarchyNodes.AddRange(
            new FaHierarchyNode { HauptFA = 100, VaterFA = 100, SubFA = 200, Position = 1, Artnr = "BG", Matchcode = "Korpus", Arbeitsschritte = "KA ST", Sollmenge = 1 },
            new FaHierarchyNode { HauptFA = 100, VaterFA = 200, SubFA = 0, Position = 1, Artnr = "B1", Matchcode = "Blech 1", Arbeitsschritte = "KA", Sollmenge = 4 },
            new FaHierarchyNode { HauptFA = 100, VaterFA = 200, SubFA = 0, Position = 2, Artnr = "B2", Matchcode = "Blech 2", Arbeitsschritte = "LA", Sollmenge = 2 });
        await ctx.SaveChangesAsync();

        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("terminal");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\terminal");
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeNurFaMeldung)).ReturnsAsync(nurFa ? "true" : "false");
        settings.Setup(s => s.GetIntValueAsync(AppSettingKeys.BdeTeilelisteLagerbestellungArbeitstage, 10)).ReturnsAsync(10); // v1.99.72
        var booking = new BdeBookingService(ctx, user.Object, settings.Object);
        var svc = new BdeTeilelisteService(new FaHierarchyNodeRepository(ctx), new FaHierarchyOrderInfoRepository(ctx), ctx,
            booking, user.Object, settings.Object,
            Mock.Of<IBdeShiftCalendarService>(c => c.GetShiftEndForBookingAsync(It.IsAny<int>(), It.IsAny<DateTime>()) == Task.FromResult(schichtende)),
            new ArticleRepository(ctx), new BusinessDayService(), Mock.Of<IHolidayRepository>());
        return new Umgebung(ctx, svc, ids);
    }

    internal static readonly TeilSchluessel Blech1 = new(100, 200, 0, 1, "B1");
    internal static readonly TeilSchluessel Korpus = new(100, 100, 200, 1, "BG");

    private static BdeTeilelisteService.MeldeAuftrag A(TeilSchluessel s, decimal? gut, decimal? ausschuss) => new(s, gut, ausschuss);

    private static Task<BdeTeilelisteService.MeldeErgebnis> Melden(Umgebung u, params BdeTeilelisteService.MeldeAuftrag[] a) =>
        u.Svc.MeldenAsync(u.Ids.OperatorId, u.Ids.WorkplaceId, u.Ids.TerminalId, a);

    [Fact]
    public async Task Einzeln_SpeichertTeilmeldungMitMengenUndAudit()
    {
        var u = await AufbauAsync();

        var r = await Melden(u, A(Blech1, 3, 1));

        r.Gemeldet.Should().Be(1);
        r.AllesGut.Should().BeTrue();
        var m = u.Ctx.BdeTeilMeldungen.Single();
        (m.GoodQuantity, m.ScrapQuantity, m.SollmengeBeiMeldung).Should().Be((3m, 1m, 4m));
        (m.Kuerzel, m.Matchcode, m.BdeOperatorId, m.BdeTerminalId).Should().Be(("KA", "Blech 1", u.Ids.OperatorId, u.Ids.TerminalId));
        (m.CreatedBy, m.CreatedByWindows).Should().Be(("terminal", "AKE\\terminal"));
        m.BdeBookingId.Should().BeNull("Blechteil meldet nur die Teilmeldung");
        u.Ctx.BdeBookings.Should().BeEmpty();
    }

    [Fact]
    public async Task VerschwundeneZeile_Abgewiesen_KeineMeldung()
    {
        // v1.99.37 (ADR 0032, Rueckfrage 3): sichtbar mit Badge, aber nicht meldbar.
        var u = await AufbauAsync();
        var b1 = u.Ctx.FaHierarchyNodes.Single(n => n.Artnr == "B1");
        b1.VerschwundenSeit = DateTime.Now;
        b1.VerschwundenGrund = FaStrukturGrund.Beendet;
        await u.Ctx.SaveChangesAsync();

        var r = await Melden(u, A(Blech1, null, null));

        r.Gemeldet.Should().Be(0);
        r.Abgewiesen.Should().ContainSingle().Which.Should().EndWith("FA in Sage beendet – nicht meldbar");
        u.Ctx.BdeTeilMeldungen.Should().BeEmpty();
    }

    [Fact]
    public void TeilelisteZeile_Verschwunden_NichtMeldbar()
    {
        var n = new FaHierarchyNode { HauptFA = 1, SubFA = 0, VerschwundenSeit = DateTime.Now, VerschwundenGrund = FaStrukturGrund.Unklar };
        new TeilelisteZeile { Node = n, Schluessel = TeilSchluessel.Von(n) }.Meldbar.Should().BeFalse();
    }

    [Fact]
    public async Task Block_OhneMengen_VolleSollmenge()
    {
        var u = await AufbauAsync();

        await Melden(u, A(Blech1, null, null));

        var m = u.Ctx.BdeTeilMeldungen.Single();
        (m.GoodQuantity, m.ScrapQuantity).Should().Be((4m, 0m));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -2)]
    [InlineData(0, 0)]
    public async Task UngueltigeMengen_NichtsGespeichert(int gut, int ausschuss)
    {
        var u = await AufbauAsync();

        var r = await Melden(u, A(Blech1, gut, ausschuss));

        r.Gemeldet.Should().Be(0);
        r.Abgewiesen.Should().ContainSingle();
        u.Ctx.BdeTeilMeldungen.Should().BeEmpty();
    }

    [Fact]
    public async Task Doppelmeldung_BereitsGemeldet_NichtsGeaendert()
    {
        var u = await AufbauAsync();
        await Melden(u, A(Blech1, null, null));

        var r = await Melden(u, A(Blech1, 1, 0));

        r.Gemeldet.Should().Be(0);
        r.BereitsGemeldet.Should().ContainSingle().Which.Should().Contain("bereits gemeldet von").And.Contain("Blech 1");
        u.Ctx.BdeTeilMeldungen.Should().ContainSingle().Which.GoodQuantity.Should().Be(4);
    }

    [Fact]
    public async Task ZeileOhneKuerzelOderWeg_Abgewiesen()
    {
        var u = await AufbauAsync();

        var r = await Melden(u, A(new TeilSchluessel(100, 200, 0, 2, "B2"), null, null), A(new TeilSchluessel(100, 200, 0, 9, "X"), null, null));

        r.Abgewiesen.Should().HaveCount(2).And.OnlyContain(t => t.Contains("nicht mehr in der FA-Struktur"));
        u.Ctx.BdeTeilMeldungen.Should().BeEmpty();
    }

    [Fact]
    public async Task VerpackterBesitzerAuftrag_Abgewiesen()
    {
        var u = await AufbauAsync();
        var po = new ProductionOrder { OrderNumber = "100", SubOrderNumber = "200", CreatedBy = "t", CreatedByWindows = "t" };
        u.Ctx.ProductionOrders.Add(po);
        await u.Ctx.SaveChangesAsync();
        await BdeBookingTestSeed.SetSageStatusAsync(u.Ctx, po.Id, "verpackt");

        var r = await Melden(u, A(Blech1, null, null));

        r.Abgewiesen.Should().ContainSingle().Which.Should().Contain("verpackt");
        u.Ctx.BdeTeilMeldungen.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)] // v1.99.76 (G5): BdeNurFaMeldung allein weist nicht mehr ab, nur die fehlende Option
    public async Task NichtWirksameWerkbank_Abgewiesen(bool option, bool nurFa)
    {
        var u = await AufbauAsync(option, nurFa);

        var r = await Melden(u, A(Blech1, null, null));

        r.Abgewiesen.Should().ContainSingle().Which.Should().Contain("Teileliste");
        u.Ctx.BdeTeilMeldungen.Should().BeEmpty();
    }

    [Fact]
    public async Task Baugruppe_MeldetEigenenArbeitsgangFertig_VaterUndHauptUnberuehrt()
    {
        var u = await AufbauAsync();
        var sub = new ProductionOrder { OrderNumber = "100", SubOrderNumber = "200", CreatedBy = "t", CreatedByWindows = "t" };
        var haupt = new ProductionOrder { OrderNumber = "100", SubOrderNumber = "", CreatedBy = "t", CreatedByWindows = "t" };
        u.Ctx.ProductionOrders.AddRange(sub, haupt);
        await u.Ctx.SaveChangesAsync();
        var agSub = new WorkOperation { ProductionOrderId = sub.Id, OperationNumber = "ka", Name = "Kanten", ProductionWorkplaceId = u.Ids.WorkplaceId, CreatedBy = "t", CreatedByWindows = "t" };
        var agHaupt = new WorkOperation { ProductionOrderId = haupt.Id, OperationNumber = "KA", Name = "Kanten", ProductionWorkplaceId = u.Ids.WorkplaceId, CreatedBy = "t", CreatedByWindows = "t" };
        u.Ctx.WorkOperations.AddRange(agSub, agHaupt);
        await u.Ctx.SaveChangesAsync();

        var r = await Melden(u, A(Korpus, null, null));

        r.AllesGut.Should().BeTrue(r.Text());
        var b = u.Ctx.BdeBookings.Single();
        b.WorkOperationId.Should().Be(agSub.Id);
        (b.Status, b.StartedAt).Should().Be((BdeBookingStatus.Finished, b.EndedAt!.Value));
        u.Ctx.BdeBookingQuantities.Single().IsFinal.Should().BeTrue();
        u.Ctx.BdeTeilMeldungen.Single().BdeBookingId.Should().Be(b.Id);
    }

    [Fact]
    public async Task Baugruppe_OhneArbeitsgang_TeilmeldungMitHinweis()
    {
        var u = await AufbauAsync();

        var r = await Melden(u, A(Korpus, null, null));

        r.Gemeldet.Should().Be(1);
        r.Hinweise.Should().ContainSingle().Which.Should().Contain("nicht vorhanden");
        u.Ctx.BdeTeilMeldungen.Single().BdeBookingId.Should().BeNull();
    }

    [Fact]
    public async Task Baugruppe_ArbeitsgangSchonFertig_TeilmeldungMitHinweis()
    {
        var u = await AufbauAsync();
        var sub = new ProductionOrder { OrderNumber = "100", SubOrderNumber = "200", CreatedBy = "t", CreatedByWindows = "t" };
        u.Ctx.ProductionOrders.Add(sub);
        await u.Ctx.SaveChangesAsync();
        var ag = new WorkOperation { ProductionOrderId = sub.Id, OperationNumber = "KA", Name = "Kanten", ProductionWorkplaceId = u.Ids.WorkplaceId, CreatedBy = "t", CreatedByWindows = "t" };
        u.Ctx.WorkOperations.Add(ag);
        await u.Ctx.SaveChangesAsync();
        var booking = new BdeBookingService(u.Ctx, Mock.Of<ICurrentUserService>(x => x.GetDisplayName() == "t" && x.GetWindowsUserName() == "t"), Mock.Of<IAppSettingRepository>(s => s.GetValueAsync(AppSettingKeys.BdeNurFaMeldung) == Task.FromResult<string?>("false")));
        (await booking.ReportCompletionAsync(u.Ids.OperatorId, ag.Id, u.Ids.WorkplaceId, u.Ids.TerminalId, 1, 0)).Outcome.Should().Be(BdeBookingOutcome.Success,
            "der bestehende Weg akzeptiert jetzt auch Teileliste-Werkbaenke");

        var r = await Melden(u, A(Korpus, null, null));

        r.Gemeldet.Should().Be(1);
        r.Hinweise.Should().ContainSingle().Which.Should().Contain("bereits fertig");
        u.Ctx.BdeBookings.Should().ContainSingle();
    }

    [Fact]
    public void Sperre_HoechstensZweihundertfuenfundfuenfzigZeichen()
    {
        var lang = new TeilSchluessel(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, new string('A', 200));
        BdeTeilelisteService.Sperre(lang, "KA").Length.Should().BeLessThanOrEqualTo(255);
        BdeTeilelisteService.Sperre(Blech1, "ka").Should().Be(BdeTeilelisteService.Sperre(Blech1, "KA"));
    }

    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("1,5", 1.5)]
    [InlineData(" 2 ", 2)]
    [InlineData("-1", -1)]
    public void TryMenge_KulturUnabhaengig(string text, double erwartet)
    {
        IdealAkeWms.Controllers.BdeTeilelisteController.TryMenge(text, out var w).Should().BeTrue();
        w.Should().Be((decimal)erwartet);
    }

    [Fact]
    public void TryMenge_LeerIstNull_TextIstFehler()
    {
        IdealAkeWms.Controllers.BdeTeilelisteController.TryMenge("", out var leer).Should().BeTrue();
        leer.Should().BeNull();
        IdealAkeWms.Controllers.BdeTeilelisteController.TryMenge("abc", out _).Should().BeFalse();
    }

    // ------------------------------------------------------------ Etappe 4: Ruecknahme (F13)

    private static async Task<BdeTeilMeldung> GemeldetAsync(Umgebung u, TeilSchluessel k)
    {
        (await Melden(u, A(k, null, null))).Gemeldet.Should().Be(1);
        return u.Ctx.BdeTeilMeldungen.Single(m => !m.IsCancelled && m.Position == k.Position && m.SubFA == k.SubFA);
    }

    [Fact]
    public async Task Ruecknahme_EigeneVorFrist_ZeileWiederOffen()
    {
        var u = await AufbauAsync(schichtende: DateTime.Now.AddHours(1));
        var m = await GemeldetAsync(u, Blech1);

        var (ok, _) = await u.Svc.ZuruecknehmenAsync(m.Id, u.Ids.OperatorId, null);

        ok.Should().BeTrue();
        m.IsCancelled.Should().BeTrue();
        m.CancellationReason.Should().Contain("Werker");
        (m.ModifiedBy, m.ModifiedByWindows).Should().Be(("terminal", "AKE\\terminal"));
        (await Melden(u, A(Blech1, null, null))).Gemeldet.Should().Be(1, "nach der Ruecknahme ist die Zeile wieder meldbar");
    }

    [Fact]
    public async Task Ruecknahme_FremderWerker_Abgewiesen()
    {
        var u = await AufbauAsync();
        var m = await GemeldetAsync(u, Blech1);

        var (ok, text) = await u.Svc.ZuruecknehmenAsync(m.Id, u.Ids.OperatorId + 1000, null);

        ok.Should().BeFalse();
        text.Should().Contain("Nur eigene");
        m.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task Ruecknahme_NachSchichtende_NurAdmin()
    {
        var u = await AufbauAsync(schichtende: DateTime.Now.AddMinutes(-1));
        var m = await GemeldetAsync(u, Blech1);

        var (ok, text) = await u.Svc.ZuruecknehmenAsync(m.Id, u.Ids.OperatorId, null);

        ok.Should().BeFalse();
        text.Should().Contain("BDE-Admin");
        (await u.Svc.ZuruecknehmenAsync(m.Id, null, " ")).Ok.Should().BeFalse("Admin braucht einen Grund");
        (await u.Svc.ZuruecknehmenAsync(m.Id, null, "Fehlbuchung")).Ok.Should().BeTrue();
        m.CancellationReason.Should().Be("Fehlbuchung");
        (await u.Svc.ZuruecknehmenAsync(m.Id, null, "nochmal")).Text.Should().Contain("bereits zurückgenommen");
    }

    [Fact]
    public async Task Frist_OhneSchicht_EndeDesMeldetags()
    {
        var u = await AufbauAsync(schichtende: null);
        var m = new BdeTeilMeldung { ProductionWorkplaceId = u.Ids.WorkplaceId, GemeldetAm = new DateTime(2026, 11, 17, 9, 30, 0) };

        (await u.Svc.RuecknahmeFristAsync(m)).Should().Be(new DateTime(2026, 11, 17, 23, 59, 59));
    }

    [Fact]
    public async Task Ruecknahme_Baugruppe_StorniertAgBuchung()
    {
        var u = await AufbauAsync();
        var sub = new ProductionOrder { OrderNumber = "100", SubOrderNumber = "200", CreatedBy = "t", CreatedByWindows = "t" };
        u.Ctx.ProductionOrders.Add(sub);
        await u.Ctx.SaveChangesAsync();
        u.Ctx.WorkOperations.Add(new WorkOperation { ProductionOrderId = sub.Id, OperationNumber = "KA", Name = "Kanten", ProductionWorkplaceId = u.Ids.WorkplaceId, CreatedBy = "t", CreatedByWindows = "t" });
        await u.Ctx.SaveChangesAsync();
        var m = await GemeldetAsync(u, Korpus);

        (await u.Svc.ZuruecknehmenAsync(m.Id, u.Ids.OperatorId, null)).Ok.Should().BeTrue();

        var b = u.Ctx.BdeBookings.Single();
        b.IsCancelled.Should().BeTrue();
        b.CancellationReason.Should().Be("Teilmeldung zurückgenommen");
        b.ModifiedBy.Should().Be("terminal");
    }
}
