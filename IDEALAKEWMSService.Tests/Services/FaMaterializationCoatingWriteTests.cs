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
/// Task 4 (Spec K2 / F5): <c>HasCoatingParts</c> wird aus der FA-Struktur abgeleitet
/// (<see cref="FaMaterializationCoating.Derive"/>) und ueber
/// <see cref="IProductionOrderPickingStatusRepository.SetCoatingPartsAsync"/> geschrieben — NICHT
/// per rohem Feld-Setzen, weil diese Methode den Ruecksetzer fuer <c>IsCoatingDone</c> kapselt
/// (Fallstrick #11). Muss NACH dem Eager-Create der Statuszeilen (Task 1) laufen, sonst laeuft der
/// Schreibversuch ins Leere (SetCoatingPartsAsync aktualisiert nur bestehende Zeilen).
/// </summary>
public class FaMaterializationCoatingWriteTests
{
    private static FaMaterializationSyncService Build(ApplicationDbContext ctx)
    {
        var config = new ConfigurationBuilder().Build(); // leer; SendMissingDigest/Digest no-op
        var mail = new Mock<IMailService>();
        return new FaMaterializationSyncService(
            ctx, config, mail.Object, new UnknownWorkplaceState(),
            new ProductionOrderPickingStatusRepository(ctx),
            NullLogger<FaMaterializationSyncService>.Instance, new FakeSyncLogger());
    }

    [Fact]
    public async Task RunAsync_SubFaWithCoatedChild_SetsHasCoatingParts()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.AddRange(
            new FaHierarchyNode { HauptFA = 1000, SubFA = 10, VaterFA = null, Sollmenge = 1, Beschichtet = false },
            new FaHierarchyNode { HauptFA = 1000, SubFA = 0, VaterFA = 10, Sollmenge = 1, Beschichtet = true });
        await ctx.SaveChangesAsync();

        await Build(ctx).RunAsync(dryRun: false);

        var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "10");
        var status = await ctx.ProductionOrderPickingStatuses.SingleAsync(s => s.ProductionOrderId == order.Id);
        status.HasCoatingParts.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_FlagFlipsToFalse_AlsoResetsIsCoatingDone()
    {
        using var ctx = TestDbContextFactory.Create();
        // Quelle: Sub-FA 10 ohne beschichtetes direktes Kind mehr -> Ableitung liefert false.
        ctx.FaHierarchyNodes.Add(
            new FaHierarchyNode { HauptFA = 1000, SubFA = 10, VaterFA = null, Sollmenge = 1, Beschichtet = false });

        var order = new ProductionOrder
        {
            OrderNumber = "1000", SubOrderNumber = "10", Quantity = 1,
            CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test"
        };
        ctx.ProductionOrders.Add(order);
        await ctx.SaveChangesAsync();

        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus
        {
            ProductionOrderId = order.Id, HasCoatingParts = true, IsCoatingDone = true,
            CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test"
        });
        await ctx.SaveChangesAsync();

        await Build(ctx).RunAsync(dryRun: false);

        var status = await ctx.ProductionOrderPickingStatuses.SingleAsync(s => s.ProductionOrderId == order.Id);
        status.HasCoatingParts.Should().BeFalse();
        status.IsCoatingDone.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_DryRun_DoesNotWriteCoatingFlag()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.AddRange(
            new FaHierarchyNode { HauptFA = 1000, SubFA = 10, VaterFA = null, Sollmenge = 1, Beschichtet = false },
            new FaHierarchyNode { HauptFA = 1000, SubFA = 0, VaterFA = 10, Sollmenge = 1, Beschichtet = true });

        var order = new ProductionOrder
        {
            OrderNumber = "1000", SubOrderNumber = "10", Quantity = 1,
            CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test"
        };
        ctx.ProductionOrders.Add(order);
        await ctx.SaveChangesAsync();

        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus
        {
            ProductionOrderId = order.Id, HasCoatingParts = false,
            CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test"
        });
        await ctx.SaveChangesAsync();

        await Build(ctx).RunAsync(dryRun: true);

        var status = await ctx.ProductionOrderPickingStatuses.SingleAsync(s => s.ProductionOrderId == order.Id);
        status.HasCoatingParts.Should().BeFalse();
    }
}
