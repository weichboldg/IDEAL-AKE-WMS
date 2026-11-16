using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>Spec 2026-11-16-beschichtungslogik-je-standort AK 11 (Freigabe-Antwort 12): der Sage-Import-Hook ruft die Lackierteil-Erkennung
/// nur bei <c>Sync:CoatingDetectionEnabled</c>; der narrow BOM-Cache laeuft unveraendert.</summary>
public class SageImportHookGateTests
{
    private static (SageImportService svc, Mock<IBomCacheSyncService> bom, Mock<ICoatingDetectionService> coating) Build()
    {
        var config = new ConfigurationBuilder().Build();
        var bom = new Mock<IBomCacheSyncService>();
        var coating = new Mock<ICoatingDetectionService>();
        var svc = new SageImportService(config, NullLogger<SageImportService>.Instance, new FakeSyncLogger(), bom.Object, coating.Object,
            new Mock<ISyncErrorNotifier>().Object, new SageArtikelReader(config), TestDbContextFactory.Create());
        return (svc, bom, coating);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task Hook_LackierteilErkennung_NurBeiCoatingDetectionEnabled(bool coatingEnabled, int aufrufe)
    {
        var (svc, bom, coating) = Build();

        await svc.NeueAuftraegeHookAsync(new List<string> { "ART-1", "ART-1" }, new List<int> { 7 }, coatingEnabled, dryRun: false, CancellationToken.None);

        bom.Verify(b => b.SyncSpecificArticleNumbersAsync(It.Is<List<string>>(l => l.Count == 1), false, It.IsAny<CancellationToken>()), Times.Once);
        coating.Verify(c => c.DetectAndUpdateCoatingFlagsAsync(false, It.IsAny<List<int>?>(), It.IsAny<CancellationToken>()), Times.Exactly(aufrufe));
    }
}
