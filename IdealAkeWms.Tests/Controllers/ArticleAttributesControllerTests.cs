using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using IdealAkeWms.Data;
using IdealAkeWms.Tests.Helpers;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class ArticleAttributesControllerTests
{
    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var definitions = new List<ArticleAttributeDefinition>
        {
            new() { Id = 1, Name = "Laserschnitt", AttributeType = AttributeType.Boolean, SortOrder = 1, IsActive = true, CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, Name = "Oberflaeche", AttributeType = AttributeType.Dropdown, SortOrder = 2, IsActive = true, CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 3, Name = "Kantung", AttributeType = AttributeType.Boolean, SortOrder = 3, IsActive = false, CreatedBy = "t", CreatedByWindows = "t" }
        };
        var repo = new Mock<IArticleAttributeRepository>();
        repo.Setup(r => r.GetAllDefinitionsAsync()).ReturnsAsync(definitions);
        repo.Setup(r => r.DefinitionHasValuesAsync(It.IsAny<int>())).ReturnsAsync(false);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var ctrl = new ArticleAttributesController(repo.Object, currentUser.Object);

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_name=Oberflaeche");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index() as ViewResult;

        var model = result!.Model as List<ArticleAttributeDefinition>;
        model!.Should().HaveCount(1);
        model[0].Name.Should().Be("Oberflaeche");
        var pagination = ctrl.ViewBag.Pagination as PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }

    // ----- E3b: Sperre fuer Sage-gefuehrte Merkmale (Spec 2026-11-06-sage-sdata-stammdaten-sync) -----

    private static ArticleAttributeDefinition SageDef() => new()
    {
        Id = 7, Name = "Maschine", AttributeType = AttributeType.Text, SortOrder = 4, IsActive = true,
        SyncSource = ArticleAttributeDefinition.SyncSourceSage, SyncFieldName = "USER_Maschine", CreatedBy = "t", CreatedByWindows = "t",
    };

    private static (ArticleAttributesController Ctrl, Mock<IArticleAttributeRepository> Repo) Controller(ArticleAttributeDefinition def)
    {
        var repo = new Mock<IArticleAttributeRepository>();
        repo.Setup(r => r.GetDefinitionByIdAsync(def.Id)).ReturnsAsync(def);
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Tester");
        user.Setup(u => u.GetWindowsUserName()).Returns(@"AKE	ester");
        var ctrl = new ArticleAttributesController(repo.Object, user.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
        };
        return (ctrl, repo);
    }

    [Fact]
    public async Task SageMerkmal_NameAendern_Gesperrt_ReihenfolgeUndAktivErlaubt()
    {
        var def = SageDef();
        var (ctrl, repo) = Controller(def);

        await ctrl.UpdateDefinition(def.Id, "Anders", 9, false);
        def.Name.Should().Be("Maschine");
        repo.Verify(r => r.UpdateDefinitionAsync(It.IsAny<ArticleAttributeDefinition>()), Times.Never);
        ((string)ctrl.TempData["WarningMessage"]!).Should().Contain("aus Sage");

        await ctrl.UpdateDefinition(def.Id, "Maschine", 9, false);
        (def.SortOrder, def.IsActive, def.ModifiedBy).Should().Be((9, false, "Tester"));
        repo.Verify(r => r.UpdateDefinitionAsync(def), Times.Once);
    }

    [Fact]
    public async Task SageMerkmal_LoeschenUndOptionen_Gesperrt()
    {
        var def = SageDef();
        var (ctrl, repo) = Controller(def);

        await ctrl.DeleteDefinition(def.Id);
        await ctrl.AddOption(def.Id, "X", 0);

        repo.Verify(r => r.DeleteDefinitionAsync(It.IsAny<int>()), Times.Never);
        repo.Verify(r => r.AddOptionAsync(It.IsAny<ArticleAttributeOption>()), Times.Never);
        ((string)ctrl.TempData["WarningMessage"]!).Should().Contain("aus Sage");
    }

    [Fact]
    public async Task CreateDefinition_TypText_Abgelehnt()
    {
        var (ctrl, repo) = Controller(SageDef());
        await ctrl.CreateDefinition("Frei", AttributeType.Text);
        repo.Verify(r => r.AddDefinitionAsync(It.IsAny<ArticleAttributeDefinition>()), Times.Never);
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
    }

    [Fact]
    public void ColumnMap_TypText_WieBadge() =>
        ArticleAttributesController.TypLabel(SageDef()).Should().Be("Text");

    [Fact]
    public async Task SaveValues_SageMerkmal_WirdNieGeaendertOderEntfernt()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.Articles.Add(new Article { Id = 1, ArticleNumber = "A-1", CreatedBy = "x", CreatedByWindows = "x" });
        ctx.ArticleAttributeDefinitions.AddRange(SageDef(),
            new ArticleAttributeDefinition { Id = 8, Name = "Laser", AttributeType = AttributeType.Boolean, CreatedBy = "x", CreatedByWindows = "x" });
        ctx.ArticleAttributeValues.Add(new ArticleAttributeValue { ArticleId = 1, ArticleAttributeDefinitionId = 7, TextValue = "M-200", CreatedBy = "s", CreatedByWindows = "s" });
        ctx.SaveChanges();

        // Bearbeiten-Formular schickt fuer das Sage-Merkmal weder Boolean noch Option ("geleert") – darf den Text nicht loeschen.
        await new ArticleAttributeRepository(ctx).SaveValuesAsync(1, new List<ArticleAttributeValue>
        {
            new() { ArticleAttributeDefinitionId = 7 },
            new() { ArticleAttributeDefinitionId = 8, BooleanValue = true },
        }, "u", "w");

        ctx.ArticleAttributeValues.Single(v => v.ArticleAttributeDefinitionId == 7).TextValue.Should().Be("M-200");
        ctx.ArticleAttributeValues.Single(v => v.ArticleAttributeDefinitionId == 8).BooleanValue.Should().BeTrue();
    }
}
