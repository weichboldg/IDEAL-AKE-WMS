using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.99.88 (Spec 2026-11-26 Leitstand nur HauptFA, Teil 2): fasst an der HauptFA-Zeile Lack-T, Glas, Zukauf und VK–VA
/// aus HauptFA und allen Sub-FAs des Auftrags zusammen (Regel Z1-Z5, Antworten 5/6). Rein, DB-frei.
/// Die Sub-FA-Zeilen kommen vom Aufrufer: nicht storniert, inkl. erledigter, ohne die HauptFA-Zeile selbst.
/// Ein Klick an der HauptFA-Zeile schreibt nur deren eigene Zeile (Antwort 7c); <see cref="PickingLeitstandItem.SubFaBestimmt"/>
/// markiert Zellen, in denen nicht jeder setzbare Wert der HauptFA-Zeile auch als Zusammenfassung erscheint (Antwort 8).
/// </summary>
public static class LeitstandHauptFaZusammenfassung
{
    public const string Glas = "HasGlass";
    public const string Zukauf = "HasExternalPurchase";
    public const string LackErledigt = "IsCoatingDone";

    public record SubFaZeile(ProductionOrderPickingStatus? Status, IReadOnlyDictionary<string, FaWorkStepPivotCell> WorkSteps);

    private static readonly FaWorkStepStatus[] AlleStatus = { FaWorkStepStatus.Offen, FaWorkStepStatus.InBearbeitung, FaWorkStepStatus.Fertig };

    public static void Anwenden(PickingLeitstandItem haupt, IReadOnlyList<SubFaZeile> subs)
    {
        haupt.SubFaAnzahl = subs.Count;

        // Z3/Z4: irgendeine Zeile. Sub-FA bestimmt, sobald eine Sub-FA gesetzt ist (Abwaehlen an der HauptFA wirkungslos, W1).
        var subGlas = subs.Any(s => s.Status?.HasGlass == true);
        if (subGlas) haupt.SubFaBestimmt.Add(Glas);
        haupt.HasGlass |= subGlas;
        var subZukauf = subs.Any(s => s.Status?.HasExternalPurchase == true);
        if (subZukauf) haupt.SubFaBestimmt.Add(Zukauf);
        haupt.HasExternalPurchase |= subZukauf;

        // Z1/Z2: Lack-T vorhanden = irgendeine; erledigt = alle Zeilen MIT Lackteilen.
        var subLack = subs.Where(s => s.Status?.HasCoatingParts == true).ToList();
        var subLackErledigt = subLack.All(s => s.Status!.IsCoatingDone);
        if (haupt.HasCoatingParts)
        {
            if (!subLackErledigt) haupt.SubFaBestimmt.Add(LackErledigt); // W2: Setzen bleibt unsichtbar
            haupt.IsCoatingDone = haupt.IsCoatingDone && subLackErledigt;
        }
        else if (subLack.Count > 0)
        {
            haupt.LackNurSubFa = true; // W3 (Antwort 9b): eigene Zeile zaehlt in Z2 nicht -> gesperrt
            haupt.HasCoatingParts = true;
            haupt.IsCoatingDone = subLackErledigt;
        }

        // Z5: je Kuerzel nur Zeilen mit dem Arbeitsgang.
        var codes = haupt.WorkSteps.Keys.Concat(subs.SelectMany(s => s.WorkSteps.Keys)).Distinct().ToList();
        var ergebnis = new Dictionary<string, FaWorkStepPivotCell>();
        foreach (var code in codes)
        {
            var subStatus = subs.Where(s => s.WorkSteps.ContainsKey(code)).Select(s => s.WorkSteps[code].Status).ToList();
            if (haupt.WorkSteps.TryGetValue(code, out var eigen))
            {
                if (AlleStatus.Any(st => Regel(subStatus.Append(st)) != st)) haupt.SubFaBestimmt.Add(code); // W5
                ergebnis[code] = eigen with { Status = Regel(subStatus.Append(eigen.Status)) };
            }
            else
            {
                ergebnis[code] = new FaWorkStepPivotCell(0, Regel(subStatus)); // W4: nichts an der HauptFA schreibbar -> gesperrt
            }
        }
        haupt.WorkSteps = ergebnis;
    }

    /// <summary>Antwort 6: alle Fertig -> Fertig; alle Offen -> Offen; sonst InBearbeitung (gemischt Fertig/Offen inklusive).</summary>
    public static FaWorkStepStatus Regel(IEnumerable<FaWorkStepStatus> status)
    {
        var l = status.ToList();
        if (l.All(s => s == FaWorkStepStatus.Fertig)) return FaWorkStepStatus.Fertig;
        if (l.All(s => s == FaWorkStepStatus.Offen)) return FaWorkStepStatus.Offen;
        return FaWorkStepStatus.InBearbeitung;
    }
}
