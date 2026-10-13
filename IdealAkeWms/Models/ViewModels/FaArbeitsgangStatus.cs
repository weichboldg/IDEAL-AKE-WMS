using System;
using System.Collections.Generic;
using System.Linq;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;

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

                    var soll = ArbeitsgangStatusRegel.Soll(bezug, kuerzel, offsets, sollRechner, out var ohneEintrag);
                    if (ohneEintrag) nichtKonfiguriert.Add(kuerzel);

                    // FA 7b: erledigt nur, wenn alle erledigt; laeuft, wenn eine (nicht erledigte) laeuft; Vorrang FA 7a in der Regel.
                    bool Erledigt(FaStatusWorkOperation o) => bde.Erledigt.Contains(o.Id);
                    var status = ArbeitsgangStatusRegel.Ableiten(aktiv.All(Erledigt),
                        aktiv.Any(o => !Erledigt(o) && bde.Laeuft.Contains(o.Id)), soll, heute);
                    t.Arbeitsgaenge[i] = sym with { Status = status, Soll = soll };
                }
            }
        }
        return new FaAgStatusErgebnis(nichtKonfiguriert.ToList(), mehrfach.ToList());
    }
}

/// <summary>Statusdarstellung (CSS-Klasse fuer site.css .fa-ag-*, Inline-SVG-Zeichen; Bootstrap-Icons-Pfade, MIT) — eine Quelle
/// fuer FA-Baum (Teil 2) und BDE-Terminal (Teil 3). Die Form traegt die Bedeutung, Farbe kommt ueber currentColor.</summary>
public static class FaAgStatusDarstellung
{
    private const string SvgOpen = "<svg class=\"fa-ag-sign\" viewBox=\"0 0 16 16\" width=\"11\" height=\"11\" fill=\"currentColor\" aria-hidden=\"true\" focusable=\"false\">";
    private const string SvgHaken = SvgOpen + "<path d=\"M13.854 3.646a.5.5 0 0 1 0 .708l-7 7a.5.5 0 0 1-.708 0l-3.5-3.5a.5.5 0 1 1 .708-.708L6.5 10.293l6.646-6.647a.5.5 0 0 1 .708 0\" stroke=\"currentColor\" stroke-width=\"1.2\"/></svg>";
    private const string SvgSanduhr = SvgOpen + "<path d=\"M2.5 15a.5.5 0 1 1 0-1h1v-1a4.5 4.5 0 0 1 2.557-4.06c.29-.139.443-.377.443-.59v-.7c0-.213-.154-.451-.443-.59A4.5 4.5 0 0 1 3.5 3V2h-1a.5.5 0 0 1 0-1h11a.5.5 0 0 1 0 1h-1v1a4.5 4.5 0 0 1-2.557 4.06c-.29.139-.443.377-.443.59v.7c0 .213.154.451.443.59A4.5 4.5 0 0 1 12.5 13v1h1a.5.5 0 0 1 0 1zm2-13v1c0 .537.12 1.045.337 1.5h6.326c.216-.455.337-.963.337-1.5V2zm3 6.35c0 .701-.478 1.236-1.011 1.492A3.5 3.5 0 0 0 4.5 13s.866-1.299 3-1.48zm1 0v3.17c2.134.181 3 1.48 3 1.48a3.5 3.5 0 0 0-1.989-3.158C8.978 9.586 8.5 9.052 8.5 8.351z\"/></svg>";
    private const string SvgDreieck = SvgOpen + "<path d=\"M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5m.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2\"/></svg>";
    private const string SvgKreis = SvgOpen + "<circle cx=\"8\" cy=\"8\" r=\"6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"/></svg>";

    /// <summary>Leer/leer fuer neutral (kein Status) und „kein BDE-Arbeitsgang“ (ohne Zeichen, nur Text).</summary>
    public static (string Css, string Svg) Fuer(FaAgStatus? status) => status switch
    {
        FaAgStatus.Erledigt => ("fa-ag-erledigt", SvgHaken),
        FaAgStatus.Laeuft => ("fa-ag-laeuft", SvgSanduhr),
        FaAgStatus.LaeuftUeberfaellig => ("fa-ag-laeuft fa-ag-laeuft-ueberfaellig", SvgSanduhr + SvgDreieck),
        FaAgStatus.Ueberfaellig => ("fa-ag-ueberfaellig", SvgDreieck),
        FaAgStatus.Offen => ("fa-ag-offen", SvgKreis),
        _ => ("", "")
    };
}
