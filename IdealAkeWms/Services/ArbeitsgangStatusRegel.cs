using System;
using System.Collections.Generic;
using System.Linq;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>
/// Gemeinsame Status-Regel der Arbeitsgaenge (FA-Struktur Teil 2 FA 4/7a, v1.55.0; seit Teil 3 v1.57.0 auch am BDE-Terminal) —
/// EIN Ort, den FA-Baum (<see cref="FaArbeitsgangStatus"/>) und Terminal (<see cref="BdeTerminalAuftraege"/>) aufrufen.
/// </summary>
public static class ArbeitsgangStatusRegel
{
    /// <summary>Vorrang FA 7a: erledigt → laeuft (mit Zusatz ueberfaellig) → ueberfaellig → offen. Ueberfaellig = Soll-Datum &lt; heute.</summary>
    public static FaAgStatus Ableiten(bool erledigt, bool laeuft, DateTime? soll, DateTime heute)
    {
        var ueberfaellig = soll is DateTime s && s.Date < heute.Date;
        return erledigt ? FaAgStatus.Erledigt
            : laeuft ? (ueberfaellig ? FaAgStatus.LaeuftUeberfaellig : FaAgStatus.Laeuft)
            : ueberfaellig ? FaAgStatus.Ueberfaellig : FaAgStatus.Offen;
    }

    /// <summary>Soll-Termin = Bezugstermin + Offset (Arbeitstage) aus dem Stammdaten-Lookup (Schluessel getrimmt; Lookup ist
    /// case-insensitiv). Ohne Eintrag Offset 0 (<paramref name="ohneEintrag"/> = true); ohne Bezugstermin kein Soll.</summary>
    public static DateTime? Soll(DateTime? bezug, string kuerzel, IReadOnlyDictionary<string, int> offsets,
        Func<DateTime, int, DateTime> rechner, out bool ohneEintrag)
    {
        ohneEintrag = !offsets.TryGetValue(kuerzel.Trim(), out var offset);
        return bezug is DateTime b ? rechner(b, offset) : null;
    }
}

/// <summary>Ein Arbeitsgang-Knopf am BDE-Terminal mit Fertigungstermin seines Auftrags, Status (Teil 3) und Prio seiner Haupt-FA
/// (v1.64.0, FA-1.5; null = keine gesetzt).</summary>
public sealed record BdeTerminalArbeitsgang(WorkOperation Op, DateTime? Fertigungstermin, FaArbeitsschrittSymbol Symbol, int? Prio = null);

/// <summary>
/// BDE-Terminal „Arbeitsgang waehlen“ (FA-Struktur Teil 3, v1.57.0): Fertigungstermin-Regel (FA 3), Status je Arbeitsgang
/// (keine W-4-Zusammenfassung: jede WorkOperation ein Knopf) und Sortierung (FA 4). Rein, unit-testbar.
/// </summary>
public static class BdeTerminalAuftraege
{
    /// <summary>IDEAL: fruehester nicht leerer FE_Termin der Kopfzeilen zur HauptFA (= OrderNumber); sonst — AKE, ohne Kopfzeile oder
    /// nur leere FE_Termin (Antwort 9) — <see cref="ProductionOrder.ProductionDate"/>.</summary>
    public static DateTime? Fertigungstermin(ProductionOrder po, ILookup<int, FaHierarchyOrderInfo> kopf)
        => (int.TryParse(po.OrderNumber, out var hauptFa) ? kopf[hauptFa].Select(o => o.FE_Termin).Min() : null) ?? po.ProductionDate;

    /// <summary>Prio der Haupt-FA (v1.64.0, Antwort 1): kleinster gesetzter Wert der Kopfzeilen (Kombigeraet), sonst null.</summary>
    public static int? Prio(ProductionOrder po, ILookup<int, FaHierarchyOrderInfo> kopf)
        => int.TryParse(po.OrderNumber, out var hauptFa) ? kopf[hauptFa].Select(o => o.Prio).Min() : null;

    /// <summary>Gruppiert nach Haupt-FA (<c>OrderNumber</c>, v1.64.0 FA-1.4): Gruppen nach Prio aufsteigend (leer zuletzt),
    /// fruehestem Fertigungstermin (Datumsteil, leer zuletzt), OrderNumber; innerhalb nach Fertigungstermin, SubOrderNumber,
    /// Auftrag, Sequence. Ohne Prio und mit einem Auftrag je OrderNumber (AKE) = die Reihenfolge vor v1.64.0.</summary>
    public static List<BdeTerminalArbeitsgang> Ordnen(IEnumerable<WorkOperation> ops, IReadOnlySet<int> laeuft,
        ILookup<int, FaHierarchyOrderInfo> kopf, IReadOnlyDictionary<string, int> offsets,
        Func<DateTime, int, DateTime> rechner, DateTime heute)
        => ops.Select(op =>
            {
                var termin = Fertigungstermin(op.ProductionOrder, kopf);
                var soll = ArbeitsgangStatusRegel.Soll(termin, op.OperationNumber, offsets, rechner, out _);
                var name = string.IsNullOrWhiteSpace(op.Name) ? null : op.Name.Trim();
                return new BdeTerminalArbeitsgang(op, termin, new FaArbeitsschrittSymbol(op.OperationNumber.Trim(), name)
                {
                    Status = ArbeitsgangStatusRegel.Ableiten(false, laeuft.Contains(op.Id), soll, heute),
                    Soll = soll
                }, Prio(op.ProductionOrder, kopf));
            })
            .GroupBy(a => a.Op.ProductionOrder.OrderNumber, StringComparer.Ordinal)
            .Select(g => (Prio: g.Min(a => a.Prio), Termin: g.Min(a => a.Fertigungstermin?.Date), g.Key, Eintraege: g))
            .OrderBy(g => g.Prio is null)
            .ThenBy(g => g.Prio)
            .ThenBy(g => g.Termin is null)
            .ThenBy(g => g.Termin)
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .SelectMany(g => g.Eintraege
                .OrderBy(a => a.Fertigungstermin is null)
                .ThenBy(a => a.Fertigungstermin?.Date)
                .ThenBy(a => a.Op.ProductionOrder.SubOrderNumber, StringComparer.Ordinal)
                .ThenBy(a => a.Op.ProductionOrderId)
                .ThenBy(a => a.Op.Sequence))
            .ToList();
}
