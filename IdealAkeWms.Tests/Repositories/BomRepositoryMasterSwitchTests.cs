using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services.HierarchischeStruktur;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class BomRepositoryMasterSwitchTests
{
    private readonly Mock<IBomRepository> _hierBom = new();
    private readonly Mock<IBomCacheRepository> _hierCache = new();
    private readonly Mock<IBomRepository> _flatBom = new();
    private readonly Mock<IBomCacheRepository> _flatCache = new();

    private BomRepositoryMasterSwitch Sut(string? master)
    {
        var settings = new Mock<IServiceSettingRepository>();
        settings.Setup(s => s.GetValueAsync(HierarchischeStrukturKeys.Master)).ReturnsAsync(master);
        return new BomRepositoryMasterSwitch(settings.Object,
            () => _hierBom.Object, () => _hierCache.Object, () => _flatBom.Object, () => _flatCache.Object);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    public async Task MasterTrue_RoutesToHierarchical_NeverTouchesFlat(string master)
    {
        var expected = new BomQueryResult(new List<BomItem>(), BomDataSources.FaHierarchie, true);
        _hierBom.Setup(r => r.GetBomItemsAsync(It.IsAny<BomKey>(), BomScope.FullStructure)).ReturnsAsync(expected);
        _hierCache.Setup(r => r.GetDeviceArticleNumbersByComponentAsync("C")).ReturnsAsync(new List<string> { "1" });

        var sut = Sut(master);
        (await sut.GetBomItemsAsync(new BomKey("A", "1", "1"), BomScope.FullStructure)).Should().BeSameAs(expected);
        (await sut.GetDeviceArticleNumbersByComponentAsync("C")).Should().ContainSingle();
        _flatBom.VerifyNoOtherCalls();
        _flatCache.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public async Task MasterFalseOrMissing_RoutesToFlat_NeverTouchesHierarchical(string? master)
    {
        var expected = new BomQueryResult(new List<BomItem>(), BomDataSources.Sage);
        _flatBom.Setup(r => r.GetBomItemsAsync(It.IsAny<BomKey>(), It.IsAny<BomScope>())).ReturnsAsync(expected);
        _flatCache.Setup(r => r.GetByArticleNumberAsync(It.IsAny<BomKey>())).ReturnsAsync(expected);

        var sut = Sut(master);
        (await sut.GetBomItemsAsync(new BomKey("A"))).Should().BeSameAs(expected);
        (await sut.GetByArticleNumberAsync(new BomKey("A"))).Should().BeSameAs(expected);
        _hierBom.VerifyNoOtherCalls();
        _hierCache.VerifyNoOtherCalls();
    }
}
