using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E9/F11: Uebergabe-Status + Outbox bei WMS-Neuanlage (nichts wird gesendet),
/// Bestand NichtNoetig, Liste „Offene Übergaben an Sage“, Zuordnung -> Uebergeben.</summary>
public class SageUebergabeTests
{
    private static Mock<ICurrentUserService> User()
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Anwender Test");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\test");
        user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        return user;
    }

    private static BdeMasterDataController BdeController(ApplicationDbContext ctx)
    {
        var actRepo = new Mock<IBdeActivityRepository>();
        actRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BdeActivity>());
        var termRepo = new Mock<IBdeTerminalRepository>();
        termRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BdeTerminal>());
        var ctrl = new BdeMasterDataController(new BdeOperatorRepository(ctx), actRepo.Object, termRepo.Object,
            new Mock<IUserRepository>().Object, new Mock<IProductionWorkplaceRepository>().Object, User().Object);
        var http = new DefaultHttpContext();
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    private static StorageLocation Platz(string code, SageUebergabeStatus status = SageUebergabeStatus.NichtNoetig) => new()
    {
        Code = code, BarcodeValue = code, SageUebergabeStatus = status, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
    };

    [Fact]
    public async Task Lagerplatz_Neuanlage_StehtAufOffen_MitOutboxEintrag_NichtsGesendet()
    {
        using var ctx = TestDbContextFactory.Create();
        var ctrl = new StorageLocationsController(new StorageLocationRepository(ctx), User().Object);

        await ctrl.Create(new StorageLocation { Code = "W-01", Description = "Neu im WMS", Zone = "Z1" });

        var l = ctx.StorageLocations.Single();
        (l.SageUebergabeStatus, l.Source, l.SageUebergabeAm).Should().Be((SageUebergabeStatus.Offen, StorageLocationSource.Manual, (DateTime?)null));
        var o = ctx.SageStammdatenOutbox.Single();
        (o.EntityType, o.EntityKey, o.Status, o.AttemptCount, o.CreatedBy, o.CreatedByWindows)
            .Should().Be((SageStammdatenOutbox.TypLagerplatz, l.Id.ToString(), SageBookingQueueStatus.Offen, 0, "Anwender Test", "AKE\\test"));
        o.PayloadJson.Should().Contain("\"Code\":\"W-01\"").And.Contain("Neu im WMS");
    }

    [Fact]
    public async Task Lagerplatz_Bestand_StehtAufNichtNoetig_OhneOutbox()
    {
        using var ctx = TestDbContextFactory.Create();
        await new StorageLocationRepository(ctx).AddAsync(Platz("ALT"));   // z. B. Seed NAN, Sage-Abgleich: kein Controller-Create

        ctx.StorageLocations.Single().SageUebergabeStatus.Should().Be(SageUebergabeStatus.NichtNoetig);
        ctx.SageStammdatenOutbox.Should().BeEmpty();
    }

    [Fact]
    public async Task Lagerplatz_Liste_OffeneUebergaben_FiltertOffenUndFehler_ZaehltNurAktive()
    {
        using var ctx = TestDbContextFactory.Create();
        var inaktiv = Platz("D", SageUebergabeStatus.Offen);
        inaktiv.IsActive = false;
        ctx.StorageLocations.AddRange(Platz("A"), Platz("B", SageUebergabeStatus.Offen), Platz("C", SageUebergabeStatus.Fehler),
            Platz("E", SageUebergabeStatus.Uebergeben), inaktiv);
        await ctx.SaveChangesAsync();
        var ctrl = new StorageLocationsController(new StorageLocationRepository(ctx), User().Object);

        var alle = (ViewResult)await ctrl.Index();
        ((List<StorageLocation>)alle.Model!).Should().HaveCount(4);
        ((int)ctrl.ViewBag.UebergabeOffenCount).Should().Be(2);

        var offen = (ViewResult)await ctrl.Index(onlyUebergabeOffen: true);
        ((List<StorageLocation>)offen.Model!).Select(l => l.Code).Should().Equal("B", "C");
        ((PaginationState)ctrl.ViewBag.Pagination).TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task BdeMitarbeiter_Neuanlage_Offen_MitOutbox_Liste_Zuordnung_Uebergeben()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.BdeOperators.Add(new BdeOperator
        {
            PersonnelNumber = "1", FirstName = "Alt", LastName = "Bestand", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        });
        ctx.SagePpsMitarbeiter.Add(new SagePpsMitarbeiter { Nummer = "4711", Nachname = "Huber", Vorname = "Max", SyncedAt = DateTime.Now });
        await ctx.SaveChangesAsync();
        var ctrl = BdeController(ctx);

        await ctrl.EditOperator(new BdeOperatorEditViewModel { PersonnelNumber = "4711", FirstName = "Max", LastName = "Huber", IsActive = true });

        var neu = ctx.BdeOperators.Single(o => o.PersonnelNumber == "4711");
        (neu.SageUebergabeStatus, neu.Source).Should().Be((SageUebergabeStatus.Offen, StorageLocationSource.Manual));
        ctx.BdeOperators.Single(o => o.PersonnelNumber == "1").SageUebergabeStatus.Should().Be(SageUebergabeStatus.NichtNoetig);
        var o = ctx.SageStammdatenOutbox.Single();
        (o.EntityType, o.EntityKey, o.Status).Should().Be((SageStammdatenOutbox.TypBdeMitarbeiter, neu.Id.ToString(), SageBookingQueueStatus.Offen));

        await ctrl.Index(tab: "operators", onlyUebergabeOffen: true);
        ((List<BdeOperator>)ctrl.ViewBag.Operators).Select(x => x.PersonnelNumber).Should().Equal("4711");
        ((int)ctrl.ViewBag.UebergabeOffenCount).Should().Be(1);

        await ctrl.SageZuordnen("4711", neu.Id);

        neu = ctx.BdeOperators.Single(x => x.PersonnelNumber == "4711");
        (neu.SageMitarbeiternummer, neu.SageUebergabeStatus, neu.Source, neu.SageUebergabeAm != null)
            .Should().Be(("4711", SageUebergabeStatus.Uebergeben, StorageLocationSource.Sage, true));
        ctx.SageStammdatenOutbox.Single().Status.Should().Be(SageBookingQueueStatus.Bestaetigt, "nichts mehr zu senden");
        ctx.SageStammdatenOutbox.Single().ModifiedBy.Should().Be("Anwender Test");
    }

    [Fact]
    public async Task BdeMitarbeiter_Bestand_Zuordnung_BleibtNichtNoetig_LoesenSetztHerkunftZurueck()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.BdeOperators.Add(new BdeOperator
        {
            PersonnelNumber = "1", FirstName = "Alt", LastName = "Bestand", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        });
        ctx.SagePpsMitarbeiter.Add(new SagePpsMitarbeiter { Nummer = "1", SyncedAt = DateTime.Now });
        await ctx.SaveChangesAsync();
        var ctrl = BdeController(ctx);
        var id = ctx.BdeOperators.Single().Id;

        await ctrl.SageZuordnen("1", id);
        var op = ctx.BdeOperators.Single();
        (op.SageUebergabeStatus, op.Source, op.SageUebergabeAm).Should().Be((SageUebergabeStatus.NichtNoetig, StorageLocationSource.Sage, (DateTime?)null));

        await ctrl.SageZuordnungLoesen(id);
        (op.SageMitarbeiternummer, op.Source).Should().Be(((string?)null, StorageLocationSource.Manual));
    }
}
