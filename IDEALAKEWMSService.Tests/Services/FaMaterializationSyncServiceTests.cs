using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services;
using Microsoft.EntityFrameworkCore;
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
            ctx, config, mail.Object, new UnknownWorkplaceState(),
            new ProductionOrderPickingStatusRepository(ctx),
            NullLogger<FaMaterializationSyncService>.Instance, new FakeSyncLogger());
    }

    private static FaHierarchyNode Node(int haupt, int sub, int? vater) => new()
    {
        HauptFA = haupt, SubFA = sub, VaterFA = vater,
        Sollmenge = 1m, Artnr = "ART", Bezeichnung1 = "Bez1", Bezeichnung2 = "Bez2",
        Matchcode = "MC-" + sub,
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

    /// <summary>
    /// UAT-Befund H0 / Spec F6: Die Materialisierung legt für neu angelegte Sub-FAs bisher NUR die
    /// <see cref="ProductionOrder"/>-Zeile an, nicht die beiden 1:1-Satelliten-Zeilen. Folge im
    /// Leitstand: Freigabe zur Kommissionierung scheitert mit "PickingStatus-Zeile fehlt.".
    /// </summary>
    [Fact]
    public async Task RunAsync_NewSubFa_CreatesPickingStatusAndBdeStatusRows()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(new FaHierarchyNode { HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1 });
        await ctx.SaveChangesAsync();

        var sut = Build(ctx);
        await sut.RunAsync(dryRun: false);

        var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "1001");
        (await ctx.ProductionOrderPickingStatuses.CountAsync(s => s.ProductionOrderId == order.Id)).Should().Be(1);
        (await ctx.ProductionOrderBdeStatuses.CountAsync(s => s.ProductionOrderId == order.Id)).Should().Be(1);
    }

    /// <summary>
    /// Der eigentliche gemeldete Fehler: ein bereits materialisierter Sub-FA OHNE Statuszeilen
    /// (Alt-Bestand vor diesem Fix) wird beim nächsten Lauf nachgezogen (Backfill, F6).
    /// </summary>
    [Fact]
    public async Task RunAsync_ExistingSubFaWithoutStatusRows_BackfillsThem()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(new FaHierarchyNode { HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1 });
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            OrderNumber = "1000", SubOrderNumber = "1001", Quantity = 1,
            CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test"
        }); // bewusst OHNE Statuszeilen
        await ctx.SaveChangesAsync();

        await Build(ctx).RunAsync(dryRun: false);

        var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "1001");
        (await ctx.ProductionOrderPickingStatuses.AnyAsync(s => s.ProductionOrderId == order.Id)).Should().BeTrue();
        (await ctx.ProductionOrderBdeStatuses.AnyAsync(s => s.ProductionOrderId == order.Id)).Should().BeTrue();
    }

    /// <summary>DryRun darf keine Statuszeilen schreiben (Global Constraint DryRun).</summary>
    [Fact]
    public async Task RunAsync_DryRun_DoesNotCreateStatusRows()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(new FaHierarchyNode { HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1 });
        await ctx.SaveChangesAsync();

        await Build(ctx).RunAsync(dryRun: true);

        (await ctx.ProductionOrderPickingStatuses.CountAsync()).Should().Be(0);
        (await ctx.ProductionOrderBdeStatuses.CountAsync()).Should().Be(0);
    }

    // Matchcode-Abgleich Anlege-/Update-Pfad (ehem. F1-Regel-Tests): entfallen mit Task 3
    // (Matchcode-im-Artikelstamm) — ProductionOrder.Matchcode wurde als Spalte entfernt, die
    // Materialisierung schreibt sie nicht mehr. Der Matchcode lebt jetzt in Article; der
    // FaHierarchyNode.Matchcode bleibt (siehe Node()-Helper oben) fuer die reine
    // Struktur-/Lack-Ableitung erhalten, wird aber nicht mehr auf ProductionOrder projiziert.
}
