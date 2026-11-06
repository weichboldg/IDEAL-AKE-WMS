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

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E6b/G14: Sage-Zuordnung in der BDE-Personalpflege
/// (Vorschlag nur bei exakter Personalnummer, nie ueber den Namen; Duplikat-Guard; Audit aus ICurrentUserService).</summary>
public class BdeMasterDataSageZuordnungTests
{
    private static BdeOperator Op(int id, string pnr, string nachname, string vorname, string? sageNr = null) => new()
    {
        Id = id, PersonnelNumber = pnr, LastName = nachname, FirstName = vorname, SageMitarbeiternummer = sageNr,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
    };

    private static SagePpsMitarbeiter Sage(string nummer, string nachname = "Huber", string vorname = "Max") =>
        new() { Nummer = nummer, Nachname = nachname, Vorname = vorname, SyncedAt = DateTime.Now };

    private static (BdeMasterDataController Ctrl, ApplicationDbContext Ctx) Controller(string query = "")
    {
        var ctx = TestDbContextFactory.Create();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Schichtleiter Test");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\sl");
        user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var actRepo = new Mock<IBdeActivityRepository>();
        actRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BdeActivity>());
        var termRepo = new Mock<IBdeTerminalRepository>();
        termRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BdeTerminal>());
        var ctrl = new BdeMasterDataController(new BdeOperatorRepository(ctx), actRepo.Object, termRepo.Object,
            new Mock<IUserRepository>().Object, new Mock<IProductionWorkplaceRepository>().Object, user.Object);
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString(query);
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return (ctrl, ctx);
    }

    [Fact]
    public void Vorschlag_NurBeiExakterPersonalnummer_NieUeberNamen_NurFreieOperatoren()
    {
        var ops = new List<BdeOperator>
        {
            Op(1, "4711", "Anders", "Heisst"),          // exakte Nummer -> Vorschlag, obwohl Name anders
            Op(2, "4712x", "Huber", "Max"),             // gleicher Name, andere Nummer -> kein Vorschlag
            Op(3, "4713", "Zu", "Geordnet", sageNr: "9000"), // gleiche Nummer wie Sage 4713? nein: schon zugeordnet -> kein Vorschlag
        };
        var zeilen = BdeMasterDataController.SageZuordnungZeilen(
            new[] { Sage("9000"), Sage("4711"), Sage("4712", "Huber", "Max"), Sage("4713"), Sage("04711") }, ops);

        zeilen.Select(z => z.Sage.Nummer).Should().Equal("04711", "4711", "4712", "4713", "9000"); // nicht zugeordnete zuerst
        zeilen.Single(z => z.Sage.Nummer == "4711").Vorschlag!.Id.Should().Be(1);
        zeilen.Single(z => z.Sage.Nummer == "04711").Vorschlag.Should().BeNull("exakt, keine Normalisierung fuehrender Nullen");
        zeilen.Single(z => z.Sage.Nummer == "4712").Vorschlag.Should().BeNull("nie ueber den Namen");
        zeilen.Single(z => z.Sage.Nummer == "4713").Vorschlag.Should().BeNull("Operator 3 ist schon vergeben");
        zeilen.Single(z => z.Sage.Nummer == "9000").Zugeordnet!.Id.Should().Be(3);
    }

    [Fact]
    public async Task Index_SageTab_Liste_Paginiert_Spaltenfilter_Warnzahl()
    {
        var (ctrl, ctx) = Controller("?colf_status=Nicht%20zugeordnet");
        ctx.BdeOperators.AddRange(Op(1, "4711", "Huber", "Max"), Op(2, "500", "Maier", "Anna", sageNr: "4712"));
        ctx.SagePpsMitarbeiter.AddRange(Sage("4711"), Sage("4712"));
        await ctx.SaveChangesAsync();

        await ctrl.Index(tab: "sage");

        var zeilen = (List<BdeSageZuordnungZeile>)ctrl.ViewBag.SageZeilen;
        zeilen.Should().ContainSingle().Which.Sage.Nummer.Should().Be("4711");
        ((PaginationState)ctrl.ViewBag.Pagination).TotalCount.Should().Be(1);
        ((int)ctrl.ViewBag.BdeOhneSageNummer).Should().Be(1);
        ((List<BdeOperator>)ctrl.ViewBag.FreieOperatoren).Select(o => o.Id).Should().Equal(1);
    }

    [Fact]
    public async Task Zuordnen_SetztNummer_MitAudit()
    {
        var (ctrl, ctx) = Controller();
        ctx.BdeOperators.Add(Op(1, "4711", "Huber", "Max"));
        ctx.SagePpsMitarbeiter.Add(Sage("4711"));
        await ctx.SaveChangesAsync();

        var r = await ctrl.SageZuordnen(" 4711 ", 1);

        r.Should().BeOfType<RedirectToActionResult>();
        var op = ctx.BdeOperators.Single();
        (op.SageMitarbeiternummer, op.ModifiedBy, op.ModifiedByWindows).Should().Be(("4711", "Schichtleiter Test", "AKE\\sl"));
        op.ModifiedAt.Should().NotBeNull();
        ctrl.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Zuordnen_DoppelteNummer_Abgewiesen()
    {
        var (ctrl, ctx) = Controller();
        ctx.BdeOperators.AddRange(Op(1, "4711", "Huber", "Max", sageNr: "4711"), Op(2, "200", "Maier", "Anna"));
        ctx.SagePpsMitarbeiter.Add(Sage("4711"));
        await ctx.SaveChangesAsync();

        await ctrl.SageZuordnen("4711", 2);

        ctx.BdeOperators.Single(o => o.Id == 2).SageMitarbeiternummer.Should().BeNull("App-Guard (InMemory erzwingt UNIQUE nicht)");
        ((string)ctrl.TempData["WarningMessage"]!).Should().Contain("bereits 'Max Huber' zugeordnet");
    }

    [Fact]
    public async Task Zuordnen_UnbekannteNummer_Oder_SchonZugeordneterOperator_Abgewiesen()
    {
        var (ctrl, ctx) = Controller();
        ctx.BdeOperators.Add(Op(1, "100", "Huber", "Max", sageNr: "4711"));
        ctx.SagePpsMitarbeiter.AddRange(Sage("4711"), Sage("4712"));
        await ctx.SaveChangesAsync();

        await ctrl.SageZuordnen("9999", 1);
        ((string)ctrl.TempData["WarningMessage"]!).Should().Contain("nicht (mehr) im Sage-Bestand");

        await ctrl.SageZuordnen("4712", 1);
        ((string)ctrl.TempData["WarningMessage"]!).Should().Contain("zuerst die Zuordnung lösen");
        ctx.BdeOperators.Single().SageMitarbeiternummer.Should().Be("4711");
    }

    [Fact]
    public async Task Loesen_LeertNummer_MitAudit()
    {
        var (ctrl, ctx) = Controller();
        ctx.BdeOperators.Add(Op(1, "100", "Huber", "Max", sageNr: "4711"));
        await ctx.SaveChangesAsync();

        await ctrl.SageZuordnungLoesen(1);

        var op = ctx.BdeOperators.Single();
        (op.SageMitarbeiternummer, op.ModifiedBy).Should().Be(((string?)null, "Schichtleiter Test"));
        (await ctrl.SageZuordnungLoesen(99)).Should().BeOfType<NotFoundResult>();
    }
}
