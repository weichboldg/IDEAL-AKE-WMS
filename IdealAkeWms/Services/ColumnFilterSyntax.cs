using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace IdealAkeWms.Services;

/// <summary>Spaltentyp eines Spaltenfilters: steuert, ob <c>,</c> ODER oder Dezimalkomma ist und ob Vergleich/Bereich gelten.</summary>
public enum ColumnFilterType { Text, Number, Date }

public enum ColumnFilterOp { Contains, StartsWith, EndsWith, Equals, Greater, GreaterOrEqual, Less, LessOrEqual, Range, Empty }

/// <summary>Ein Teil eines Filterwerts. <see cref="Value"/> kleingeschrieben/getrimmt (Text); Zahl/Datum in <see cref="Low"/>/<see cref="High"/>
/// (Datum als <c>yyyyMMdd</c>-Zahl). Bei <c>=</c> in Zahl/Datum gesetzt, wenn der Wert lesbar ist (sonst Textvergleich).</summary>
public sealed record ColumnFilterPart(ColumnFilterOp Op, string Value, decimal? Low = null, decimal? High = null);

/// <summary>Ergebnis von <see cref="ColumnFilterHelper.ParseFilter"/>. Ungueltige Teile sind wirkungslos (Antwort 6), stehen aber mit
/// Grund in <see cref="Invalid"/> fuer die Anzeige.</summary>
public sealed record ParsedColumnFilter(bool Negate, IReadOnlyList<ColumnFilterPart> Parts, IReadOnlyList<string> Invalid)
{
    public static readonly ParsedColumnFilter None = new(false, Array.Empty<ColumnFilterPart>(), Array.Empty<string>());
    public bool IsEmpty => Parts.Count == 0;
}

/// <summary>Spaltentypen je Liste (Server-Seite). Etappe A: Geruest + FA-Liste; Etappe B registriert die uebrigen Listen
/// (Spec 2026-09-23-suchsyntax-spaltenfilter-erweitern). Gegenstueck im Browser: <c>&lt;th data-date-filter&gt;</c> bzw.
/// <c>&lt;th data-filter-type="number"&gt;</c>.</summary>
public static class ColumnFilterTypes
{
    public static readonly IReadOnlyDictionary<string, ColumnFilterType> FaListe =
        new Dictionary<string, ColumnFilterType>(StringComparer.OrdinalIgnoreCase)
        {
            ["coating-date"] = ColumnFilterType.Date, ["bg-date"] = ColumnFilterType.Date,
            ["picking-date"] = ColumnFilterType.Date, ["production-date"] = ColumnFilterType.Date,
            ["delivery-date"] = ColumnFilterType.Date, ["konstruktions-termin"] = ColumnFilterType.Date,
            ["prio"] = ColumnFilterType.Number,
        };
}

/// <summary>
/// SQL-Ausdrucksbauer der Spaltenfilter-Syntax (Etappe A): baut <c>string.Contains/StartsWith/EndsWith</c> und <c>==</c> auf
/// KONSTANTEN als Ausdrucksbaum — EF maskiert <c>%</c>/<c>_</c>/<c>[</c> dabei selbst (per ToQueryString-Test belegt), kein
/// <c>EF.Functions.Like</c>, kein <c>tokens.Any(...)</c>-Lambda (beides scheitert unter EF-InMemory, fallstricke §12), kein
/// <c>ToLower()</c> auf der Spalte (Index fuer „beginnt mit“; Gross/Klein macht die CI-Kollation). Nur Text-Teile — Zahl/Datum
/// werden heute nirgends in SQL gefiltert (YAGNI).
/// </summary>
public static class ColumnFilterSql
{
    private static readonly System.Reflection.MethodInfo ContainsM = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
    private static readonly System.Reflection.MethodInfo StartsM = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
    private static readonly System.Reflection.MethodInfo EndsM = typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!;
    private static readonly System.Reflection.MethodInfo BlankM = typeof(string).GetMethod(nameof(string.IsNullOrWhiteSpace), new[] { typeof(string) })!;

    /// <summary>Platzhalter fuer <see cref="Splice{T}"/>: im Vorlagen-Lambda <c>ColumnFilterSql.Wert(x)</c> schreiben.</summary>
    public static bool Wert(string? value) => throw new InvalidOperationException("Nur als Platzhalter in Ausdrucksvorlagen.");

    /// <summary>Praedikat fuer EINEN Teil auf einen (nullable) Wert, null-sicher. <c>null</c> fuer Zahl-/Datums-Teile.</summary>
    public static Expression<Func<string?, bool>>? ValuePredicate(ColumnFilterPart part)
    {
        var s = Expression.Parameter(typeof(string), "s");
        var notNull = Expression.NotEqual(s, Expression.Constant(null, typeof(string)));
        Expression? body = part.Op switch
        {
            ColumnFilterOp.Empty => Expression.Call(BlankM, s),
            ColumnFilterOp.Contains => Expression.AndAlso(notNull, Expression.Call(s, ContainsM, Expression.Constant(part.Value))),
            ColumnFilterOp.StartsWith => Expression.AndAlso(notNull, Expression.Call(s, StartsM, Expression.Constant(part.Value))),
            ColumnFilterOp.EndsWith => Expression.AndAlso(notNull, Expression.Call(s, EndsM, Expression.Constant(part.Value))),
            ColumnFilterOp.Equals when part.Low is null => Expression.AndAlso(notNull, Expression.Equal(s, Expression.Constant(part.Value))),
            _ => null
        };
        return body == null ? null : Expression.Lambda<Func<string?, bool>>(body, s);
    }

    /// <summary>Filter auf eine oder mehrere Spalten (Regel 8: ein Teil trifft, wenn er EINE Spalte trifft; <c>(leer)</c>, wenn
    /// ALLE leer sind). <c>null</c>, wenn kein SQL-faehiger Teil bleibt (Filter wirkungslos).</summary>
    public static Expression<Func<T, bool>>? Predicate<T>(ParsedColumnFilter filter, params Expression<Func<T, string?>>[] selectors)
    {
        var param = selectors[0].Parameters[0];
        var bodies = selectors.Select(sel => Replace(sel.Body, sel.Parameters[0], param)).ToList();
        Expression? chain = null;
        foreach (var part in filter.Parts)
        {
            var pred = ValuePredicate(part);
            if (pred == null) continue;
            var empty = part.Op == ColumnFilterOp.Empty;
            var perColumn = bodies.Select(b => Guarded(b, Replace(pred.Body, pred.Parameters[0], b), empty));
            var partExpr = part.Op == ColumnFilterOp.Empty ? perColumn.Aggregate(Expression.AndAlso) : perColumn.Aggregate(Expression.OrElse);
            chain = chain == null ? partExpr : Expression.OrElse(chain, partExpr);
        }
        if (chain == null) return null;
        return Expression.Lambda<Func<T, bool>>(filter.Negate ? Expression.Not(chain) : chain, param);
    }

    /// <summary>Null-Guards fuer Zwischenglieder einer Navigationskette (<c>o.ProductionWorkplace.Name</c>,
    /// <c>o.ExtraInfo.Ventil</c>): fehlt das Zwischenglied, gilt der Wert als leer — unter EF-InMemory wie in SQL gleich.</summary>
    private static Expression Guarded(Expression selectorBody, Expression predicate, bool empty)
    {
        var guards = new List<Expression>();
        for (var e = (selectorBody as MemberExpression)?.Expression; e is MemberExpression m; e = m.Expression)
            if (!m.Type.IsValueType) guards.Add(m);
        foreach (var g in guards)
            predicate = empty
                ? Expression.OrElse(Expression.Equal(g, Expression.Constant(null, g.Type)), predicate)
                : Expression.AndAlso(Expression.NotEqual(g, Expression.Constant(null, g.Type)), predicate);
        return predicate;
    }

    /// <summary>Setzt das Teil-Praedikat an jeder Stelle <c>Wert(x)</c> der Vorlage ein (Compiler-Lambdas bleiben erhalten, z. B.
    /// korrelierte Subqueries — kein handgebauter geteilter Parameter, fallstricke/Kunde-Subquery).</summary>
    public static Expression<Func<T, bool>> Splice<T>(Expression<Func<T, bool>> template, Expression<Func<string?, bool>> valuePredicate)
        => (Expression<Func<T, bool>>)new SpliceVisitor(valuePredicate).Visit(template);

    private static Expression Replace(Expression body, ParameterExpression from, Expression to) => new ReplaceVisitor(from, to).Visit(body);

    private sealed class ReplaceVisitor(ParameterExpression from, Expression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }

    private sealed class SpliceVisitor(Expression<Func<string?, bool>> pred) : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
            => node.Method.DeclaringType == typeof(ColumnFilterSql) && node.Method.Name == nameof(Wert)
                ? Replace(pred.Body, pred.Parameters[0], Visit(node.Arguments[0]))
                : base.VisitMethodCall(node);
    }
}

/// <summary>Grammatik-Kern (Spec 2026-09-23-suchsyntax-spaltenfilter-erweitern, „Grammatik“) — JS-Spiegel
/// <c>wwwroot/js/column-filter-syntax.js</c>, beide gegen <c>IdealAkeWms.Tests/Js/column-filter-cases.json</c> geprueft.</summary>
public static class ColumnFilterSyntax
{
    private static readonly Regex CellNumber = new(@"^-?[\d.]*\d(,\d+)?", RegexOptions.Compiled);
    private static readonly Regex CellDate = new(@"^(\d{1,2})\.(\d{1,2})\.(\d{2}|\d{4})(?!\d)", RegexOptions.Compiled);

    public static ParsedColumnFilter Parse(string? raw, ColumnFilterType type)
    {
        var trimmed = raw?.Trim() ?? string.Empty;
        if (trimmed.Length == 0) return ParsedColumnFilter.None;
        var negate = trimmed.StartsWith('!');
        var payload = negate ? trimmed[1..] : trimmed;
        var separators = type == ColumnFilterType.Text ? new[] { ';', ',' } : new[] { ';' };

        var parts = new List<ColumnFilterPart>();
        var invalid = new List<string>();
        foreach (var token in payload.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var t = token.ToLowerInvariant();
            var (part, error) = ParsePart(t, type);
            if (error != null) invalid.Add($"{token}: {error}");
            else if (part != null) parts.Add(part);
        }
        return new ParsedColumnFilter(negate, parts, invalid);
    }

    private static (ColumnFilterPart? Part, string? Error) ParsePart(string t, ColumnFilterType type)
    {
        if (t == "(leer)") return (new ColumnFilterPart(ColumnFilterOp.Empty, t), null);
        foreach (var (op, kind) in new[] { (">=", ColumnFilterOp.GreaterOrEqual), ("<=", ColumnFilterOp.LessOrEqual),
                     (">", ColumnFilterOp.Greater), ("<", ColumnFilterOp.Less) })
        {
            if (!t.StartsWith(op, StringComparison.Ordinal)) continue;
            if (type == ColumnFilterType.Text) return (null, "Vergleich nur in Zahl- und Datumsspalten");
            var v = ParseValue(t[op.Length..].Trim(), type);
            return v is null ? (null, Unlesbar(type)) : (new ColumnFilterPart(kind, t, v, v), null);
        }
        if (t.StartsWith('='))
        {
            var rest = t[1..].Trim();
            if (rest.Length == 0) return (null, null);
            var v = type == ColumnFilterType.Text ? null : ParseValue(rest, type);
            return (new ColumnFilterPart(ColumnFilterOp.Equals, rest, v, v), null);
        }
        var range = t.IndexOf("...", StringComparison.Ordinal);
        if (range >= 0)
        {
            if (type == ColumnFilterType.Text) return (null, "Bereich nur in Zahl- und Datumsspalten");
            var a = ParseValue(t[..range].Trim(), type);
            var b = ParseValue(t[(range + 3)..].Trim(), type);
            if (a is null || b is null) return (null, Unlesbar(type));
            return (new ColumnFilterPart(ColumnFilterOp.Range, t, Math.Min(a.Value, b.Value), Math.Max(a.Value, b.Value)), null);
        }
        var starts = t.EndsWith('%');
        var ends = t.StartsWith('%');
        var value = t.Trim('%').Trim();
        if (value.Length == 0) return (null, null); // nur % → wirkungslos
        if (ends && !starts) return (new ColumnFilterPart(ColumnFilterOp.EndsWith, value), null);
        if (starts && !ends) return (new ColumnFilterPart(ColumnFilterOp.StartsWith, value), null);
        return (new ColumnFilterPart(ColumnFilterOp.Contains, ends ? value : t), null);
    }

    private static string Unlesbar(ColumnFilterType type)
        => type == ColumnFilterType.Date ? "Datum nicht lesbar (TT.MM.JJJJ)" : "Zahl nicht lesbar";

    /// <summary>Zahl deutsch (<c>.</c> Tausender, <c>,</c> Dezimal) bzw. Datum <c>d.M.yyyy</c>/<c>dd.MM.yy</c> → <c>yyyyMMdd</c>.</summary>
    public static decimal? ParseValue(string s, ColumnFilterType type)
    {
        if (type == ColumnFilterType.Date)
        {
            var m = CellDate.Match(s);
            if (!m.Success || m.Length != s.Length) return null;
            return DateKey(m);
        }
        if (!Regex.IsMatch(s, @"^-?[\d.]*\d(,\d+)?$")) return null;
        return decimal.Parse(s.Replace(".", "").Replace(',', '.'), CultureInfo.InvariantCulture);
    }

    /// <summary>Zahl/Datum am Anfang des angezeigten Zelltexts (Regel 6); Datum tageweise (Antwort 3).</summary>
    public static decimal? CellValue(string cell, ColumnFilterType type)
    {
        var s = cell.Trim();
        if (type == ColumnFilterType.Date) { var m = CellDate.Match(s); return m.Success ? DateKey(m) : null; }
        var n = CellNumber.Match(s);
        return n.Success ? decimal.Parse(n.Value.Replace(".", "").Replace(',', '.'), CultureInfo.InvariantCulture) : null;
    }

    private static decimal? DateKey(Match m)
    {
        var d = int.Parse(m.Groups[1].Value); var mo = int.Parse(m.Groups[2].Value); var y = int.Parse(m.Groups[3].Value);
        if (y < 100) y += 2000;
        if (mo is < 1 or > 12 || d < 1 || d > DateTime.DaysInMonth(y, mo)) return null;
        return y * 10000 + mo * 100 + d;
    }

    /// <summary>Speicher-Auswerter: <c>true</c>, wenn der angezeigte Zelltext den Filter erfuellt. Ohne gueltigen Teil wirkungslos.</summary>
    public static bool Matches(string? cell, ParsedColumnFilter filter, ColumnFilterType type)
    {
        if (filter.IsEmpty) return true;
        var text = (cell ?? string.Empty).ToLowerInvariant();
        var trimmed = text.Trim();
        var hit = filter.Parts.Any(p => p.Op switch
        {
            ColumnFilterOp.Empty => trimmed.Length == 0,
            ColumnFilterOp.Contains => text.Contains(p.Value, StringComparison.Ordinal),
            ColumnFilterOp.StartsWith => trimmed.StartsWith(p.Value, StringComparison.Ordinal),
            ColumnFilterOp.EndsWith => trimmed.EndsWith(p.Value, StringComparison.Ordinal),
            ColumnFilterOp.Equals when p.Low is decimal v => CellValue(text, type) == v,
            ColumnFilterOp.Equals => trimmed == p.Value,
            _ => CellValue(text, type) is decimal c && p.Op switch
            {
                ColumnFilterOp.Greater => c > p.Low,
                ColumnFilterOp.GreaterOrEqual => c >= p.Low,
                ColumnFilterOp.Less => c < p.Low,
                ColumnFilterOp.LessOrEqual => c <= p.Low,
                _ => c >= p.Low && c <= p.High // Range
            }
        });
        return filter.Negate ? !hit : hit;
    }
}
