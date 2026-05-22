using Microsoft.AspNetCore.Http;

namespace IdealAkeWms.Services;

/// <summary>
/// Reads and parses column-filter query-string params (URL-Schema: <c>colf_&lt;col-key&gt;=value</c>)
/// and supports the same OR (',') / NOT ('!') Mini-Syntax wie der bisherige
/// clientseitige Filter in <c>table-filter.js</c>.
///
/// Pro Liste mappt der Controller / Repo dann die parsed Tokens auf konkrete
/// LINQ-Expressions (z.B. <c>q.Where(o =&gt; tokens.Any(t =&gt; EF.Functions.Like(o.OrderNumber, $"%{t}%")))</c>).
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

    /// <summary>
    /// Parses a single filter value. Returns:
    ///   - <c>Tokens</c>: comma-separated, trimmed, non-empty parts
    ///   - <c>Negate</c>: true wenn der Filter mit <c>!</c> beginnt
    ///
    /// Beispiele:
    ///   "960,886"   -&gt; Tokens=["960","886"], Negate=false  =&gt; OR-Match
    ///   "!960"      -&gt; Tokens=["960"],        Negate=true   =&gt; NICHT enthalten
    ///   "!960,886"  -&gt; Tokens=["960","886"], Negate=true   =&gt; beide NICHT enthalten (AND-Negation)
    /// </summary>
    public static (List<string> Tokens, bool Negate) Parse(string raw)
    {
        var trimmed = raw?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed)) return (new List<string>(), false);

        var negate = trimmed.StartsWith("!", StringComparison.Ordinal);
        var payload = negate ? trimmed.Substring(1) : trimmed;

        var tokens = payload
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.ToLowerInvariant())
            .ToList();

        return (tokens, negate);
    }

    /// <summary>True wenn mindestens ein Filter ein Token enthaelt.</summary>
    public static bool HasAny(IReadOnlyDictionary<string, string>? filters)
        => filters != null && filters.Any(kv => Parse(kv.Value).Tokens.Count > 0);

    /// <summary>
    /// In-Memory-Variante fuer Listen die bereits geladene Daten paginieren.
    /// Spalten-Map: <c>col-key -&gt; getter(item) -&gt; searchable string</c>.
    /// Unbekannte col-keys werden ignoriert.
    /// </summary>
    public static IEnumerable<T> Apply<T>(
        IEnumerable<T> source,
        IReadOnlyDictionary<string, string>? filters,
        IReadOnlyDictionary<string, Func<T, string?>> colMap)
    {
        if (filters == null || filters.Count == 0) return source;

        var query = source;
        foreach (var (key, raw) in filters)
        {
            if (!colMap.TryGetValue(key, out var getter)) continue;
            var (tokens, negate) = Parse(raw);
            if (tokens.Count == 0) continue;

            query = query.Where(item =>
            {
                var value = (getter(item) ?? string.Empty).ToLowerInvariant();
                var hasMatch = tokens.Any(t => value.Contains(t));
                return negate ? !hasMatch : hasMatch;
            });
        }
        return query;
    }
}
