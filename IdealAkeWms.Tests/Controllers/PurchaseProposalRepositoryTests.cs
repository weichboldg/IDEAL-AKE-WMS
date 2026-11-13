using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.20 Spec 2026-11-08-einkauf-bestellvorschlaege F2 „Offene Bedarfe“ (AK 5).</summary>
public class PurchaseProposalRepositoryTests
{
    [Fact]
    public async Task OffeneBedarfe_SubmittedVoll_TeilgeliefertNurRestNachlieferung_SonstNicht()
    {
        using var db = TestDbContextFactory.Create();
        void Bestellung(int id, WarehouseRequisitionStatus status, params (string Nr, decimal Soll, decimal? Ist, ShortageStatus S)[] pos)
        {
            db.WarehouseRequisitions.Add(new WarehouseRequisition { Id = id, ProductionWorkplaceId = 1, Status = status, CreatedBy = "t", CreatedByWindows = "t" });
            var p = 0;
            foreach (var x in pos)
                db.WarehouseRequisitionItems.Add(new WarehouseRequisitionItem
                {
                    WarehouseRequisitionId = id, Position = ++p, ArticleNumber = x.Nr, ArticleDescription = "x", QuantityRequested = x.Soll,
                    QuantityPicked = x.Ist, ShortageStatus = x.S, CreatedBy = "t", CreatedByWindows = "t",
                });
        }
        Bestellung(1, WarehouseRequisitionStatus.Submitted, ("A1", 4m, null, ShortageStatus.None));
        Bestellung(2, WarehouseRequisitionStatus.PartiallyDelivered, ("A1", 10m, 6m, ShortageStatus.WillBeRestocked), ("A1", 5m, 1m, ShortageStatus.NoRestock));
        Bestellung(3, WarehouseRequisitionStatus.Draft, ("A1", 100m, null, ShortageStatus.None));
        Bestellung(4, WarehouseRequisitionStatus.Closed, ("A1", 100m, 0m, ShortageStatus.WillBeRestocked));
        Bestellung(5, WarehouseRequisitionStatus.Cancelled, ("A1", 100m, null, ShortageStatus.None));
        foreach (var (nr, status, menge) in new[] { ("A1", PartRequisitionStatus.Offen, 2m), ("A1", PartRequisitionStatus.Erfuellt, 50m), ("A1", PartRequisitionStatus.Storniert, 50m) })
            db.PartRequisitions.Add(new PartRequisition { ProductionOrderId = 1, ArticleNumber = nr, Status = status, Quantity = menge, CreatedBy = "t", CreatedByWindows = "t" });
        await db.SaveChangesAsync();

        var repo = new PurchaseProposalRepository(db);
        (await repo.GetOffeneLagerbestellungenAsync()).Select(b => b.Menge).Should().BeEquivalentTo(new[] { 4m, 4m });
        (await repo.GetOffeneBedarfsmeldungenAsync()).Select(b => b.Menge).Should().Equal(2m);
    }
}
