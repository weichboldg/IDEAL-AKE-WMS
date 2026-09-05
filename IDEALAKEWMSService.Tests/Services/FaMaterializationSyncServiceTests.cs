using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>
/// Z1-Regressionstest (Spec 2026-08-18-fa-liste-hierarchie-anzeige, Etappe D):
/// Der Materialisierungs-Sync darf WMS-eigene Zustände NIE überschreiben. Insbesondere bleibt eine
/// gesetzte BDE-Fertigmeldung (<see cref="ProductionOrderBdeStatus.IsDoneBde"/>) nach einem Sync-Lauf
/// unverändert — die Kaskade aus Etappe C wäre sonst nach spätestens 15 Minuten still zurückgesetzt.
/// Nagelt die (laut Code-Prüfung H1 bereits erfüllte) Invariante fest gegen einen künftigen Umbau
/// des Update-Pfads. IsDoneBde liegt auf der separaten Entität, die der Sync gar nicht lädt.
/// </summary>
public class FaMaterializationSyncServiceTests
{
    private static FaMaterializationSyncService Build(ApplicationDbContext ctx)
    {
        var config = new ConfigurationBuilder().Build(); // leer; SendMissingDigest no-op bei 0 vermissten
        var mail = new Mock<IMailService>();
        return new FaMaterializationSyncService(
            ctx, config, mail.Object,
            NullLogger<FaMaterializationSyncService>.Instance, new FakeSyncLogger());
    }

    private static FaHierarchyNode Node(int haupt, int sub, int? vater) => new()
    {
        HauptFA = haupt, SubFA = sub, VaterFA = vater,
        Sollmenge = 1m, Artnr = "ART", Bezeichnung1 = "Bez1", Bezeichnung2 = "Bez2",
        SyncedAt = DateTime.Now
    };

    [Fact]
    public async Task RunAsync_DoesNotResetIsDoneBde_OnExistingSubFa()
    {
        using var ctx = TestDbContextFactory.Create();

        // Quelle: HauptFA 100 mit Sub-FA 101 — beide im Source (nichts wird vermisst/angelegt).
        ctx.FaHierarchyNodes.AddRange(Node(100, 100, null), Node(100, 101, 100));

        // Bestehende, materialisierte Sub-FA 101, fertiggemeldet (IsDoneBde=true) + offener Komm-Status.
        var order = new ProductionOrder
        {
            OrderNumber = "100", SubOrderNumber = "101", ParentSubOrderNumber = "100",
            Quantity = 1m, IsDone = false,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        // Und die HauptFA-Zeile selbst (Sub 100), damit auch sie nur ein Update, kein Create ist.
        var haupt = new ProductionOrder
        {
            OrderNumber = "100", SubOrderNumber = "100", ParentSubOrderNumber = null,
            Quantity = 1m, IsDone = false,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionOrders.AddRange(order, haupt);
        ctx.SaveChanges();

        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus
        {
            ProductionOrderId = order.Id, IsDonePicking = false,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.ProductionOrderBdeStatuses.Add(new ProductionOrderBdeStatus
        {
            ProductionOrderId = order.Id, IsDoneBde = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.SaveChanges();

        var svc = Build(ctx);
        var result = await svc.RunAsync(dryRun: false);

        result.Skipped.Should().BeFalse();

        // Kern der Invariante: die Fertigmeldung überlebt den Sync unverändert.
        var bde = ctx.ProductionOrderBdeStatuses.Single(s => s.ProductionOrderId == order.Id);
        bde.IsDoneBde.Should().BeTrue();

        // Auch die übrigen App-Felder bleiben unberührt.
        var reloaded = ctx.ProductionOrders.Single(o => o.Id == order.Id);
        reloaded.IsDone.Should().BeFalse();
        var ps = ctx.ProductionOrderPickingStatuses.Single(s => s.ProductionOrderId == order.Id);
        ps.IsDonePicking.Should().BeFalse();
    }
}
