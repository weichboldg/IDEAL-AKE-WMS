using System;
using System.Collections.Generic;
using IdealAkeWms.Models;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Reine Ableitung des Lackier-Flags aus der FA-Struktur (S8: DB-frei, unit-testbar).
/// Regel (Spec K2, FREIGABE-NACHTRAG 2026-10-07): Ein Sub-FA gilt als beschichtet, wenn er
/// SELBST beschichtet ist oder mindestens ein DIREKTES Blatt-Kind (VaterFA == SubFA, SubFA == 0)
/// beschichtet ist. Eine Kind-Zeile mit eigenem Sub-FA (SubFA != 0) zaehlt nur fuer sich selbst —
/// zum Lackierer gehen nur die Teile, nicht der Korpus (gleiche Regel wie die Arbeitsgang-Erkennung
/// seit v1.47.4). Enkel zaehlen ebenfalls nicht — sie tragen ihr Flag an ihrem eigenen Sub-FA.
/// Ersetzt fuer IDEAL die AKE-Heuristik (CoatingDetectionService, per Klasse-D-Gate aus).
/// </summary>
public static class FaMaterializationCoating
{
    public static Dictionary<string, bool> Derive(IReadOnlyCollection<FaHierarchyNode> nodes)
    {
        var coatedParents = new HashSet<int>();
        foreach (var n in nodes)
            if (n.Beschichtet && n.SubFA == 0 && n.VaterFA.HasValue)
                coatedParents.Add(n.VaterFA.Value);

        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var n in nodes)
        {
            if (n.SubFA == 0) continue;                 // Blaetter werden nicht materialisiert
            result[n.SubFA.ToString()] = n.Beschichtet || coatedParents.Contains(n.SubFA);
        }
        return result;
    }
}
