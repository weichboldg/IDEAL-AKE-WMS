using IdealAkeWms.Data;
using IdealAkeWms.Models;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// Helper fuer Tests, der einen <see cref="ProductionOrder"/> mit den seit Phase 1 (v1.11.0)
/// zugehoerigen Status-Zeilen anlegt:
/// - 1 <see cref="ProductionOrderPickingStatus"/>
/// - 1 <see cref="ProductionOrderBdeStatus"/>
///
/// Entspricht dem Spec-Eager-Create-Verhalten des Sage-AgentJobs.
/// Die frueheren 5 ProductionOrderAssemblyGroup-Zeilen entfallen seit v1.22.0
/// (ersetzt durch FaWorkSteps — Zeilen entstehen via Detection-Sync oder manuell,
/// Tests legen sie bei Bedarf selbst an).
/// </summary>
public static class TestDataHelper
{
    public sealed record CreatedOrder(
        ProductionOrder Order,
        ProductionOrderPickingStatus PickingStatus,
        ProductionOrderBdeStatus BdeStatus);

    public static CreatedOrder CreateOrderWithStatuses(
        ApplicationDbContext context,
        string orderNumber,
        decimal qty = 1m,
        bool releaseForPicking = false,
        bool isDone = false,
        int? pickingPriority = null,
        DateTime? productionDate = null,
        int? assignedPickerId = null,
        string? assignedPickerName = null,
        bool hasGlass = false,
        bool hasExternalPurchase = false,
        bool hasCoatingParts = false,
        bool isCoatingDone = false,
        bool isDonePicking = false,
        string? articleNumber = null,
        int orderId = 0)
    {
        var order = new ProductionOrder
        {
            OrderNumber = orderNumber,
            Quantity = qty,
            IsDone = isDone,
            ArticleNumber = articleNumber,
            ProductionDate = productionDate,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        if (orderId > 0) order.Id = orderId;

        context.ProductionOrders.Add(order);
        context.SaveChanges();

        var ps = new ProductionOrderPickingStatus
        {
            ProductionOrderId = order.Id,
            IsReleasedForPicking = releaseForPicking,
            PickingPriority = pickingPriority,
            AssignedPickerId = assignedPickerId,
            AssignedPickerName = assignedPickerName,
            HasGlass = hasGlass,
            HasExternalPurchase = hasExternalPurchase,
            HasCoatingParts = hasCoatingParts,
            IsCoatingDone = isCoatingDone,
            IsDonePicking = isDonePicking,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        if (releaseForPicking)
        {
            ps.ReleasedAt = DateTime.UtcNow;
            ps.ReleasedBy = "test";
        }

        var bde = new ProductionOrderBdeStatus
        {
            ProductionOrderId = order.Id,
            IsDoneBde = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

        context.ProductionOrderPickingStatuses.Add(ps);
        context.ProductionOrderBdeStatuses.Add(bde);
        context.SaveChanges();

        return new CreatedOrder(order, ps, bde);
    }
}
