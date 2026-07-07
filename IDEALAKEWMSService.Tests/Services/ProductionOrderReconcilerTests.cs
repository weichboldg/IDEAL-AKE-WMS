using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class ProductionOrderReconcilerTests
{
    private static WmsOrderState Wms(string orderNumber, bool isDone = false, bool isCancelled = false)
        => new(orderNumber, isDone, isCancelled);

    [Fact]
    public void Plan_EmptySageRead_SkipsWithGuard_NothingToCancelOrReactivate()
    {
        var sage = new List<string>(); // leer -> Guard
        var wms = new[] { Wms("FA-1"), Wms("FA-CANCELLED", isCancelled: true) };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.Skipped.Should().BeTrue();
        plan.SkipReason.Should().Be("Sage-Read leer");
        plan.ToCancel.Should().BeEmpty();
        plan.ToReactivate.Should().BeEmpty();
    }

    [Fact]
    public void Plan_OrphanOpenFa_GoesToCancel()
    {
        var sage = new[] { "FA-1" };
        var wms = new[] { Wms("FA-1"), Wms("FA-ORPHAN") };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.Skipped.Should().BeFalse();
        plan.ToCancel.Should().ContainSingle().Which.Should().Be("FA-ORPHAN");
        plan.ToReactivate.Should().BeEmpty();
    }

    [Fact]
    public void Plan_DoneFaNotInSage_IsNotCancelled()
    {
        // Erledigte FAs sind nicht "offen" -> werden nie storniert.
        var sage = new[] { "FA-1" };
        var wms = new[] { Wms("FA-1"), Wms("FA-DONE", isDone: true) };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.ToCancel.Should().BeEmpty();
    }

    [Fact]
    public void Plan_CancelledFaBackInSage_GoesToReactivate()
    {
        var sage = new[] { "FA-1", "FA-BACK" };
        var wms = new[] { Wms("FA-1"), Wms("FA-BACK", isCancelled: true) };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.ToReactivate.Should().ContainSingle().Which.Should().Be("FA-BACK");
        plan.ToCancel.Should().BeEmpty();
    }

    [Fact]
    public void Plan_CancelledFaStillGone_StaysCancelled_NoDoubleCancel()
    {
        var sage = new[] { "FA-1" };
        var wms = new[] { Wms("FA-1"), Wms("FA-STILLGONE", isCancelled: true) };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.ToCancel.Should().BeEmpty();       // bereits storniert -> nicht erneut
        plan.ToReactivate.Should().BeEmpty();   // nicht in Sage -> nicht reaktivieren
        plan.Skipped.Should().BeFalse();
    }

    [Fact]
    public void Plan_ToCancelExceedsCap_Skips_ButReactivationsSurvive()
    {
        var sage = new[] { "FA-KEEP", "FA-BACK" };
        var wms = new[]
        {
            Wms("FA-KEEP"),
            Wms("FA-BACK", isCancelled: true), // Reaktivierung
            Wms("FA-ORPHAN-A"),
            Wms("FA-ORPHAN-B"),
            Wms("FA-ORPHAN-C"),
        };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 2);

        plan.Skipped.Should().BeTrue();
        plan.SkipReason.Should().Be("Cap ueberschritten (3 > 2)");
        plan.ToCancel.Should().BeEmpty(); // geleert
        plan.ToReactivate.Should().BeEquivalentTo(new[] { "FA-BACK" }); // Reaktivierungen bleiben
    }

    [Fact]
    public void Plan_OrderNumberMatching_IsCaseInsensitiveAndTrimmed()
    {
        // Defensive: Sage-View kann Trailing-Spaces liefern; Casing egal.
        var sage = new[] { " fa-1 " };
        var wms = new[] { Wms("FA-1") };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.ToCancel.Should().BeEmpty(); // FA-1 gilt als in Sage vorhanden
    }
}
