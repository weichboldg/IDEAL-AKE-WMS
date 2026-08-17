using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services.HierarchischeStruktur;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests;

public class GuardedServiceSettingRepositoryTests
{
    private static (GuardedServiceSettingRepository repo, Mock<IServiceSettingRepository> inner,
                    Mock<IProductionOrderRepository> orders) Build(string currentMaster, bool dataExists)
    {
        var inner = new Mock<IServiceSettingRepository>();
        inner.Setup(r => r.GetValueAsync(HierarchischeStrukturKeys.Master))
             .ReturnsAsync(currentMaster);
        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.HierarchicalDataExistsAsync()).ReturnsAsync(dataExists);
        return (new GuardedServiceSettingRepository(inner.Object, orders.Object), inner, orders);
    }

    [Fact]
    public async Task MasterDeaktivieren_MitDaten_WirftUndSchreibtNicht()
    {
        var (repo, inner, _) = Build(currentMaster: "true", dataExists: true);
        var act = () => repo.UpsertAsync(HierarchischeStrukturKeys.Master, "false");
        await act.Should().ThrowAsync<HierarchischeStrukturGuardException>();
        inner.Verify(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task MasterAktivieren_OhneDaten_SchreibtDurch()
    {
        var (repo, inner, _) = Build(currentMaster: "false", dataExists: false);
        await repo.UpsertAsync(HierarchischeStrukturKeys.Master, "true", "FA-Hierarchie", "d");
        inner.Verify(r => r.UpsertAsync(HierarchischeStrukturKeys.Master, "true", "FA-Hierarchie", "d"), Times.Once);
    }

    [Fact]
    public async Task UngeschuetzterKey_SchreibtImmerDurch()
    {
        var (repo, inner, _) = Build(currentMaster: "true", dataExists: true);
        await repo.UpsertAsync("Sync:BomCacheEnabled", "true");
        inner.Verify(r => r.UpsertAsync("Sync:BomCacheEnabled", "true", null, null), Times.Once);
    }

    [Fact]
    public async Task MasterLoeschen_MitDaten_Wirft()
    {
        var (repo, _, _) = Build(currentMaster: "true", dataExists: true);
        var act = () => repo.DeleteAsync(HierarchischeStrukturKeys.Master);
        await act.Should().ThrowAsync<HierarchischeStrukturGuardException>();
    }
}
