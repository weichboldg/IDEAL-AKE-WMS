using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services.HierarchischeStruktur;
using Moq;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// IDEAL Minimal-Fix: der BOM-Guard hängt am Master-Schalter, nicht am Fehler.
/// </summary>
public class HierarchicalBomGuardRepositoryTests
{
    private readonly Mock<IBomRepository> _inner = new();
    private readonly Mock<IServiceSettingRepository> _settings = new();

    private HierarchicalBomGuardRepository Sut() => new(_inner.Object, _settings.Object);

    private void Master(string? value) =>
        _settings.Setup(s => s.GetValueAsync(HierarchischeStrukturKeys.Master)).ReturnsAsync(value);

    [Fact]
    public async Task Hierarchical_ReturnsMarker_WithoutTouchingInner()
    {
        Master("true");

        var result = await Sut().GetBomItemsAsync("ART-1");

        result.DataSource.Should().Be(BomDataSources.HierarchicalUnavailable);
        result.Items.Should().BeEmpty();
        _inner.Verify(r => r.GetBomItemsAsync(It.IsAny<string>()), Times.Never); // kein Cache/DB-Zugriff
    }

    [Theory]
    [InlineData("TRUE")]
    [InlineData("True")]
    public async Task Hierarchical_IsCaseInsensitive(string masterValue)
    {
        Master(masterValue);

        var result = await Sut().GetBomItemsAsync("ART-1");

        result.DataSource.Should().Be(BomDataSources.HierarchicalUnavailable);
        _inner.Verify(r => r.GetBomItemsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FlatMode_False_DelegatesToInner_Unchanged()
    {
        Master("false");
        var inner = new BomQueryResult(new List<BomItem> { new() { Artikelnummer = "X" } }, BomDataSources.Sage);
        _inner.Setup(r => r.GetBomItemsAsync("ART-1")).ReturnsAsync(inner);

        var result = await Sut().GetBomItemsAsync("ART-1");

        result.Should().BeSameAs(inner);
        _inner.Verify(r => r.GetBomItemsAsync("ART-1"), Times.Once);
    }

    [Fact]
    public async Task FlatMode_NullMaster_DelegatesToInner()
    {
        Master(null); // Master nie gesetzt (AKE) -> flacher Modus
        var inner = new BomQueryResult(new List<BomItem>(), BomDataSources.NoData);
        _inner.Setup(r => r.GetBomItemsAsync("ART-1")).ReturnsAsync(inner);

        var result = await Sut().GetBomItemsAsync("ART-1");

        result.Should().BeSameAs(inner);
        _inner.Verify(r => r.GetBomItemsAsync("ART-1"), Times.Once);
    }
}
