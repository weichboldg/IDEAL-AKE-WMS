using Microsoft.AspNetCore.Http;

namespace IdealAkeWms.Services;

/// <summary>
/// Liest die Spaltenfilter aus der Query (URL-Schema: <c>colf_&lt;col-key&gt;=value</c>) und filtert geladene Listen im Speicher.
/// Die Bedeutung eines Filterwerts steht ausschliesslich im Grammatik-Kern <see cref="ColumnFilterSyntax"/> (C#) bzw.
/// <c>wwwroot/js/column-filter-syntax.js</c> (Browser) — ADR 0017; SQL-Wege bauen ueber <see cref="ColumnFilterSql"/>.
/// </summary>
public static class ColumnFilterHelper
{
    private const string QueryPrefix = "colf_";

    /// <summary>
    /// Returns a dict <c>col-key -&gt; raw value</c> (without the <c>colf_</c> prefix)
    /// from the request query string. Empty values are skipped.
    /// </summary>
    public static Dictionary<string, string> ReadFromQuery(HttpRequest? request)
    {
        var result = new Dictionary<string, string>();
        if (request?.Query == null) return result;
        foreach (var (key, value) in request.Query)
        {
            if (!key.StartsWith(QueryPrefix, StringComparison.Ordinal)) continue;
            var raw = value.ToString();
            if (string.IsNullOrWhiteSpace(raw)) continue;
            result[key.Substring(QueryPrefix.Length)] = raw;
        }
        return result;
    }

    /// <summary>True wenn mindestens ein Filter einen wirksamen Teil enthaelt (Grammatik-Kern). <paramref name="types"/> = Register der
    /// Liste wie bei <see cref="Apply{T}"/> — sonst gilt <c>&gt;10</c> in einer Zahlspalte als wirkungslos, obwohl Apply damit filtert
    /// (Druck-Banner „Gefilterte Ansicht“ fehlte, QA Epic Suchsyntax I-1).</summary>
    public static bool HasAny(IReadOnlyDictionary<string, string>? filters, IReadOnlyDictionary<string, ColumnFilterType>? types = null)
        => filters != null && filters.Any(kv => !ColumnFilterSyntax.Parse(kv.Value,
            types != null && types.TryGetValue(kv.Key, out var t) ? t : ColumnFilterType.Text).IsEmpty);

    /// <summary>
    /// In-Memory-Variante fuer Listen die bereits geladene Daten paginieren.
    /// Spalten-Map: <c>col-key -&gt; getter(item) -&gt; searchable string</c>.
    /// Unbekannte col-keys werden ignoriert.
    /// Seit v1.58.0 (Spaltenfilter-Suchsyntax, Etappe A) ueber den Grammatik-Kern <see cref="ColumnFilterSyntax"/>: alle
    /// Aufrufer verstehen die neue Syntax ohne eigene Aenderung (Antwort 4); ohne <paramref name="types"/> gilt jede Spalte als Text.
    /// </summary>
    public static IEnumerable<T> Apply<T>(
        IEnumerable<T> source,
        IReadOnlyDictionary<string, string>? filters,
        IReadOnlyDictionary<string, Func<T, string?>> colMap,
        IReadOnlyDictionary<string, ColumnFilterType>? types = null)
    {
        if (filters == null || filters.Count == 0) return source;

        var query = source;
        foreach (var (key, raw) in filters)
        {
            if (!colMap.TryGetValue(key, out var getter)) continue;
            var type = types != null && types.TryGetValue(key, out var t) ? t : ColumnFilterType.Text;
            var parsed = ColumnFilterSyntax.Parse(raw, type);
            if (parsed.IsEmpty) continue;
            query = query.Where(item => ColumnFilterSyntax.Matches(getter(item), parsed, type));
        }
        return query;
    }
}
