using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>
/// Controller-Tests fuer die Mehrfach-Einbuchung (Teil-2-Spec). Fokus: keine Teilbuchung bei
/// ungueltigen Zeilen (S2/AK9), Buchung ausschliesslich ueber AddAsync (S1/AK8), Erfolgsmeldung
/// mit Anzahl (AK4), gemeinsamer Kopf (AK2/AK3).
/// </summary>
public class StockMovementsControllerBulkInboundTests
{
    private readonly Mock<IStockMovementRepository> _stockRepo = new();
    private readonly Mock<IArticleRepository> _articleRepo = new();
    private readonly Mock<IStorageLocationRepository> _locationRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAppSettingRepository> _settings = new();
    private readonly Mock<IPartRequisitionRepository> _partReq = new();

    private StockMovementsController BuildController()
    {
        _locationRepo.Setup(r => r.GetActiveOrderedExcludingPickingTransportAsync())
            .ReturnsAsync(new List<StorageLocation>());
        _settings.Setup(r => r.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        _currentUser.Setup(c => c.GetCurrentAppUserId()).Returns(7);
        _currentUser.Setup(c => c.GetWindowsUserName()).Returns("DOMAIN\\tester");
        _currentUser.Setup(c => c.GetDisplayName()).Returns("Tester");
        _articleRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new Article { Id = id, ArticleNumber = $"A-{id}", CreatedBy = "t", CreatedByWindows = "t" });

        var controller = new StockMovementsController(
            _stockRepo.Object, _articleRepo.Object, _locationRepo.Object, _userRepo.Object,
            _currentUser.Object, _settings.Object, _partReq.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    private static StockMovementBulkInboundViewModel Vm(int storageLocationId, string? fa, params (int articleId, decimal qty)[] lines)
    {
        return new StockMovementBulkInboundViewModel
        {
            StorageLocationId = storageLocationId,
            ProductionOrder = fa,
            Lines = lines.Select(l => new StockMovementBulkInboundLine { ArticleId = l.articleId, Quantity = l.qty }).ToList()
        };
    }

    [Fact] // AK2/AK3/AK4: mehrere gueltige Zeilen -> je Zeile eine Einbuchung, gemeinsamer Kopf
    public async Task InboundBulk_Post_ValidLines_BooksOnePerLineViaAddAsync()
    {
        var controller = BuildController();
        var captured = new List<StockMovement>();
        _stockRepo.Setup(r => r.AddAsync(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(captured.Add)
            .ReturnsAsync((StockMovement m) => m);

        var vm = Vm(storageLocationId: 5, fa: "1234567", (10, 2m), (11, 3m));
        var result = await controller.InboundBulk(vm);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(StockMovementsController.InboundBulk));
        _stockRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Exactly(2));
        captured.Should().HaveCount(2);
        captured.Should().OnlyContain(m => m.MovementType == MovementType.Einbuchung
                                        && m.StorageLocationId == 5
                                        && m.ProductionOrder == "1234567");
        captured.Should().Contain(m => m.ArticleId == 10 && m.Quantity == 2m);
        captured.Should().Contain(m => m.ArticleId == 11 && m.Quantity == 3m);
        // Gemeinsamer Timestamp
        captured.Select(m => m.Timestamp).Distinct().Should().ContainSingle();
        controller.TempData["SuccessMessage"].Should().Be("2 Artikel erfolgreich eingebucht.");
    }

    [Fact] // AK9: eine ungueltige Zeile (Artikel fehlt) -> NICHTS gebucht, Zeile markiert
    public async Task InboundBulk_Post_MissingArticle_BooksNothingAndMarksLine()
    {
        var controller = BuildController();

        var vm = Vm(storageLocationId: 5, fa: "1234567", (10, 2m), (0, 1m), (11, 3m));
        var result = await controller.InboundBulk(vm);

        _stockRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Never);
        var view = result.Should().BeOfType<ViewResult>().Which;
        var returnedVm = view.Model.Should().BeOfType<StockMovementBulkInboundViewModel>().Which;
        returnedVm.Lines.Should().HaveCount(3); // alle Eingaben erhalten (S2/AK10)
        returnedVm.Lines[1].Error.Should().NotBeNull();
        returnedVm.Lines[0].Error.Should().BeNull();
    }

    [Fact] // AK9: Menge 0 -> ungueltig, nichts gebucht
    public async Task InboundBulk_Post_ZeroQuantity_BooksNothing()
    {
        var controller = BuildController();

        var vm = Vm(storageLocationId: 5, fa: null, (10, 1m), (11, 0m));
        var result = await controller.InboundBulk(vm);

        _stockRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Never);
        result.Should().BeOfType<ViewResult>();
    }

    [Fact] // AK5: leere Zeilenliste -> abgelehnt
    public async Task InboundBulk_Post_NoLines_IsRejected()
    {
        var controller = BuildController();

        var vm = Vm(storageLocationId: 5, fa: null);
        var result = await controller.InboundBulk(vm);

        _stockRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Never);
        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }
}
