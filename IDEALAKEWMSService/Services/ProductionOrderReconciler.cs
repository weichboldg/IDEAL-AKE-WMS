using System.Collections.Generic;
using System.Linq;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Zustand einer WMS-FA fuer den Reconcile-Abgleich (rein wertbasiert, keine DB).
/// </summary>
public sealed record WmsOrderState(string OrderNumber, bool IsDone, bool IsCancelled);

/// <summary>
/// Ergebnis des Reconcile-Abgleichs. <see cref="ToCancel"/>/<see cref="ToReactivate"/>
/// sind OrderNumbers. Bei <see cref="Skipped"/>=true wird KEIN Storno geschrieben
/// (Reaktivierungen bleiben trotzdem gueltig — siehe Cap-Regel).
/// </summary>
public sealed record ReconcilePlan(
    IReadOnlyList<string> ToCancel,
    IReadOnlyList<string> ToReactivate,
    bool Skipped,
    string? SkipReason);

/// <summary>
/// Reine Reconcile-Logik (voll unit-testbar, keine DB). Vergleicht die aus der
/// Sage-View gelesenen OrderNumbers mit dem WMS-Zustand und liefert, was storniert
/// bzw. reaktiviert werden muss. Guard (leerer Sage-Read) + Cap schuetzen vor
/// versehentlichem Massen-Stornieren bei Sage-Teil-/Ausfaellen.
/// </summary>
public static class ProductionOrderReconciler
{
    public static ReconcilePlan Plan(
        IReadOnlyCollection<string> sageOrderNumbers,
        IReadOnlyCollection<WmsOrderState> wmsOrders,
        int maxCancelPerRun)
    {
        // Guard: leerer Sage-Read -> nichts anfassen (Sage-Ausfall/Teil-Read).
        if (sageOrderNumbers.Count == 0)
        {
            return new ReconcilePlan(
                Array.Empty<string>(), Array.Empty<string>(),
                Skipped: true, SkipReason: "Sage-Read leer");
        }

        var sageSet = new HashSet<string>(
            sageOrderNumbers
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim()),
            StringComparer.OrdinalIgnoreCase);

        // Reaktivieren: WMS-storniert, aber wieder in Sage vorhanden.
        var toReactivate = wmsOrders
            .Where(o => o.IsCancelled && sageSet.Contains(o.OrderNumber.Trim()))
            .Select(o => o.OrderNumber)
            .ToList();

        // Stornieren: offen (nicht IsDone, nicht bereits storniert) UND nicht in Sage.
        var toCancel = wmsOrders
            .Where(o => !o.IsDone && !o.IsCancelled && !sageSet.Contains(o.OrderNumber.Trim()))
            .Select(o => o.OrderNumber)
            .ToList();

        // Cap: zu viele Storni -> kein Storno (aber Reaktivierungen bleiben).
        if (toCancel.Count > maxCancelPerRun)
        {
            return new ReconcilePlan(
                Array.Empty<string>(), toReactivate,
                Skipped: true,
                SkipReason: $"Cap ueberschritten ({toCancel.Count} > {maxCancelPerRun})");
        }

        return new ReconcilePlan(toCancel, toReactivate, Skipped: false, SkipReason: null);
    }
}
