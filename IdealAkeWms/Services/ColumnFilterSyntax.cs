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

/// <summary>Ergebnis von <see cref="ColumnFilterSyntax.Parse"/>. Ungueltige Teile sind wirkungslos (Antwort 6), stehen aber mit
/// Grund in <see cref="Invalid"/> fuer die Anzeige.</summary>
public sealed record ParsedColumnFilter(bool Negate, IReadOnlyList<ColumnFilterPart> Parts, IReadOnlyList<string> Invalid)
{
    public static readonly ParsedColumnFilter None = new(false, Array.Empty<ColumnFilterPart>(), Array.Empty<string>());
    public bool IsEmpty => Parts.Count == 0;
}

/// <summary>
/// Spaltentypen je Liste (Etappe B: alle Listen). Schluessel = View-Pfad unter <c>Views/</c> ohne <c>.cshtml</c>; Spalten ohne
/// Eintrag sind Text. Gegenstueck im Browser: <c>&lt;th data-date-filter&gt;</c> (Datum, auch Kalenderknopf) bzw.
/// <c>&lt;th data-filter-type="number"&gt;</c>. Der Drift-Waechter <c>ColumnFilterTypesDriftTests</c> vergleicht beide Seiten und
/// prueft, dass jede Server-Liste mit Typen ihr Register an <c>Apply</c>/Postfilter uebergibt — sonst filtern Browser-Hinweis und
/// Server verschieden. Getter typisierter Spalten muessen wie die Zelle rendern (Regel 6).
/// </summary>
public static class ColumnFilterTypes
{
    private const ColumnFilterType N = ColumnFilterType.Number, D = ColumnFilterType.Date;

    private static IReadOnlyDictionary<string, ColumnFilterType> L(params (string Key, ColumnFilterType Type)[] cols)
        => cols.ToDictionary(c => c.Key, c => c.Type, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, ColumnFilterType> None = L();

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, ColumnFilterType>> Lists =
        new Dictionary<string, IReadOnlyDictionary<string, ColumnFilterType>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ArticleAttributes/Index"] = L(("sort-order", N)),
            ["ArticleCategories/Index"] = L(("article-count", N)),
            ["BdeBookings/Index"] = L(("started-at", D), ("ended-at", D), ("good-qty", N), ("scrap-qty", N)),
            ["Beschichtungsauftraege/Details"] = L(("breite", N), ("hoehe", N), ("tiefe", N), ("menge", N)),
            ["Beschichtungsauftraege/Index"] = L(("start", D), ("retour", D), ("positionen", N), ("angelegtam", D)),
            ["FaAttributes/Index"] = L(("sort-order", N)),
            ["FaCompletion/Index"] = L(("quantity", N), ("production-date", D), ("spec-count", N)),
            ["FaHierarchyBeschichtung/Index"] = L(("sollmenge", N), ("breite", N), ("hoehe", N), ("tiefe", N)),
            ["FaHierarchyBeschichtung/Summiert"] = L(("breite", N), ("hoehe", N), ("tiefe", N), ("positionen", N), ("sollmenge", N)),
            ["FaHierarchyKommissionierListen/Index"] = L(("sollmenge", N)),
            ["FaHierarchyKommissionierListen/Summiert"] = L(("sollmenge", N)),
            ["FaHierarchyVormontage/Index"] = L(("sollmenge", N), ("fertigungmenge", N), ("fe-termin", D)),
            ["FaHierarchyVormontage/Summiert"] = L(("sollmenge", N), ("fertigungmenge", N), ("fe-termin", D)),
            ["FaWorklist/Index"] = L(("quantity", N), ("coating-date", D), ("bg-date", D), ("picking-date", D), ("production-date", D)),
            ["PartRequisitions/Index"] = L(("quantity", N), ("ordered-at", D)),
            ["Picking/Bom"] = L(("quantity", N)),
            ["Picking/Index"] = L(("priority", N), ("picking-date", D)),
            ["PickingLeitstand/Index"] = L(("coating-date", D), ("bg-date", D), ("picking-date", D), ("production-date", D), ("delivery-date", D)),
            ["ProductionOrders/Index"] = L(("prio", N), ("konstruktions-termin", D), ("coating-date", D), ("bg-date", D),
                ("picking-date", D), ("production-date", D), ("delivery-date", D)),
            ["ProductionWorkplaces/Index"] = L(("pre-picking-days", N), ("sage-missing", D)),
            ["Roles/Index"] = L(("user-count", N)),
            ["SageBookingQueue/Index"] = L(("id", N), ("quantity", N), ("created-at", D), ("sent-at", D), ("confirmed-at", D), ("attempts", N)),
            ["StockMovements/Index"] = L(("datetime", D)),
            ["StockOverview/Index"] = L(("reorder-level", N), ("current-stock", N)),
            ["StorageLocations/Index"] = L(("capacity", N), ("created-at", D)),
            ["SyncLog/Index"] = L(("Timestamp", D)),
            ["Tracking/ByWorkplace"] = L(("sequence", N)),
            ["Users/Index"] = L(("created-at", D)),
            ["WarehousePicking/Details"] = L(("pos", N), ("requested", N)),
            ["WarehousePicking/Index"] = L(("id", N), ("submitted", D), ("items", N)),
            ["WarehouseRequisitions/Index"] = L(("id", N), ("items", N), ("created", D), ("submitted", D)),
            ["Creditors/Details"] = L(("mindestmenge", N)), // v1.85.0 Karte „Artikel dieses Lieferanten“ (Server-Mode, QA Q-1)
            ["PurchaseOrders/Index"] = L(("liefertermin", D), ("bestellt", N), ("geliefert", N), ("offen", N), ("erfasst", N)),
            ["WorkSteps/Index"] = L(("sort-order", N)),
        };

    /// <summary>Typen einer Liste (View-Pfad wie <c>"FaWorklist/Index"</c>); unbekannte Liste = alles Text.</summary>
    public static IReadOnlyDictionary<string, ColumnFilterType> For(string list) => Lists.TryGetValue(list, out var t) ? t : None;

    /// <summary>Typ einer Spalte einer Liste; ohne Eintrag Text.</summary>
    public static ColumnFilterType Of(string list, string key) => For(list).TryGetValue(key, out var t) ? t : ColumnFilterType.Text;
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

    /// <summary>Je Teil ein Praedikat aus einer Vorlage (z. B. mit korrelierter Subquery, <see cref="Splice{T}"/>), ODER-verknuepft,
    /// Negation ueber alles. <c>null</c>, wenn kein SQL-faehiger Teil bleibt.</summary>
    public static Expression<Func<T, bool>>? Combine<T>(ParsedColumnFilter filter,
        Func<ColumnFilterPart, Expression<Func<string?, bool>>, Expression<Func<T, bool>>> perPart)
    {
        Expression<Func<T, bool>>? combined = null;
        foreach (var part in filter.Parts)
        {
            var value = ValuePredicate(part);
            if (value == null) continue;
            var p = perPart(part, value);
            combined = combined == null ? p : Expression.Lambda<Func<T, bool>>(
                Expression.OrElse(combined.Body, Replace(p.Body, p.Parameters[0], combined.Parameters[0])), combined.Parameters[0]);
        }
        if (combined == null || !filter.Negate) return combined;
        return Expression.Lambda<Func<T, bool>>(Expression.Not(combined.Body), combined.Parameters[0]);
    }

    /// <summary>Datum in SQL (nur Bewegungshistorie, <c>Timestamp</c>): Vergleich/Bereich/<c>=</c> tageweise (Antwort 3); ein Wert
    /// ohne Operator wie bisher als Zeitraum — Tag <c>01.10.2026</c>, Monat <c>10.2026</c>, Jahr <c>2026</c>; sonst traegt der Teil
    /// nichts bei. <c>null</c>, wenn kein Teil bleibt (Aufrufer: positiv → leer wie bisher, negiert → alle).</summary>
    public static Expression<Func<T, bool>>? DatePredicate<T>(ParsedColumnFilter filter, Expression<Func<T, DateTime>> selector)
    {
        var t = selector.Body;
        Expression Between(DateTime from, DateTime to) => Expression.AndAlso(
            Expression.GreaterThanOrEqual(t, Expression.Constant(from)), Expression.LessThan(t, Expression.Constant(to)));
        Expression? chain = null;
        foreach (var part in filter.Parts)
        {
            Expression? clause = null;
            if (part.Low is decimal low)
            {
                var a = ColumnFilterSyntax.KeyToDate(low);
                var b = ColumnFilterSyntax.KeyToDate(part.High ?? low);
                clause = part.Op switch
                {
                    ColumnFilterOp.Greater => Expression.GreaterThanOrEqual(t, Expression.Constant(a.AddDays(1))),
                    ColumnFilterOp.GreaterOrEqual => Expression.GreaterThanOrEqual(t, Expression.Constant(a)),
                    ColumnFilterOp.Less => Expression.LessThan(t, Expression.Constant(a)),
                    ColumnFilterOp.LessOrEqual => Expression.LessThan(t, Expression.Constant(a.AddDays(1))),
                    _ => Between(a, b.AddDays(1)) // Equals, Range
                };
            }
            else if (part.Op is ColumnFilterOp.Contains or ColumnFilterOp.StartsWith or ColumnFilterOp.Equals)
            {
                var v = part.Value.Trim();
                var de = CultureInfo.GetCultureInfo("de-AT");
                if (DateTime.TryParseExact(v, new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy" }, de, DateTimeStyles.None, out var day))
                    clause = Between(day.Date, day.Date.AddDays(1));
                else if (DateTime.TryParseExact(v, new[] { "MM.yyyy", "M.yyyy" }, de, DateTimeStyles.None, out var month))
                    clause = Between(new DateTime(month.Year, month.Month, 1), new DateTime(month.Year, month.Month, 1).AddMonths(1));
                else if (v.Length == 4 && int.TryParse(v, out var year) && year is >= 2000 and <= 2100)
                    clause = Between(new DateTime(year, 1, 1), new DateTime(year + 1, 1, 1));
            }
            if (clause != null) chain = chain == null ? clause : Expression.OrElse(chain, clause);
        }
        if (chain == null) return null;
        return Expression.Lambda<Func<T, bool>>(filter.Negate ? Expression.Not(chain) : chain, selector.Parameters[0]);
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

    /// <summary>Werte einer festen Menge (z. B. Enum mit Anzeigename), deren Text einen Teil trifft — OHNE Negation; der Aufrufer
    /// baut daraus <c>IN</c> bzw. bei <see cref="ParsedColumnFilter.Negate"/> <c>NOT IN</c>.</summary>
    public static List<T> MatchingValues<T>(IEnumerable<T> values, Func<T, string> text, ParsedColumnFilter filter)
    {
        var positive = filter with { Negate = false };
        return values.Where(v => Matches(text(v), positive, ColumnFilterType.Text)).ToList();
    }

    internal static DateTime KeyToDate(decimal key)
    {
        var k = (int)key;
        return new DateTime(k / 10000, k / 100 % 100, k % 100);
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
