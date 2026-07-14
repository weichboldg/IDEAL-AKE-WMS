using System;
using System.Collections.Generic;
using System.Linq;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Ergebnis des Nullsetz-Abgleichs. <see cref="ToZero"/> traegt je Eintrag den zu
/// nullenden WMS-Bestand mit (fuer die Korrekturbuchung). Bei <see cref="Skipped"/>=true
/// wird KEINE Nullkorrektur geschrieben (Guard/Cap).
/// </summary>
public sealed record LagerbestandZeroingPlan(
    IReadOnlyList<(int ArticleId, int StorageLocationId, decimal WmsBestand)> ToZero,
    bool Skipped,
    string? SkipReason);

/// <summary>
/// Reine Nullsetz-Logik (voll unit-testbar, keine DB). Findet WMS-verwaltete Bestaende
/// (Sage-Quelle + aktiv + Menge != 0), deren (Artikel, Lagerplatz)-Paar nicht mehr im
/// aktuellen Sage-Snapshot vorkommt, und liefert die auf 0 zu korrigierenden Paare.
/// Leer-Guard (leerer Sage-Read) + Cap schuetzen vor Massen-Nullsetzen bei Sage-Teil-/Ausfaellen.
/// </summary>
public static class LagerbestandZeroingPlanner
{
    public static LagerbestandZeroingPlan Plan(
        int sageRowCountRaw,
        IReadOnlySet<(int ArticleId, int StorageLocationId)> sagePresentKeys,
        IReadOnlyDictionary<(int ArticleId, int StorageLocationId), decimal> managedStock,
        int maxPerRun)
    {
        // Guard: leerer Sage-Read -> nichts anfassen (Sage-Ausfall/Teil-Read).
        if (sageRowCountRaw == 0)
        {
            return new LagerbestandZeroingPlan(
                Array.Empty<(int, int, decimal)>(),
                Skipped: true,
                SkipReason: "Sage-Read leer (0 Zeilen)");
        }

        // Kandidaten: managedStock-Keys, die NICHT im Sage-Snapshot vorkommen.
        var candidates = managedStock
            .Where(kv => !sagePresentKeys.Contains(kv.Key))
            .Select(kv => (kv.Key.ArticleId, kv.Key.StorageLocationId, kv.Value))
            .ToList();

        // Cap: zu viele Kandidaten -> kein Nullsetzen (Schutz vor Sage-Teil-Read).
        if (candidates.Count > maxPerRun)
        {
            return new LagerbestandZeroingPlan(
                Array.Empty<(int, int, decimal)>(),
                Skipped: true,
                SkipReason: $"Cap ueberschritten: {candidates.Count} > {maxPerRun} — kein Nullsetzen");
        }

        return new LagerbestandZeroingPlan(candidates, Skipped: false, SkipReason: null);
    }
}
