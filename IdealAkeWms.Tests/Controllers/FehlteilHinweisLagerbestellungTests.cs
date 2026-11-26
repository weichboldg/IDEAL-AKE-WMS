using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.86 (Spec 2026-11-26-lagerbestellung-hinweis-fehlteil-bereits-bestellt): Hinweis im Entwurf.</summary>
public class FehlteilHinweisLagerbestellungTests
{
    // ---- Text F1/E4/E5 ----

    [Fact]
    public void FehlteilHinweis_Text_Listen_Typ_Liefertermin()
    {
        var stand = new DateTime(2026, 11, 26);
        string H(int[] ids, WarehouseRequisitionType t = WarehouseRequisitionType.Lager, DateTime? s = null, string? z = null)
            => WarehouseRequisitionsController.FehlteilHinweis(ids, t, s, z);

        H(new[] { 45 }, s: stand, z: "12.12.2026").Should()
            .Be("Hinweis: bereits als Fehlteil bestellt mit Lagerbestellung #45 (wird nachgeliefert). Nächster Liefertermin: 12.12.2026.");
        H(new[] { 45, 38, 31 }, s: stand, z: "05.11.2026 überfällig (+1)").Should()
            .Be("Hinweis: bereits als Fehlteil bestellt mit Lagerbestellung #45, #38, #31 (wird nachgeliefert). Nächster Liefertermin: 05.11.2026 überfällig (+1).");
        H(new[] { 5, 4, 3, 2, 1 }, s: stand, z: "ohne Termin").Should()
            .Be("Hinweis: bereits als Fehlteil bestellt mit Lagerbestellung #5, #4, #3 und 2 weitere (wird nachgeliefert). Nächster Liefertermin: ohne Termin.");
        H(new[] { 7 }, WarehouseRequisitionType.Glas, stand, null).Should()
            .Be("Hinweis: bereits als Fehlteil bestellt mit Glasbestellung #7 (wird nachgeliefert). Keine offene Bestellung in Sage.");
        H(new[] { 7 }, s: stand, z: "—").Should().EndWith("Keine offene Bestellung in Sage.");
        H(new[] { 7 }, s: null, z: "12.12.2026").Should().EndWith("Liefertermin unbekannt (offene Bestellungen werden nicht aus Sage gelesen).");
    }

    // ---- Edit (InMemory) ----

    private sealed class Welt
    {
        public ApplicationDbContext Db = TestDbContextFactory.Create();
        public int UserId, W1, W2;
        public Mock<ISagePurchaseOrderLineRepository> Po = new();
        public Mock<IWarehouseRequisitionRepository>? RepoSpy;
        public Mock<IPurchaseOrderReceiptRepository> Rc = new();

        public Welt(bool zuordnung = true)
        {
            var u = new User { Name = "a", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
            var w1 = new ProductionWorkplace { Name = "W1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
            var w2 = new ProductionWorkplace { Name = "W2", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
            Db.Users.Add(u); Db.ProductionWorkplaces.AddRange(w1, w2); Db.SaveChanges();
            (UserId, W1, W2) = (u.Id, w1.Id, w2.Id);
            if (zuordnung)
            {
                Db.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
                    { UserId = u.Id, ProductionWorkplaceId = w1.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
                Db.SaveChanges();
            }
            Po.Setup(p => p.GetStandAsync()).ReturnsAsync(new DateTime(2026, 11, 26, 6, 0, 0));
            Po.Setup(p => p.GetByArtikelnummernAsync(It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync(new List<SagePurchaseOrderLine>());
            Rc.Setup(r => r.GetErfasstJeBelPosAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync(new Dictionary<int, ErfasstJeBelPos>());
        }

        public int Liste(int wp, WarehouseRequisitionStatus st, ShortageStatus sh, string art, DateTime? closed = null,
            WarehouseRequisitionType typ = WarehouseRequisitionType.Lager, int? erstellerId = null)
        {
            var r = new WarehouseRequisition
            {
                ProductionWorkplaceId = wp, Status = st, Type = typ, ClosedAt = closed ?? DateTime.Now, CreatedByUserId = erstellerId,
                CreatedAt = DateTime.Now, CreatedBy = "b", CreatedByWindows = "b",
            };
            r.Items.Add(new WarehouseRequisitionItem
            {
                ArticleNumber = art, ArticleDescription = "x", QuantityRequested = 2, QuantityPicked = 1, Position = 1, ShortageStatus = sh,
                CreatedAt = DateTime.Now, CreatedBy = "b", CreatedByWindows = "b",
            });
            Db.WarehouseRequisitions.Add(r); Db.SaveChanges();
            return r.Id;
        }

        public async Task<Dictionary<string, string>?> Hinweise(int entwurfId)
        {
            var user = new Mock<ICurrentUserService>();
            user.Setup(s => s.GetCurrentAppUserId()).Returns(UserId);
            user.Setup(s => s.GetDisplayName()).Returns("a");
            IWarehouseRequisitionRepository repo = new WarehouseRequisitionRepository(Db);
            if (RepoSpy != null) repo = RepoSpy.Object;
            var ctrl = new WarehouseRequisitionsController(repo, new ProductionWorkplaceRepository(Db),
                Mock.Of<IOrderRecipientRepository>(), user.Object, Mock.Of<IAppSettingRepository>(), Mock.Of<IArticleRepository>(),
                Po.Object, Rc.Object);
            return ((WarehouseRequisitionEditViewModel)((ViewResult)await ctrl.Edit(entwurfId)).Model!).FehlteilHinweise;
        }

        public int Entwurf(WarehouseRequisitionType typ = WarehouseRequisitionType.Lager,
            WarehouseRequisitionStatus st = WarehouseRequisitionStatus.Draft, params string[] artikel)
        {
            var r = new WarehouseRequisition
            {
                ProductionWorkplaceId = W1, Status = st, Type = typ, CreatedByUserId = UserId,
                CreatedAt = DateTime.Now, CreatedBy = "a", CreatedByWindows = "a",
            };
            var pos = 1;
            foreach (var a in artikel)
                r.Items.Add(new WarehouseRequisitionItem
                {
                    ArticleNumber = a, ArticleDescription = "x", QuantityRequested = 1, Position = pos++,
                    CreatedAt = DateTime.Now, CreatedBy = "a", CreatedByWindows = "a",
                });
            Db.WarehouseRequisitions.Add(r); Db.SaveChanges();
            return r.Id;
        }
    }

    [Fact]
    public async Task Edit_EigeneWerkbank_Kollege_GrossKlein_Leerzeichen_MehrereListenNeuesteZuerst_AK1_2_5()
    {
        var w = new Welt();
        var alt = w.Liste(w.W1, WarehouseRequisitionStatus.Closed, ShortageStatus.WillBeRestocked, "X-1", new DateTime(2026, 11, 1), erstellerId: 999);
        var neu = w.Liste(w.W1, WarehouseRequisitionStatus.PartiallyDelivered, ShortageStatus.WillBeRestocked, "x-1 ", new DateTime(2026, 11, 20));
        var id = w.Entwurf(artikel: " X-1");

        var h = await w.Hinweise(id);

        h.Should().ContainKey("X-1");
        h!["x-1"].Should().Be($"Hinweis: bereits als Fehlteil bestellt mit Lagerbestellung #{neu}, #{alt} (wird nachgeliefert). Keine offene Bestellung in Sage.");
    }

    [Fact]
    public async Task Edit_KeinHinweis_FremdeWerkbank_AndererTyp_NoRestock_None_QuelleDraftOderSubmitted_AK3_8_9()
    {
        var w = new Welt();
        w.Liste(w.W2, WarehouseRequisitionStatus.Closed, ShortageStatus.WillBeRestocked, "A");
        w.Liste(w.W1, WarehouseRequisitionStatus.Closed, ShortageStatus.WillBeRestocked, "B", typ: WarehouseRequisitionType.Glas);
        w.Liste(w.W1, WarehouseRequisitionStatus.Closed, ShortageStatus.NoRestock, "C");
        w.Liste(w.W1, WarehouseRequisitionStatus.Closed, ShortageStatus.None, "D");
        w.Liste(w.W1, WarehouseRequisitionStatus.Submitted, ShortageStatus.WillBeRestocked, "E");
        w.Liste(w.W1, WarehouseRequisitionStatus.Draft, ShortageStatus.WillBeRestocked, "F");
        var id = w.Entwurf(artikel: new[] { "A", "B", "C", "D", "E", "F" });

        (await w.Hinweise(id)).Should().BeNull();
    }

    [Fact]
    public async Task Edit_Glas_HinweisMitGlasbestellung_AK8()
    {
        var w = new Welt();
        var g = w.Liste(w.W1, WarehouseRequisitionStatus.Closed, ShortageStatus.WillBeRestocked, "G", typ: WarehouseRequisitionType.Glas);
        var id = w.Entwurf(WarehouseRequisitionType.Glas, artikel: "G");

        (await w.Hinweise(id))!["G"].Should().StartWith($"Hinweis: bereits als Fehlteil bestellt mit Glasbestellung #{g} ");
    }

    [Fact]
    public async Task Edit_OhneZuordnung_KeinHinweis_AK4()
    {
        var w = new Welt(zuordnung: false);
        w.Liste(w.W1, WarehouseRequisitionStatus.Closed, ShortageStatus.WillBeRestocked, "X");
        (await w.Hinweise(w.Entwurf(artikel: "X"))).Should().BeNull();
    }

    [Fact]
    public async Task Edit_Dummy_UndAbgeschickt_KeinHinweis_KeineFehlteilAbfrage_AK10_11_F4()
    {
        var w = new Welt();
        w.Liste(w.W1, WarehouseRequisitionStatus.Closed, ShortageStatus.WillBeRestocked, "DUMMY");
        w.Liste(w.W1, WarehouseRequisitionStatus.Closed, ShortageStatus.WillBeRestocked, "X");
        var dummy = w.Entwurf(artikel: "dummy");
        var abgeschickt = w.Entwurf(st: WarehouseRequisitionStatus.Submitted, artikel: "X");

        var echt = new WarehouseRequisitionRepository(w.Db);
        w.RepoSpy = new Mock<IWarehouseRequisitionRepository>();
        w.RepoSpy.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>())).Returns((int i, bool inc) => echt.GetByIdAsync(i, inc));

        (await w.Hinweise(dummy)).Should().BeNull();
        (await w.Hinweise(abgeschickt)).Should().BeNull();
        w.RepoSpy.Verify(r => r.GetMissingPartsAsync(It.IsAny<ShortageStatus>(), It.IsAny<WarehouseRequisitionType?>(),
            It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        w.Po.Verify(p => p.GetStandAsync(), Times.Never);
    }

    [Fact]
    public async Task Edit_OhneTreffer_KeineLieferterminAbfrage_F4()
    {
        var w = new Welt();
        (await w.Hinweise(w.Entwurf(artikel: "X"))).Should().BeNull();
        w.Po.Verify(p => p.GetStandAsync(), Times.Never);
    }

    [Fact]
    public async Task Edit_Liefertermin_GleicheQuelleWieLagerDetails_AK6_7()
    {
        var w = new Welt();
        var l = w.Liste(w.W1, WarehouseRequisitionStatus.Closed, ShortageStatus.WillBeRestocked, "X");
        w.Po.Setup(p => p.GetByArtikelnummernAsync(It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync(new List<SagePurchaseOrderLine>
        {
            new() { BelPosId = 1, BelId = 1, Belegkennzeichen = "EBB", Belegnummer = "B", Artikelnummer = "X", MengeBestellt = 3, MengeOffen = 3,
                    Liefertermin = DateTime.Today.AddDays(-1), SyncedAt = DateTime.Now },
            new() { BelPosId = 2, BelId = 2, Belegkennzeichen = "EBB", Belegnummer = "C", Artikelnummer = "X", MengeBestellt = 3, MengeOffen = 3,
                    Liefertermin = DateTime.Today.AddDays(5), SyncedAt = DateTime.Now },
        });
        var erwartet = LieferterminTexte.Zelle(new (DateTime?, decimal)[] { (DateTime.Today.AddDays(-1), 3m), (DateTime.Today.AddDays(5), 3m) }, DateTime.Today);

        (await w.Hinweise(w.Entwurf(artikel: "X")))!["X"].Should()
            .Be($"Hinweis: bereits als Fehlteil bestellt mit Lagerbestellung #{l} (wird nachgeliefert). Nächster Liefertermin: {erwartet}.")
            .And.Contain("überfällig (+1)");
    }

    [Fact]
    public void View_RendertHinweisInDerArtikelNrZelle_F1_F2()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        var view = File.ReadAllText(Path.Combine(dir!.FullName, "IdealAkeWms", "Views", "WarehouseRequisitions", "Edit.cshtml"));
        view.Should().Contain("Model.FehlteilHinweise?.TryGetValue(i.ArticleNumber.Trim(), out var fehlteilHinweis) == true")
            .And.Contain("<div class=\"small text-warning-emphasis\">@fehlteilHinweis</div>");
    }
}
