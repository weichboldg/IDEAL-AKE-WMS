using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>
/// Tests der Kombigeraet-Erkennung im FA-Struktur-Sync (Plan 2026-09-09, Task 5).
/// <see cref="FaHierarchySyncService.RunAsync"/> selbst liest ueber eine echte
/// <c>SqlConnection</c> aus Sage (siehe <c>SageImportServiceTests</c> — dort wird das
/// gleiche Muster dokumentiert) und ist deshalb ohne Live-DB nicht End-to-End InMemory
/// testbar. Die Meldelogik ist als eigene <c>internal</c> Methode
/// (<see cref="FaHierarchySyncService.LogAmbiguousHauptFasAsync"/>) herausgezogen und wird
/// hier direkt gegen einen <see cref="FakeSyncRun"/> geprueft (InternalsVisibleTo).
/// </summary>
public class FaHierarchySyncServiceTests
{
    private static FaHierarchyOrderInfo Info(int hauptFa, string montageAbteilung)
        => new() { HauptFA = hauptFa, MontageAbteilung = montageAbteilung, SyncedAt = DateTime.Now };

    [Fact]
    public async Task LogAmbiguousHauptFasAsync_HauptFaWithTwoOrderInfoRows_LogsAmbiguityOnce()
    {
        var run = new FakeSyncRun("FaHierarchy");
        var infos = new List<FaHierarchyOrderInfo>
        {
            Info(1000, "Montage A"),
            Info(1000, "Montage B"),
        };

        var count = await FaHierarchySyncService.LogAmbiguousHauptFasAsync(run, infos, CancellationToken.None);

        count.Should().Be(1);
        run.Events.Should().ContainSingle(e => e.Level == "Warning");
        run.Events.Single(e => e.Level == "Warning").Message.Should().Contain("1000");
    }

    [Fact]
    public async Task LogAmbiguousHauptFasAsync_AllHauptFasUnique_NoWarningAndZeroCount()
    {
        var run = new FakeSyncRun("FaHierarchy");
        var infos = new List<FaHierarchyOrderInfo>
        {
            Info(1000, "Montage A"),
            Info(1001, "Montage A"),
        };

        var count = await FaHierarchySyncService.LogAmbiguousHauptFasAsync(run, infos, CancellationToken.None);

        count.Should().Be(0);
        run.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task LogAmbiguousHauptFasAsync_MultipleAmbiguousHauptFas_LogsOneWarningWithAllKeys()
    {
        var run = new FakeSyncRun("FaHierarchy");
        var infos = new List<FaHierarchyOrderInfo>
        {
            Info(1000, "Montage A"),
            Info(1000, "Montage B"),
            Info(2000, "Montage A"),
            Info(2000, "Montage B"),
            Info(3000, "Montage A"),
        };

        var count = await FaHierarchySyncService.LogAmbiguousHauptFasAsync(run, infos, CancellationToken.None);

        count.Should().Be(2);
        run.Events.Should().ContainSingle(e => e.Level == "Warning");
        var message = run.Events.Single().Message;
        message.Should().Contain("1000");
        message.Should().Contain("2000");
        message.Should().NotContain("3000");
    }
}
