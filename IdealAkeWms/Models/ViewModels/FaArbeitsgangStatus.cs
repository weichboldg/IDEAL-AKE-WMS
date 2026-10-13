using System;
using System.Collections.Generic;
using System.Linq;
using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>Status eines Arbeitsgang-Symbols im FA-Baum (FA-Struktur Teil 2, v1.55.0). <c>null</c> am Symbol = neutral wie Teil 1.</summary>
public enum FaAgStatus
{
    /// <summary>Kein passender Arbeitsgang (Antwort 6 b, W-2, W-3) — neutral, nur Text.</summary>
    KeinBdeArbeitsgang,
    Offen,
    Ueberfaellig,
    Laeuft,
    LaeuftUeberfaellig,
    Erledigt
}

/// <summary>Ergebnis des Status-Laufs: Sammelhinweis-Kuerzel (Offset fehlt) und Mehrfach-Treffer fuer die eine LogWarning (W-4).</summary>
public sealed record FaAgStatusErgebnis(IReadOnlyList<string> NichtKonfiguriert, IReadOnlyList<string> Mehrfach);

/// <summary>
/// Reine Status-Ableitung (Spec 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2): Zuordnung Kuerzel → <c>WorkOperation</c>
/// (FA 9 a: SubFA ≠ 0 → eigener Auftrag, sonst VaterFA nur wenn dieser Knoten in der Struktur existiert — W-3; kein
/// Vater-Rueckfall — W-2), Werkbank-Gate (FA 6), Zusammenfassung (FA 7b/W-4), Soll-Termin = fruehester FE_Termin + Offset.
/// Nur bei BDE global an aufrufen — bei BDE aus bleiben die Symbole wie Teil 1.
/// </summary>
public static class FaArbeitsgangStatus
{
    public static FaAgStatusErgebnis Zuordnen(
        IEnumerable<FaHierarchyStructure> structures,
        IEnumerable<FaStatusWorkOperation> operations,
        IReadOnlySet<int> bdeAktiveWerkbaenke,
        FaBdeStatus bde,
        IReadOnlyDictionary<string, int> offsets,
        Func<DateTime, int, DateTime> sollRechner,
        DateTime heute)
    {
        // Schluessel (OrderNumber, SubOrderNumber, Kuerzel) — Kuerzel getrimmt, case-insensitiv wie die Erkennung.
        var ops = operations
            .GroupBy(o => (o.OrderNumber, o.SubOrderNumber, Kuerzel: o.OperationNumber.Trim().ToUpperInvariant()))
            .ToDictionary(g => g.Key, g => g.ToList());
        var nichtKonfiguriert = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var mehrfach = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var s in structures)
        {
            var hauptFa = s.HauptFA.ToString();
            var subFas = new HashSet<int>();
            var alle = new List<FaHierarchyTreeNode>();
            var offen = new Stack<FaHierarchyTreeNode>(s.Roots);
            while (offen.Count > 0)
            {
                var t = offen.Pop();
                alle.Add(t);
                if (t.Node.SubFA != 0) subFas.Add(t.Node.SubFA);
                foreach (var c in t.Children) offen.Push(c);
            }
            // Kombigeraet (Antwort 9): fruehester nicht leerer FE_Termin; keiner → nie ueberfaellig.
            var bezug = s.OrderInfos.Select(o => o.FE_Termin).Min(); // Min ueber DateTime? ignoriert null

            foreach (var t in alle)
            {
                int? auftrag = t.Node.SubFA != 0 ? t.Node.SubFA
                    : t.Node.VaterFA is int v && v != 0 && subFas.Contains(v) ? v : null;
                for (var i = 0; i < t.Arbeitsgaenge.Count; i++)
                {
                    var sym = t.Arbeitsgaenge[i];
                    var kuerzel = sym.Kuerzel.Trim();
                    if (auftrag is not int a || !ops.TryGetValue((hauptFa, a.ToString(), kuerzel.ToUpperInvariant()), out var treffer))
                    {
                        t.Arbeitsgaenge[i] = sym with { Status = FaAgStatus.KeinBdeArbeitsgang };
                        continue;
                    }
                    if (treffer.Count > 1)
                        mehrfach.Add($"{hauptFa}/{a} {kuerzel}: {treffer.Count}× (Ids {string.Join(", ", treffer.Select(o => o.Id))})");

                    var aktiv = treffer.Where(o => o.ProductionWorkplaceId is int w && bdeAktiveWerkbaenke.Contains(w)).ToList();
                    if (aktiv.Count == 0) continue; // Werkbank-Gate: neutral wie Teil 1

                    if (!offsets.TryGetValue(kuerzel, out var offset)) nichtKonfiguriert.Add(kuerzel);
                    DateTime? soll = bezug is DateTime b ? sollRechner(b, offset) : null;
                    var ueberfaellig = soll is DateTime sd && sd.Date < heute.Date;

                    // FA 7a/7b: laeuft, wenn eine laeuft; erledigt nur, wenn alle erledigt; sonst offen.
                    bool Erledigt(FaStatusWorkOperation o) => bde.Erledigt.Contains(o.Id);
                    var status = aktiv.Any(o => !Erledigt(o) && bde.Laeuft.Contains(o.Id))
                        ? (ueberfaellig ? FaAgStatus.LaeuftUeberfaellig : FaAgStatus.Laeuft)
                        : aktiv.All(Erledigt) ? FaAgStatus.Erledigt
                        : ueberfaellig ? FaAgStatus.Ueberfaellig : FaAgStatus.Offen;
                    t.Arbeitsgaenge[i] = sym with { Status = status, Soll = soll };
                }
            }
        }
        return new FaAgStatusErgebnis(nichtKonfiguriert.ToList(), mehrfach.ToList());
    }
}
