using System.Collections.Generic;
using System.Linq;

namespace IDEALAKEWMSService.Services;

/// <summary>Quell-FA (aus der lokalen FA-Struktur, Teil 1) — rein wertbasiert.</summary>
public sealed record MaterializationSourceOrder(
    string SubOrderNumber, string OrderNumber, string? ParentSubOrderNumber,
    string? Arbeitsbereich, bool HasCoatingParts);

/// <summary>Zustand einer bereits materialisierten WMS-FA — rein wertbasiert.</summary>
public sealed record MaterializationExistingOrder(
    string SubOrderNumber, string OrderNumber, string? ParentSubOrderNumber,
    bool IsMissingMarked, bool IsKnownDone);

/// <summary>Erkannte Umhängung: Vater in der Quelle weicht vom gespeicherten Wert ab.</summary>
public sealed record ReparentConflict(string SubOrderNumber, string? OldParent, string? NewParent);

/// <summary>
/// Ergebnis der Materialisierung. Die Listen tragen <c>SubOrderNumber</c>-Schlüssel. Bei
/// <see cref="Skipped"/>=true wird NICHTS geschrieben.
/// </summary>
public sealed record MaterializationPlan(
    IReadOnlyList<string> ToCreate,
    IReadOnlyList<string> ToClearMissing,
    IReadOnlyList<string> ToMarkMissing,
    IReadOnlyList<ReparentConflict> ReparentConflicts,
    IReadOnlyList<string> MissingToNotify,
    bool Skipped,
    string? SkipReason);

/// <summary>
/// Reine, unit-testbare Sync-Regeln der Struktur→ProductionOrders-Materialisierung (Muster
/// <see cref="ProductionOrderReconciler"/>, keine DB). Drei Regeln: (1) neuer Sub-FA → anlegen;
/// (2) verschwundener FA → NICHT löschen, <c>SageMissingSince</c> setzen (selbstheilend beim
/// Wiederauftauchen); (3) Umhängung → melden, nicht still übernehmen. Empty-Source-Guard schützt
/// vor Massen-Markierung bei leerem Struktur-Read (analog Reconciler-Guard).
/// </summary>
public static class FaMaterializationPlanner
{
    public static MaterializationPlan Plan(
        IReadOnlyCollection<MaterializationSourceOrder> source,
        IReadOnlyCollection<MaterializationExistingOrder> existing)
    {
        var empty = System.Array.Empty<string>();
        if (source.Count == 0)
            return new MaterializationPlan(empty, empty, empty,
                System.Array.Empty<ReparentConflict>(), empty,
                Skipped: true, SkipReason: "Struktur-Quelle leer");

        // Dedup je SubOrderNumber (erste Zeile gewinnt; Quelle sollte eindeutig sein).
        var srcByKey = new Dictionary<string, MaterializationSourceOrder>(System.StringComparer.Ordinal);
        foreach (var s in source)
            if (!string.IsNullOrWhiteSpace(s.SubOrderNumber))
                srcByKey.TryAdd(s.SubOrderNumber, s);

        var existByKey = new Dictionary<string, MaterializationExistingOrder>(System.StringComparer.Ordinal);
        foreach (var e in existing)
            existByKey.TryAdd(e.SubOrderNumber, e);

        var toCreate = srcByKey.Keys.Where(k => !existByKey.ContainsKey(k)).ToList();

        var toClearMissing = new List<string>();
        var toMarkMissing = new List<string>();
        var reparent = new List<ReparentConflict>();
        var missingToNotify = new List<string>();

        foreach (var e in existByKey.Values)
        {
            if (srcByKey.TryGetValue(e.SubOrderNumber, out var s))
            {
                if (e.IsMissingMarked)
                    toClearMissing.Add(e.SubOrderNumber); // Regel 2: wieder aufgetaucht
                if (!ParentEquals(e.ParentSubOrderNumber, s.ParentSubOrderNumber))
                    reparent.Add(new ReparentConflict(e.SubOrderNumber,
                        Norm(e.ParentSubOrderNumber), Norm(s.ParentSubOrderNumber))); // Regel 3
            }
            else
            {
                if (!e.IsMissingMarked)
                {
                    toMarkMissing.Add(e.SubOrderNumber); // Regel 2: verschwunden
                    if (!e.IsKnownDone)
                        missingToNotify.Add(e.SubOrderNumber);
                }
            }
        }

        return new MaterializationPlan(toCreate, toClearMissing, toMarkMissing,
            reparent, missingToNotify, Skipped: false, SkipReason: null);
    }

    private static string? Norm(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static bool ParentEquals(string? a, string? b) =>
        string.Equals(Norm(a), Norm(b), System.StringComparison.Ordinal);
}
