using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>
/// Regel fuer die Artikelgruppen-Trennung Lager vs. Glas (v1.25.0).
/// Glas-Bestellung: nur Gruppen aus GlasArtikelgruppen + GemeinsameArtikelgruppen.
/// Lager-Bestellung: alles AUSSER GlasArtikelgruppen; GemeinsameArtikelgruppen (z.B. EUZ)
/// sind IMMER in beiden erlaubt. Normalisierung: Trim, Uppercase, Sage-Suffix " - Name" ab.
/// </summary>
public static class GlasArticleGroupFilter
{
    public static string NormalizeGroup(string? group)
    {
        if (string.IsNullOrWhiteSpace(group)) return string.Empty;
        var g = group.Trim();
        // Sage liefert teils "940 - Kleinmaterial" — Articles speichert nur den Code.
        var dashIdx = g.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIdx > 0) g = g[..dashIdx].Trim();
        return g.ToUpperInvariant();
    }

    public static HashSet<string> ParseGroups(string? csv)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(csv)) return set;
        foreach (var token in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var n = NormalizeGroup(token);
            if (n.Length > 0) set.Add(n);
        }
        return set;
    }

    public static bool IsAllowedForType(string? articleGroup, WarehouseRequisitionType type,
        IReadOnlySet<string> glasGroups, IReadOnlySet<string> sharedGroups)
    {
        var g = NormalizeGroup(articleGroup);
        if (sharedGroups.Contains(g)) return true;
        return type == WarehouseRequisitionType.Glas
            ? glasGroups.Contains(g)
            : !glasGroups.Contains(g);
    }
}
