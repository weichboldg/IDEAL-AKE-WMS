namespace IdealAkeWms.Services;

/// <summary>
/// Werkbank-Filter fuer die FA-Abarbeitungsliste: komma-separierte Tokens, Treffer wenn der
/// Werkbank-Name (case-insensitiv) EINEN der Tokens ENTHAELT (Komma-OR). Leerer/Null-Filter =
/// alle. Gleiche Contains-Semantik wie der `workbench`-Spaltenfilter (ColumnFilterHelper).
/// </summary>
public static class WorkbenchFilter
{
    public static bool Matches(string? workplaceName, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;

        var tokens = filter
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Where(t => t.Length > 0)
            .ToList();
        if (tokens.Count == 0) return true;

        var name = (workplaceName ?? string.Empty).ToLowerInvariant();
        return tokens.Any(t => name.Contains(t));
    }
}
