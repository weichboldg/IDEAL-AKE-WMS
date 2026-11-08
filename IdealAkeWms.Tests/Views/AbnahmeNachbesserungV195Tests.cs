using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>v1.95.0 (Vorab-Abnahme 2026-11-08, Bericht 2026-11-08-buendel-dev-v1-94-browser-testbericht, Befunde 1–4).</summary>
public class AbnahmeNachbesserungV195Tests
{
    private static string View(params string[] teile)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(new[] { dir!.FullName, "IdealAkeWms", "Views" }.Concat(teile).ToArray()));
    }

    // Befund 2: FA-Link findet den FA unabhaengig vom Status (FA-Liste blendet sonst erledigte/fertig kommissionierte FAs aus)
    [Fact]
    public void FaLink_View_FaListeMitShowDone() =>
        View("Shared", "_FaLink.cshtml").Should().Contain("new { filterOrderNumber = fa.OrderNumber, showDone = true }");

    [Fact]
    public async Task FaLink_Api_FaListeMitShowDone()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "2602098", SubOrderNumber = "2602098", ArticleNumber = "X", IsDone = true, CreatedBy = "t", CreatedByWindows = "t" });
        await db.SaveChangesAsync();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.CanPickAsync()).ReturnsAsync(true);
        UrlActionContext? aufruf = null;
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Callback((UrlActionContext a) => aufruf = a).Returns("/ProductionOrders");
        var api = new StockApiController(Mock.Of<IStockMovementRepository>(), new ProductionOrderRepository(db), user.Object,
            Mock.Of<IServiceSettingRepository>(), Mock.Of<IAppSettingRepository>()) { Url = url.Object };

        (await api.FaLookup("2602098")).Should().BeOfType<OkObjectResult>();
        aufruf!.Controller.Should().Be("ProductionOrders");
        aufruf.Values!.GetType().GetProperty("showDone")!.GetValue(aufruf.Values).Should().Be(true);
    }

    // Befund 3: Hilfe – Bestandsuebersicht uebernimmt nie die FA aus dem Artikel-QR
    [Fact]
    public void Hilfe_QrFa_Bestandsuebersicht_Nie()
    {
        var hilfe = View("Help", "Index.cshtml");
        hilfe.Should().NotContain("sowie in der Bestandsübersicht (nur mit");
        hilfe.Should().NotContain("bei eingeschalteter Einstellung <code>QrMitFaNummer</code> ebenfalls in das FA-Feld uebernommen");
        hilfe.Should().Contain("Bestandsübersicht seit v1.87.0 nie");
    }

    // Befund 4: leere Kommissionierer-Liste erklaeren, in allen drei Dialogen
    [Fact]
    public void Leitstand_KeinKommissionierer_HinweisMitLink()
    {
        var leitstand = View("PickingLeitstand", "Index.cshtml");
        System.Text.RegularExpressions.Regex.Matches(leitstand, "@if \\(activePickers.Count == 0\\) \\{ <partial name=\"_KeinKommissioniererHinweis\" /> \\}")
            .Count.Should().Be(3);
        var hinweis = View("PickingLeitstand", "_KeinKommissioniererHinweis.cshtml");
        hinweis.Should().Contain("Kein Benutzer als Kommissionierer gekennzeichnet").And.Contain("(Benutzer &rarr; Kommissionierer)")
            .And.Contain("asp-controller=\"Users\" asp-action=\"Index\"");
    }
}
