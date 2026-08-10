using System;
using System.Collections.Generic;
using System.Linq;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services;

/// <summary>
/// Baut aus den flachen <see cref="FaHierarchyNode"/>-Zeilen die rekursive Baumstruktur je HauptFA
/// (IDEAL Teil 2), analog <see cref="ReadOnlyBomBuilder"/> — aber ueber echte Zeiger-Rekursion
/// (VaterFA -&gt; SubFA), nicht ueber eine Positions-Zeichenkette.
///
/// Kern-Invarianten (Spec Teil 2, B-3/B-4/S-3):
/// <list type="bullet">
///   <item><b>Zyklenschutz:</b> Visited-Set der besuchten SubFA im aktuellen Pfad.</item>
///   <item><b>Tiefen-Cap:</b> konfigurierbar (Default 500), reine Reissleine.</item>
///   <item>Verstoss bricht <b>nur die betroffene Struktur</b> ab (ILogger-Warnung + Fehler-Markierung,
///     <b>keine</b> Fehlermail/kein SyncLog aus dem Web — „Das Web verschickt keine Mails").</item>
///   <item><b>Waisen:</b> Knoten mit VaterFA ohne importierten Parent werden eigene, markierte
///     Pseudowurzel-Strukturen (zaehlen fuer die Pagination).</item>
/// </list>
/// </summary>
public class FaHierarchyTreeBuilder
{
    public const int DefaultDepthCap = 500;

    private readonly ILogger<FaHierarchyTreeBuilder> _logger;

    public FaHierarchyTreeBuilder(ILogger<FaHierarchyTreeBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Baut alle Strukturen aus der uebergebenen Knotenmenge. Reihenfolge: HauptFA aufsteigend,
    /// je Gruppe Wurzel(n) vor Waisen-Pseudowurzeln. Der Aufrufer paginiert danach ueber die
    /// Struktur-Liste (nie ueber Knoten-Zeilen).
    /// </summary>
    public List<FaHierarchyStructure> Build(
        IReadOnlyCollection<FaHierarchyNode> nodes,
        IReadOnlyCollection<FaHierarchyOrderInfo> orderInfos,
        int depthCap)
    {
        var result = new List<FaHierarchyStructure>();
        if (nodes == null || nodes.Count == 0) return result;
        if (depthCap < 1) depthCap = DefaultDepthCap;

        var orderInfosByHauptFa = (orderInfos ?? Array.Empty<FaHierarchyOrderInfo>())
            .GroupBy(o => o.HauptFA)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var group in nodes.GroupBy(n => n.HauptFA).OrderBy(g => g.Key))
        {
            var groupNodes = group.ToList();

            // Kinder-Lookup: ein Kind haengt an parent.SubFA (child.VaterFA == parent.SubFA).
            var childrenByVaterFa = groupNodes
                .Where(n => n.VaterFA.HasValue)
                .GroupBy(n => n.VaterFA!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Alle innerhalb der Gruppe existierenden SubFA-Werte (fuer Waisen-Erkennung).
            var subFaSet = groupNodes.Select(n => n.SubFA).ToHashSet();

            var infos = orderInfosByHauptFa.TryGetValue(group.Key, out var oi)
                ? oi
                : new List<FaHierarchyOrderInfo>();
            var isAmbiguous = infos
                .Select(i => i.MontageAbteilung ?? string.Empty)
                .Distinct()
                .Count() > 1;

            // (1) echte Wurzeln (VaterFA IS NULL)
            var roots = groupNodes.Where(n => n.VaterFA == null).ToList();
            // (2) Waisen: VaterFA gesetzt, aber Ziel nicht in der Gruppe importiert
            var orphans = groupNodes
                .Where(n => n.VaterFA.HasValue && !subFaSet.Contains(n.VaterFA.Value))
                .ToList();

            foreach (var root in roots)
                result.Add(BuildStructure(group.Key, root, childrenByVaterFa, infos, isAmbiguous, depthCap, isOrphan: false));

            foreach (var orphan in orphans)
                result.Add(BuildStructure(group.Key, orphan, childrenByVaterFa, infos, isAmbiguous, depthCap, isOrphan: true));
        }

        return result;
    }

    private FaHierarchyStructure BuildStructure(
        int hauptFa,
        FaHierarchyNode root,
        Dictionary<int, List<FaHierarchyNode>> childrenByVaterFa,
        List<FaHierarchyOrderInfo> infos,
        bool isAmbiguous,
        int depthCap,
        bool isOrphan)
    {
        var structure = new FaHierarchyStructure
        {
            HauptFA = hauptFa,
            OrderInfos = infos,
            IsAmbiguous = isAmbiguous,
            IsOrphan = isOrphan,
            OrphanVaterFa = isOrphan ? root.VaterFA : null,
        };

        var path = new HashSet<int>();
        var count = 0;
        var rootTreeNode = BuildNode(root, childrenByVaterFa, path, 0, depthCap, structure, ref count);
        structure.Roots.Add(rootTreeNode);
        structure.NodeCount = count;

        if (structure.HasError)
        {
            // Querschnittsregel „Das Web verschickt keine Mails": nur ILogger, keine Mail, kein SyncLog.
            _logger.LogWarning(
                "FaHierarchy-Baumaufbau abgebrochen: HauptFA={HauptFA}, Grund={Reason}, Abbruchstelle SubFA={SubFA}, Tiefen-Cap={DepthCap}.",
                hauptFa, structure.ErrorMessage, structure.ErrorAtSubFa, depthCap);
        }

        return structure;
    }

    private static FaHierarchyTreeNode BuildNode(
        FaHierarchyNode node,
        Dictionary<int, List<FaHierarchyNode>> childrenByVaterFa,
        HashSet<int> path,
        int level,
        int depthCap,
        FaHierarchyStructure structure,
        ref int count)
    {
        count++;
        var treeNode = new FaHierarchyTreeNode { Node = node, Level = level };

        // Tiefen-Cap (Reissleine): ab hier nicht weiter absteigen.
        if (level >= depthCap)
        {
            structure.MarkError($"Tiefen-Cap {depthCap} erreicht", node.SubFA);
            return treeNode;
        }

        // Blatt: SubFA == 0 (Kaufteil/Endmaterial ohne eigenen FA) hat keine Kinder.
        if (node.SubFA == 0)
            return treeNode;

        // Zyklenschutz: SubFA bereits im aktuellen Pfad -> Zyklus.
        if (!path.Add(node.SubFA))
        {
            structure.MarkError($"Zyklus erkannt (SubFA {node.SubFA} erneut im Pfad)", node.SubFA);
            return treeNode;
        }

        if (childrenByVaterFa.TryGetValue(node.SubFA, out var children))
        {
            foreach (var child in children)
            {
                treeNode.Children.Add(
                    BuildNode(child, childrenByVaterFa, path, level + 1, depthCap, structure, ref count));

                // Erster Verstoss bricht den Aufbau DIESER Struktur ab (uebrige Strukturen unberuehrt).
                if (structure.HasError)
                    break;
            }
        }

        path.Remove(node.SubFA);
        return treeNode;
    }
}
