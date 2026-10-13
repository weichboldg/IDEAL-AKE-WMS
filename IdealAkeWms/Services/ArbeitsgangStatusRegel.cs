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

/// <summary>Ein Arbeitsgang-Knopf am BDE-Terminal mit Fertigungstermin seines Auftrags und Status (Teil 3).</summary>
public sealed record BdeTerminalArbeitsgang(WorkOperation Op, DateTime? Fertigungstermin, FaArbeitsschrittSymbol Symbol);

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

    /// <summary>Aufsteigend nach Fertigungstermin (Datumsteil), ohne Termin am Ende; Gleichstand OrderNumber, SubOrderNumber;
    /// je Auftrag zusammenhaengend nach Sequence.</summary>
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
                });
            })
            .OrderBy(a => a.Fertigungstermin is null)
            .ThenBy(a => a.Fertigungstermin?.Date)
            .ThenBy(a => a.Op.ProductionOrder.OrderNumber, StringComparer.Ordinal)
            .ThenBy(a => a.Op.ProductionOrder.SubOrderNumber, StringComparer.Ordinal)
            .ThenBy(a => a.Op.ProductionOrderId)
            .ThenBy(a => a.Op.Sequence)
            .ToList();
}
