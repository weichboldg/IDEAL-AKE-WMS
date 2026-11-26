using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class BdeTerminalControllerTests
{
    private static BdeTerminalController CreateTerminalController(ApplicationDbContext ctx, bool multiMa = false, bool multiOp = false, bool groupFinish = false, bool nurFa = false,
        Mock<IBdeTerminalRepository>? terminals = null, ICurrentUserService? user = null)
    {
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");
        userSvc.Setup(u => u.GetCurrentAppUserId()).Returns(1);

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeMehrfachBuchungProArbeitsgang))
            .ReturnsAsync(multiMa ? "true" : "false");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeMehrfachBuchungProOperator))
            .ReturnsAsync(multiOp ? "true" : "false");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeGleichzeitigerAbschlussBeiMehrfachStart))
            .ReturnsAsync(groupFinish ? "true" : "false");
        settings.Setup(s => s.GetValueAsync("BdeNurFaMeldung"))
            .ReturnsAsync(nurFa ? "true" : "false");

        var bookingSvc = new BdeBookingService(ctx, userSvc.Object, settings.Object);
        var defaultWoSvc = new BdeDefaultWorkOperationService(ctx, settings.Object);

        if (terminals == null)
        {
            // v1.99.79 F-28: Standard = das geseedete Terminal gehoert dem angemeldeten Benutzer
            terminals = new Mock<IBdeTerminalRepository>();
            terminals.Setup(t => t.GetByUserIdAsync(1)).ReturnsAsync(() => ctx.BdeTerminals.FirstOrDefault());
        }

        return new BdeTerminalController(terminals.Object, bookingSvc, user ?? userSvc.Object,
            defaultWoSvc, settings.Object, ctx, NullLogger<BdeTerminalController>.Instance);
    }

    [Fact]
    public async Task CloseOthers_DelegatesToService_ReturnsCount()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = op2, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-30),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        // v1.99.81 (2A): schliessen darf nur, wer auf dem AG selbst fertig gemeldet hat
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            DateTime.Now.AddHours(-1), DateTime.Now));
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx);
        var result = await controller.CloseOthers(ids.WorkOperationId, ids.OperatorId, ids.TerminalId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        json.Should().Contain("\"closedCount\":1");
    }

    [Fact]
    public async Task Finish_MultiMaEnabledAndFinalQty_IncludesOtherActiveBookings()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        var own = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(own);
        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = op2, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-30),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx, multiMa: true);

        var result = await controller.Finish(own.Id, ids.OperatorId, ids.TerminalId, goodQty: 5, scrapQty: 0);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"otherActiveBookings\"");
        json.Should().Contain($"\"operatorId\":{op2}");
    }

    // v1.99.79 F-28: fremde terminalId aus dem Client wird abgewiesen, es entsteht keine Buchung.
    [Fact]
    public async Task StartProduction_FremdeTerminalId_Abgewiesen_KeineBuchung()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var result = await CreateTerminalController(ctx)
            .StartProduction(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId + 999);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"outcome\":\"InvalidState\"").And.Contain("nicht dem angemeldeten Benutzer");
        ctx.BdeBookings.Count().Should().Be(0);
    }

    // v1.63.0 (AK 30): Fertigmeldung bei erlaubter Mehrfachbuchung -> Antwort traegt die anderen Laufenden.
    [Fact]
    public async Task ReportCompletion_MultiMa_SavedAndIncludesOtherActiveBookings()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        (await ctx.ProductionWorkplaces.FindAsync(ids.WorkplaceId))!.BdeNurAgFertigmeldung = true;
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = op2, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-30),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx, multiMa: true);
        var result = await controller.ReportCompletion(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId, 10, 0);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"outcome\":\"Success\"");
        json.Should().Contain($"\"operatorId\":{op2}");
        json.Should().Contain($"\"workOperationId\":{ids.WorkOperationId}");
    }

    [Fact]
    public async Task Finish_NoQuantity_EmptyOtherActiveBookings()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var own = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(own);
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx, multiMa: true);

        var result = await controller.Finish(own.Id, ids.OperatorId, ids.TerminalId, goodQty: null, scrapQty: null);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"otherActiveBookings\":[]");
    }

    [Fact]
    public async Task Finish_GroupSettingOnAndMultipleActive_ReturnsGroupFinishRequired()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        var b1 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        var b2 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-30), workOperationId: wo2);
        ctx.BdeBookings.AddRange(b1, b2);
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx, multiOp: true, groupFinish: true);

        var result = await controller.Finish(b1.Id, ids.OperatorId, ids.TerminalId, goodQty: 5m, scrapQty: 0m);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"outcome\":\"GroupFinishRequired\"");
        json.Should().Contain($"\"bookingId\":{b1.Id}");
        json.Should().Contain($"\"bookingId\":{b2.Id}");
    }

    // QA v1.63.0 (FA-a9/Antwort 12): StartProductionForOrder prueft VOR dem Anlegen des Default-AG „01".
    [Theory]
    [InlineData(true, false)]  // NurFA an, FA an anderer/keiner Werkbank
    [InlineData(false, true)]  // Normal-Modus: Endpunkt wird nicht angeboten, auch FA der eigenen Werkbank
    public async Task StartProductionForOrder_NotOffered_Rejected_NoDefaultAgCreated(bool nurFa, bool orderAtTerminal)
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        if (orderAtTerminal)
        {
            (await ctx.ProductionOrders.FindAsync(ids.ProductionOrderId))!.ProductionWorkplaceId = ids.WorkplaceId;
            await ctx.SaveChangesAsync();
        }
        var before = ctx.WorkOperations.Count();

        var result = await CreateTerminalController(ctx, nurFa: nurFa)
            .StartProductionForOrder(ids.OperatorId, ids.ProductionOrderId, ids.WorkplaceId, ids.TerminalId);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"outcome\":\"InvalidState\"");
        ctx.WorkOperations.Count().Should().Be(before);
        ctx.BdeBookings.Count().Should().Be(0);
    }

    [Fact]
    public async Task StartProductionForOrder_NurFa_OrderOfTerminalWorkplace_Started()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        (await ctx.ProductionOrders.FindAsync(ids.ProductionOrderId))!.ProductionWorkplaceId = ids.WorkplaceId;
        (await ctx.ProductionWorkplaces.FindAsync(ids.WorkplaceId))!.BdeDefaultArbeitsgang = "PRODUKTION";
        await ctx.SaveChangesAsync();

        var result = await CreateTerminalController(ctx, nurFa: true)
            .StartProductionForOrder(ids.OperatorId, ids.ProductionOrderId, ids.WorkplaceId, ids.TerminalId);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"outcome\":\"Success\"");
        ctx.WorkOperations.Should().Contain(w => w.ProductionOrderId == ids.ProductionOrderId && w.OperationNumber == "01");
    }

    // v1.99.78 (Spec 2026-11-25-bde-masken-vereinheitlichung, Etappe 5): BdeTerminal/Index leitet an beiden Standorten IMMER auf die
    // BDE-Seite weiter (Werkbank aus Query, sonst Terminal-Vorgabe); Login- und Terminal-Pruefung bleiben. Ersetzt Index_HierarchischUndMaxHauptFa.
    [Theory]
    [InlineData(null, 65)]
    [InlineData(12, 12)]
    public async Task Index_LeitetImmerAufBdeSeiteWeiter(int? query, int erwartetWerkbank)
    {
        var ctx = TestDbContextFactory.Create();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(1);
        var terminals = new Mock<IBdeTerminalRepository>();
        terminals.Setup(t => t.GetByUserIdAsync(1)).ReturnsAsync(new BdeTerminal { Id = 7, DefaultProductionWorkplaceId = 65 });

        var r = await CreateTerminalController(ctx, terminals: terminals, user: user.Object).Index(query);

        var redirect = r.Should().BeOfType<RedirectToActionResult>().Subject;
        (redirect.ControllerName, redirect.ActionName).Should().Be(("BdeTeileliste", "Index"));
        redirect.RouteValues!["workplaceId"].Should().Be(erwartetWerkbank);
    }

    [Fact]
    public async Task Index_OhneTerminal_Startseite_OhneAppUser_Login()
    {
        var ctx = TestDbContextFactory.Create();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(1);
        var c = CreateTerminalController(ctx, terminals: new Mock<IBdeTerminalRepository>(), user: user.Object);
        c.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

        var ohneTerminal = (await c.Index(null)).Should().BeOfType<RedirectToActionResult>().Subject;
        (ohneTerminal.ControllerName, ohneTerminal.ActionName).Should().Be(("Home", "Index"));
        ((string)c.TempData["WarningMessage"]!).Should().Contain("kein Terminal konfiguriert");

        var ohneUser = (await CreateTerminalController(ctx, user: Mock.Of<ICurrentUserService>()).Index(null))
            .Should().BeOfType<RedirectToActionResult>().Subject;
        (ohneUser.ControllerName, ohneUser.ActionName).Should().Be(("Account", "Login"));
    }

    // ---- v1.99.81 (Fix-Lauf, Entscheid 2A / 13A): Waechter an Pause, Finish, ReportPartial, FinishGroup, CloseOthers ----

    private static System.Text.Json.JsonElement Antwort(IActionResult r) =>
        System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(System.Text.Json.JsonSerializer.Serialize(
            r is JsonResult j ? j.Value : ((ObjectResult)r).Value));

    private static Task<IActionResult> Aktion(BdeTerminalController c, string aktion, int bookingId, int woId, int operatorId, int terminalId) =>
        aktion switch
        {
            "Pause" => c.Pause(bookingId, operatorId, terminalId, 1, 0),
            "Finish" => c.Finish(bookingId, operatorId, terminalId, 1, 0),
            "ReportPartial" => c.ReportPartial(bookingId, operatorId, terminalId, 1, 0),
            "FinishGroup" => c.FinishGroup(operatorId, terminalId, $"[{{\"bookingId\":{bookingId},\"goodQty\":1,\"scrapQty\":0,\"isFinal\":true}}]"),
            "CloseOthers" => c.CloseOthers(woId, operatorId, terminalId),
            _ => throw new ArgumentOutOfRangeException(nameof(aktion))
        };

    public static TheoryData<string> Aktionen => new() { "Pause", "Finish", "ReportPartial", "FinishGroup", "CloseOthers" };

    /// <summary>Eigene laufende Buchung (Werker 1) + laufende Buchung eines zweiten Werkers auf demselben AG.</summary>
    private static async Task<(BdeBookingTestSeed.Ids ids, BdeBooking eigene, BdeBooking fremde)> SeedZweiWerkerAsync(ApplicationDbContext ctx)
    {
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        var eigene = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        var fremde = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-30));
        fremde.BdeOperatorId = op2;
        ctx.BdeBookings.AddRange(eigene, fremde);
        await ctx.SaveChangesAsync();
        return (ids, eigene, fremde);
    }

    private static void NichtsGeaendert(ApplicationDbContext ctx, params BdeBooking[] buchungen)
    {
        foreach (var b in buchungen)
        {
            var frisch = ctx.BdeBookings.AsNoTracking().Single(x => x.Id == b.Id);
            frisch.Status.Should().Be(BdeBookingStatus.Running);
            frisch.EndedAt.Should().BeNull();
        }
        ctx.BdeBookingQuantities.Count().Should().Be(0);
    }

    [Theory, MemberData(nameof(Aktionen))]
    public async Task Waechter_FremdesTerminal_Abgelehnt_NichtsGeaendert(string aktion)
    {
        var ctx = TestDbContextFactory.Create();
        var (ids, eigene, fremde) = await SeedZweiWerkerAsync(ctx);

        var json = Antwort(await Aktion(CreateTerminalController(ctx, multiMa: true, multiOp: true), aktion, eigene.Id, ids.WorkOperationId,
            ids.OperatorId, ids.TerminalId + 999));

        json.GetProperty("message").GetString().Should().Contain("nicht dem angemeldeten Benutzer");
        NichtsGeaendert(ctx, eigene, fremde);
    }

    [Theory]
    [InlineData("Pause")]
    [InlineData("Finish")]
    [InlineData("ReportPartial")]
    [InlineData("FinishGroup")]
    public async Task Waechter_FremdeBuchung_Abgelehnt_NichtsGeaendert(string aktion)
    {
        var ctx = TestDbContextFactory.Create();
        var (ids, eigene, fremde) = await SeedZweiWerkerAsync(ctx);

        var json = Antwort(await Aktion(CreateTerminalController(ctx, multiMa: true, multiOp: true), aktion, fremde.Id, ids.WorkOperationId,
            ids.OperatorId, ids.TerminalId));

        json.GetProperty("message").GetString().Should().Contain("gehört nicht");
        NichtsGeaendert(ctx, eigene, fremde);
    }

    // v1.99.81 (Entscheid 9, Review Focus 4): fremde Pause abschliessen → abgewiesen, nichts geaendert; eigene → beendet
    [Fact]
    public async Task ClosePaused_Fremd_Abgelehnt_Eigen_Beendet()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        var eigene = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused, DateTime.Now.AddHours(-2), DateTime.Now.AddHours(-1));
        var fremde = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused, DateTime.Now.AddHours(-2), DateTime.Now.AddHours(-1));
        fremde.BdeOperatorId = op2;
        ctx.BdeBookings.AddRange(eigene, fremde);
        await ctx.SaveChangesAsync();
        var c = CreateTerminalController(ctx);

        Antwort(await c.ClosePaused(fremde.Id, ids.OperatorId, ids.TerminalId)).GetProperty("message").GetString().Should().Contain("gehört nicht");
        Antwort(await c.ClosePaused(eigene.Id, ids.OperatorId, ids.TerminalId + 999)).GetProperty("message").GetString().Should().Contain("nicht dem angemeldeten Benutzer");
        ctx.BdeBookings.AsNoTracking().Single(x => x.Id == fremde.Id).Status.Should().Be(BdeBookingStatus.Paused);
        ctx.BdeBookings.AsNoTracking().Single(x => x.Id == eigene.Id).Status.Should().Be(BdeBookingStatus.Paused);

        await c.ClosePaused(eigene.Id, ids.OperatorId, ids.TerminalId);
        ctx.BdeBookings.AsNoTracking().Single(x => x.Id == eigene.Id).Status.Should().Be(BdeBookingStatus.Finished);
    }

    [Fact]
    public async Task CloseOthers_OhneEigeneBeendeteBuchung_Abgelehnt_NichtsGeaendert()
    {
        var ctx = TestDbContextFactory.Create();
        var (ids, eigene, fremde) = await SeedZweiWerkerAsync(ctx);

        var json = Antwort(await CreateTerminalController(ctx, multiMa: true).CloseOthers(ids.WorkOperationId, ids.OperatorId, ids.TerminalId));

        json.GetProperty("message").GetString().Should().Contain("selbst fertig");
        NichtsGeaendert(ctx, eigene, fremde);
    }

    [Theory, MemberData(nameof(Aktionen))]
    public async Task Waechter_InaktiverWerker_Abgelehnt_NichtsGeaendert(string aktion)
    {
        var ctx = TestDbContextFactory.Create();
        var (ids, eigene, fremde) = await SeedZweiWerkerAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished, DateTime.Now.AddHours(-3), DateTime.Now.AddHours(-2)));
        (await ctx.BdeOperators.FindAsync(ids.OperatorId))!.IsActive = false;
        await ctx.SaveChangesAsync();

        var json = Antwort(await Aktion(CreateTerminalController(ctx, multiMa: true, multiOp: true), aktion, eigene.Id, ids.WorkOperationId,
            ids.OperatorId, ids.TerminalId));

        json.GetProperty("message").GetString().Should().Contain("nicht (mehr) aktiv");
        NichtsGeaendert(ctx, eigene, fremde);
    }

    // Review Focus 1: Besitz zaehlt, nicht das Start-Terminal
    [Fact]
    public async Task Finish_AnderesStartTerminal_GleicherWerker_Erlaubt()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var zweites = new BdeTerminal { UserId = ids.UserId, DefaultProductionWorkplaceId = ids.WorkplaceId, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.BdeTerminals.Add(zweites);
        await ctx.SaveChangesAsync();
        var b = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        b.BdeTerminalId = zweites.Id;   // an Terminal B gestartet
        ctx.BdeBookings.Add(b);
        await ctx.SaveChangesAsync();

        // angemeldet ist Terminal A (das erste geseedete)
        var json = Antwort(await CreateTerminalController(ctx).Finish(b.Id, ids.OperatorId, ids.TerminalId, 10, 0));

        json.GetProperty("outcome").GetString().Should().Be("Success");
        ctx.BdeBookings.AsNoTracking().Single(x => x.Id == b.Id).Status.Should().Be(BdeBookingStatus.Finished);
    }

    // H-B: Exception -> fester deutscher Text, kein Exception-Typ und keine ex.Message beim Werker
    [Fact]
    public async Task Finish_Exception_FesterText_OhneInterna()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var b = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(b);
        await ctx.SaveChangesAsync();
        var svc = new Mock<IBdeBookingService>();
        svc.Setup(s => s.FinishAsync(It.IsAny<int>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()))
            .ThrowsAsync(new InvalidOperationException("geheimes Detail"));
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(1);
        var terminals = new Mock<IBdeTerminalRepository>();
        terminals.Setup(t => t.GetByUserIdAsync(1)).ReturnsAsync(() => ctx.BdeTerminals.FirstOrDefault());
        var c = new BdeTerminalController(terminals.Object, svc.Object, user.Object, Mock.Of<IBdeDefaultWorkOperationService>(),
            Mock.Of<IAppSettingRepository>(), ctx, NullLogger<BdeTerminalController>.Instance);

        var text = Antwort(await c.Finish(b.Id, ids.OperatorId, ids.TerminalId, 1, 0)).GetProperty("message").GetString();

        text.Should().StartWith("Serverfehler").And.NotContain("InvalidOperationException").And.NotContain("geheimes Detail");
    }

    // v1.99.81 (3A, Review Focus 3): Gruppen-Antwort mit Sub-FA + Werkbank je Zeile, nur Buchungen der Werkbank der angeklickten Buchung
    [Fact]
    public async Task Finish_Gruppe_SubFaUndWerkbank_NurEigeneWerkbank()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);
        ctx.ProductionOrders.Single(p => p.Id == ids.ProductionOrderId).SubOrderNumber = "SUB-42";
        var andereWb = new ProductionWorkplace { Name = "Andere Werkbank", BdeAktiv = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(andereWb);
        await ctx.SaveChangesAsync();
        var wo3 = new WorkOperation { ProductionOrderId = ids.ProductionOrderId, OperationNumber = "30", Name = "Dritter", ProductionWorkplaceId = andereWb.Id,
            Sequence = 30, IsReportable = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.WorkOperations.Add(wo3);
        await ctx.SaveChangesAsync();
        var b1 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        var b2 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-30), workOperationId: wo2);
        var b3 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-20), workOperationId: wo3.Id);
        b3.ProductionWorkplaceId = andereWb.Id;
        ctx.BdeBookings.AddRange(b1, b2, b3);
        await ctx.SaveChangesAsync();

        var json = Antwort(await CreateTerminalController(ctx, multiOp: true, groupFinish: true).Finish(b1.Id, ids.OperatorId, ids.TerminalId, 5m, 0m));

        json.GetProperty("outcome").GetString().Should().Be("GroupFinishRequired");
        var zeilen = json.GetProperty("bookings").EnumerateArray().ToList();
        zeilen.Select(z => z.GetProperty("bookingId").GetInt32()).Should().BeEquivalentTo(new[] { b1.Id, b2.Id });
        zeilen.Should().OnlyContain(z => z.GetProperty("subOrderNumber").GetString() == "SUB-42");
        zeilen.Should().OnlyContain(z => z.GetProperty("workplaceName").GetString() == "Werkbank");
    }

    // Nur eine Buchung an dieser Werkbank -> kein Gruppen-Dialog, normales Fertig
    [Fact]
    public async Task Finish_Gruppe_ZweiteBuchungAndereWerkbank_EinzelFertig()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var andereWb = new ProductionWorkplace { Name = "Andere Werkbank", BdeAktiv = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(andereWb);
        await ctx.SaveChangesAsync();
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);
        var b1 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        var b2 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-30), workOperationId: wo2);
        b2.ProductionWorkplaceId = andereWb.Id;
        ctx.BdeBookings.AddRange(b1, b2);
        await ctx.SaveChangesAsync();

        var json = Antwort(await CreateTerminalController(ctx, multiOp: true, groupFinish: true).Finish(b1.Id, ids.OperatorId, ids.TerminalId, 10m, 0m));

        json.GetProperty("outcome").GetString().Should().Be("Success");
        ctx.BdeBookings.AsNoTracking().Single(b => b.Id == b2.Id).Status.Should().Be(BdeBookingStatus.Running);
    }
}
