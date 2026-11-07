using System.Linq.Expressions;
using IdealAkeWms.Services;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderRepository : Repository<ProductionOrder>, IProductionOrderRepository
{
    public ProductionOrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> HierarchicalDataExistsAsync()
        // EINE Sub-FA-Definition (Spec 2026-09-25): ProductionOrder.IsHauptFa. Leere Tabelle -> All = true -> false.
        => !await _dbSet
            .AsNoTracking()
            .AllAsync(ProductionOrder.IsHauptFa);

    public async Task<List<ProductionOrder>> GetAllOrderedAsync()
    {
        return await _dbSet
            .Include(o => o.ProductionWorkplace)
            .Include(o => o.PickingStatus)
            .Include(o => o.ExtraInfo)
            .OrderBy(o => o.OrderNumber)
            .ToListAsync();
    }

    public async Task<LeitstandOrderPage> GetForLeitstandAsync(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone,
        int page,
        int pageSize,
        IReadOnlyDictionary<string, string>? columnFilters = null)
    {
        var q = BuildLeitstandQuery(filterOrderNumber, filterArticleNumber, filterCustomer,
            showDone, columnFilters, hierarchical: false);

        var totalCount = await q.CountAsync();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = int.MaxValue;
        var skip = (page - 1) * pageSize;

        var rows = await ProjectLeitstandRows(
            q.OrderBy(o => o.OrderNumber).Skip(skip).Take(pageSize)).ToListAsync();

        return new LeitstandOrderPage(rows, totalCount);
    }

    public async Task<LeitstandOrderGroupedPage> GetForLeitstandGroupedAsync(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone,
        int page,
        int pageSize,
        IReadOnlyDictionary<string, string>? columnFilters = null)
    {
        // Dieselben Filter (Zeilen-Ebene, Z3b), aber order-number matcht SubOrderNumber ODER OrderNumber.
        var q = BuildLeitstandQuery(filterOrderNumber, filterArticleNumber, filterCustomer,
            showDone, columnFilters, hierarchical: true);

        var totalRowCount = await q.CountAsync();

        // Paginierung ueber GRUPPEN: distinkte OrderNumber, geordnet. Eine Gruppe wird nie geteilt.
        var groupKeysQuery = q.Select(o => o.OrderNumber).Distinct().OrderBy(k => k);
        var totalGroupCount = await groupKeysQuery.CountAsync();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = int.MaxValue;
        var pageKeys = await groupKeysQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        if (pageKeys.Count == 0)
            return new LeitstandOrderGroupedPage(new List<LeitstandOrderGroup>(), totalGroupCount, totalRowCount);

        // Alle passenden Zeilen der Seiten-Gruppen, je Gruppe SubOrderNumber aufsteigend (Z2).
        var rows = await ProjectLeitstandRows(
            q.Where(o => pageKeys.Contains(o.OrderNumber))
             .OrderBy(o => o.OrderNumber).ThenBy(o => o.SubOrderNumber)).ToListAsync();

        // GroupBy erhaelt die Quell-Reihenfolge (nach OrderNumber sortiert) -> Gruppen in Schluessel-Ordnung.
        var groups = rows
            .GroupBy(r => r.OrderNumber)
            .Select(g => new LeitstandOrderGroup(g.Key, g.ToList()))
            .ToList();

        return new LeitstandOrderGroupedPage(groups, totalGroupCount, totalRowCount);
    }

    // Gemeinsamer Filter-Aufbau fuer flache und gruppierte Leitstand-/FA-Liste. hierarchical=true laesst
    // die order-number-Suche zusaetzlich SubOrderNumber treffen (Z3b/§4: SubOrderNumber ODER OrderNumber).
    // internal (statt private): Fixrunde 1/Task 7, Punkt (c) — ein Test baut hierueber eine Query gegen
    // einen SqlServer-Provider (nie verbunden) und prueft per ToQueryString() die tatsaechliche
    // SQL-Uebersetzung, ohne die Methode oeffentlich zu machen. Siehe InternalsVisibleTo in der csproj.
    internal IQueryable<ProductionOrder> BuildLeitstandQuery(
        string? filterOrderNumber, string? filterArticleNumber, string? filterCustomer,
        bool showDone, IReadOnlyDictionary<string, string>? columnFilters, bool hierarchical)
    {
        IQueryable<ProductionOrder> q = _dbSet.AsNoTracking();

        // Filterkarten-Freitext (Antwort 8 Suchsyntax-Spec, v1.58.0): weiter „enthaelt“ ohne Syntax, aber string.Contains statt
        // EF.Functions.Like — EF maskiert %/_/[ im Parameter, „A_1“ trifft nicht mehr „AX1“.
        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
            q = hierarchical
                ? q.Where(o => o.OrderNumber.Contains(filterOrderNumber)
                            || (o.SubOrderNumber != null && o.SubOrderNumber.Contains(filterOrderNumber)))
                : q.Where(o => o.OrderNumber.Contains(filterOrderNumber));

        if (!string.IsNullOrWhiteSpace(filterArticleNumber))
            q = q.Where(o => o.ArticleNumber != null && o.ArticleNumber.Contains(filterArticleNumber));

        if (!string.IsNullOrWhiteSpace(filterCustomer))
        {
            if (hierarchical)
            {
                // K1 (Task 7): bei IDEAL haengt der Kunde am HauptFA, ProductionOrder.Customer ist je
                // Sub-FA leer (FaHierarchyOrderInfo.Kunde, verknuepft ueber HauptFA == OrderNumber).
                // Subquery statt Join, damit die Zeilenmenge nicht aufgeblaeht wird (Kombigeraete
                // haben mehrere Kopfzeilen je HauptFA) und die Gruppen-Paginierung nicht verfaelscht.
                // i.HauptFA.ToString() == o.OrderNumber bleibt bewusst so (Fixrunde 1, Punkt e):
                // erzeugt im SQL-Server-Provider ein CONVERT im EXISTS und ist damit nicht
                // indexnutzbar, aber eine vorab ermittelte HauptFA-Schluesselliste (IN-Liste) wiegt
                // den Zusatzaufwand nicht auf — dasselbe Muster steht identisch im Spaltenfilter
                // (BuildCustomerTokenPredicate).
                q = q.Where(o =>
                    (o.Customer != null && o.Customer.Contains(filterCustomer)) ||
                    _context.FaHierarchyOrderInfos.Any(i =>
                        i.Kunde != null && i.Kunde.Contains(filterCustomer) &&
                        i.HauptFA.ToString() == o.OrderNumber));
            }
            else
            {
                q = q.Where(o => o.Customer != null && o.Customer.Contains(filterCustomer));
            }
        }

        if (!showDone)
            q = q.Where(o => !o.IsDone && !o.IsCancelled && (o.PickingStatus == null || !o.PickingStatus.IsDonePicking));

        if (columnFilters != null)
        {
            foreach (var (key, raw) in columnFilters)
            {
                // Alle SQL-Spalten dieser Liste sind Text (Datum/Zahl filtert der Controller in C#, ColumnFilterTypes "ProductionOrders/Index").
                var parsed = ColumnFilterSyntax.Parse(raw, ColumnFilterType.Text);
                if (parsed.IsEmpty) continue;
                q = ApplyLeitstandColumnFilter(q, key, parsed, hierarchical);
            }
        }

        return q;
    }

    // Matchcode (Task 2, Artikelstamm): NICHT mehr aus o.Matchcode projiziert — die Spalte wird in
    // Task 3 entfernt. LeitstandOrderRow.Matchcode bleibt hier bewusst auf dem Record-Default
    // (null); der Aufrufer (Controller) fuellt ihn nach der (paginierten) Materialisierung aus einem
    // Batch-Lookup gegen Article (IArticleRepository), siehe MatchcodeLookup.BuildAsync. Kleinster
    // Diff: Record-Shape unveraendert, nur das letzte Konstruktor-Argument entfaellt (Default greift).
    private static IQueryable<LeitstandOrderRow> ProjectLeitstandRows(IQueryable<ProductionOrder> q) =>
        q.Select(o => new LeitstandOrderRow(
            o.Id,
            o.OrderNumber,
            o.Quantity,
            o.Customer,
            o.ArticleNumber,
            o.Description1,
            o.Description2,
            o.ProductionDate,
            o.DeliveryDate,
            o.IsDone,
            o.PickingStatus != null && o.PickingStatus.IsDonePicking,
            o.IsCancelled,
            o.ProductionWorkplace != null ? o.ProductionWorkplace.Name : null,
            o.ExtraInfo != null ? o.ExtraInfo.Kaeltemittel : null,
            o.ExtraInfo != null ? o.ExtraInfo.Ventil : null,
            o.ExtraInfo != null ? o.ExtraInfo.AusfuehrungEZ : null,
            o.ExtraInfo != null ? o.ExtraInfo.Maschine : null,
            o.ExtraInfo != null ? o.ExtraInfo.SageStatus : null,
            o.ProductionWorkplace != null ? o.ProductionWorkplace.OverridePrePickingDays : null,
            o.SubOrderNumber,
            o.ParentSubOrderNumber,
            o.SageMissingSince));

    public async Task<List<ProductionOrder>> GetOpenOrdersAsync()
    {
        return await _dbSet.Where(o => !o.IsDone && !o.IsCancelled).OrderBy(o => o.OrderNumber).ToListAsync();
    }

    public async Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<int> CountByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet.CountAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<List<ProductionOrder>> SearchAsync(string? query, int limit = 20)
    {
        var q = _dbSet.Where(o => !o.IsDone && !o.IsCancelled);

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(o =>
                o.OrderNumber.Contains(query) ||
                (o.ArticleNumber != null && o.ArticleNumber.Contains(query)) ||
                (o.Customer != null && o.Customer.Contains(query)));
        }

        // F-C (Post-QA-Review 2026-09-25): Picking/IndexDropdown -> Bom.cshtml hat einen Sub-FA-Guard;
        // ohne diesen Filter fuellen Sub-FAs (gleiche OrderNumber) das Limit und bouncen dort immer ab.
        return await q
            .Where(ProductionOrder.IsHauptFa)
            .OrderBy(o => o.ProductionDate.HasValue ? 0 : 1)
            .ThenBy(o => o.ProductionDate)
            .Take(limit).ToListAsync();
    }

    public async Task<FaAufloesung> FindForBookingAsync(string? text)
    {
        var t = text?.Trim();
        if (string.IsNullOrEmpty(t)) return new(FaAufloesungStatus.Leer, null);
        if (t.Contains(';') || t.Contains(',')) return new(FaAufloesungStatus.Liste, null);
        var sub = await _dbSet.AsNoTracking().Where(o => o.SubOrderNumber == t).Take(2).ToListAsync();
        if (sub.Count == 1) return new(FaAufloesungStatus.Gefunden, sub[0]);
        var haupt = await _dbSet.AsNoTracking().Where(o => o.OrderNumber == t).Where(ProductionOrder.IsHauptFa).Take(2).ToListAsync();
        return haupt.Count switch
        {
            0 => new(FaAufloesungStatus.Unbekannt, null),
            1 => new(FaAufloesungStatus.Gefunden, haupt[0]),
            _ => new(FaAufloesungStatus.Mehrdeutig, null),
        };
    }

    public async Task<List<ProductionOrder>> GetOpenOrdersInWindowAsync(int weeksAhead, int maxCount)
    {
        if (weeksAhead <= 0) weeksAhead = 8;
        if (maxCount <= 0) maxCount = 200;

        var cutoff = DateTime.Now.AddDays(weeksAhead * 7);

        return await _dbSet
            .Where(po => !po.IsDone
                         && !po.IsCancelled
                         && !(po.PickingStatus != null && po.PickingStatus.IsDonePicking)
                         && po.ProductionDate != null
                         && po.ProductionDate <= cutoff)
            .OrderBy(po => po.ProductionDate)
            .Take(maxCount)
            .ToListAsync();
    }

    public async Task<List<ProductionOrder>> GetByArticleNumbersAsync(List<string> articleNumbers)
    {
        if (articleNumbers == null || articleNumbers.Count == 0)
            return new List<ProductionOrder>();

        return await _dbSet
            .AsNoTracking()
            .Include(o => o.PickingStatus)
            .Where(o => o.ArticleNumber != null && articleNumbers.Contains(o.ArticleNumber))
            .OrderBy(o => o.ProductionDate)
            .ToListAsync();
    }

    public async Task<List<ProductionOrder>> GetBySubOrderNumbersAsync(IReadOnlyCollection<string> subOrderNumbers)
    {
        if (subOrderNumbers == null || subOrderNumbers.Count == 0)
            return new List<ProductionOrder>();

        var set = subOrderNumbers.ToList();
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.PickingStatus)
            .Where(o => o.SubOrderNumber != null && set.Contains(o.SubOrderNumber))
            .ToListAsync();
    }

    /// <summary>
    /// Maps a <c>data-col-key</c> der FA-/Leitstand-Liste auf die Spalte(n) von <see cref="ProductionOrder"/> und wendet den
    /// geparsten Filter ueber den zentralen SQL-Bauer an (<see cref="ColumnFilterSql"/>, Spaltenfilter-Suchsyntax v1.58.0):
    /// beginnt/endet/genau/enthaelt/(leer), Negation, ODER; EF maskiert Platzhalter (kein EF.Functions.Like mehr).
    /// Date-Spalten werden hier NICHT gefiltert (C#-Postfilter im Controller nach der Termin-Berechnung).
    /// Unbekannter Key → No-Op (z. B. ein evtl. noch gesendeter alter "matchcode"-SQL-Filter, siehe Controller).
    /// </summary>
    private IQueryable<ProductionOrder> ApplyLeitstandColumnFilter(
        IQueryable<ProductionOrder> q, string key, ParsedColumnFilter f, bool hierarchical = false)
    {
        var pred = key switch
        {
            // Im hierarchischen Modus zeigt die order-number-Zelle SubOrderNumber -> der Filter matcht
            // SubOrderNumber ODER OrderNumber (HauptFA bringt die ganze Gruppe, Sub-FA die Zeile).
            "order-number" => hierarchical
                ? ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.OrderNumber, o => o.SubOrderNumber)
                : ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.OrderNumber),
            // K1 (Task 7): NUR im hierarchischen Modus zusaetzlich ueber den HauptFA-Kopf (FaHierarchyOrderInfo.Kunde).
            "customer" => hierarchical
                ? BuildCustomerColumnFilterPredicate(f)
                : ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.Customer),
            "article-number" => ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.ArticleNumber),
            "description1" => ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.Description1),
            // Matchcode laeuft als C#-Postfilter (Controller, FaListComputedColumnKeys) — hier bewusst kein Case.
            "description2" => ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.Description2),
            "workbench" => ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.ProductionWorkplace!.Name),
            // Antwort 9: „Uebergeordnete Sub-FA“ war filterbar, aber ohne Mapping (fiel still auf No-Op).
            "parent-sub-order-number" => ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.ParentSubOrderNumber),
            // FA-Zusatzinfos (Sage, v1.26.0); Null-Guard auf ExtraInfo setzt der Bauer.
            "kaeltemittel" => ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.ExtraInfo!.Kaeltemittel),
            "ventil" => ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.ExtraInfo!.Ventil),
            "ausfuehrung" => ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.ExtraInfo!.AusfuehrungEZ),
            "maschine" => ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.ExtraInfo!.Maschine),
            "sage-status" => ColumnFilterSql.Predicate<ProductionOrder>(f, o => o.ExtraInfo!.SageStatus),
            _ => null
        };
        return pred == null ? q : q.Where(pred);
    }

    /// <summary>
    /// Kunde-Spaltenfilter im hierarchischen Modus (Task 7, K1/IDEAL): matcht zusaetzlich ueber den
    /// HauptFA-Kopf (<see cref="FaHierarchyOrderInfo.Kunde"/>), weil
    /// <see cref="ProductionOrder.Customer"/> je Sub-FA leer ist (Subquery, kein Join — ein
    /// HauptFA hat bei Kombigeraeten mehrere Kopfzeilen, ein Join wuerde die Gruppen-Paginierung
    /// verfaelschen). NUR fuer den hierarchischen Fall zustaendig — der Aufrufer (Case "customer"
    /// oben) schickt den flachen AKE-Fall bewusst NICHT hierher (F4, Fixrunde 1 Punkt a).
    /// Je Token ein eigenstaendiges, ganz normal geschriebenes Praedikat (inkl. der korrelierten
    /// Subquery), ueber Parameter-Rebind ODER-verknuepft — ein <c>tokens.Any(...)</c>-Lambda ueber
    /// die Token-LISTE selbst scheitert am InMemory-Provider (derselbe Fallstrick wie bei
    /// <see cref="BuildExtraInfoOrContains"/>, hier zusaetzlich bestaetigt: auch ein manuell
    /// gebautes Expression-Tree, das denselben ParameterExpression zwischen HAUPT- und
    /// Subquery-Lambda teilt, scheitert an EF Cores NavigationExpanding — deshalb
    /// Compiler-generierte Lambdas je Token statt Handbau).
    /// Negation: eine Zeile gilt nur dann als Treffer der Negativ-Anzeige (bleibt sichtbar), wenn
    /// WEDER die Zeile NOCH der Kopf passt — <c>Expression.Not(rowMatches || headMatches)</c> ist
    /// per De-Morgan aequivalent zu <c>!rowMatches &amp;&amp; !headMatches</c> und wurde per
    /// SqlServer-<c>ToQueryString()</c>-Test gegen Wegoptimierung der Null-Pruefung abgesichert
    /// (Fixrunde 1, Punkt c).
    /// </summary>
    private Expression<Func<ProductionOrder, bool>>? BuildCustomerColumnFilterPredicate(ParsedColumnFilter f)
    {
        Expression<Func<ProductionOrder, bool>>? combined = null;
        foreach (var part in f.Parts)
        {
            var value = ColumnFilterSql.ValuePredicate(part);
            if (value == null) continue;
            // (leer) trifft nur, wenn Zeile UND alle Kopfzeilen leer sind (Regel 8); sonst Zeile ODER ein Kopf.
            Expression<Func<ProductionOrder, bool>> template = part.Op == ColumnFilterOp.Empty
                ? o => ColumnFilterSql.Wert(o.Customer) &&
                       !_context.FaHierarchyOrderInfos.Any(i => i.HauptFA.ToString() == o.OrderNumber && !ColumnFilterSql.Wert(i.Kunde))
                : o => ColumnFilterSql.Wert(o.Customer) ||
                       _context.FaHierarchyOrderInfos.Any(i => ColumnFilterSql.Wert(i.Kunde) && i.HauptFA.ToString() == o.OrderNumber);
            var token = ColumnFilterSql.Splice(template, value);
            combined = combined == null ? token : OrElseProductionOrderPredicate(combined, token);
        }
        if (combined == null || !f.Negate) return combined;
        return Expression.Lambda<Func<ProductionOrder, bool>>(Expression.Not(combined.Body), combined.Parameters[0]);
    }

    /// <summary>Klassisches PredicateBuilder-Muster: verknuepft zwei separat gebaute Praedikate
    /// per ParameterExpression-Rebind mit OR zu einem einzigen Lambda mit gemeinsamem Parameter.</summary>
    private static Expression<Func<ProductionOrder, bool>> OrElseProductionOrderPredicate(
        Expression<Func<ProductionOrder, bool>> a, Expression<Func<ProductionOrder, bool>> b)
    {
        var rebound = new ParameterRebinder(b.Parameters[0], a.Parameters[0]).Visit(b.Body)!;
        return Expression.Lambda<Func<ProductionOrder, bool>>(Expression.OrElse(a.Body, rebound), a.Parameters[0]);
    }

    private sealed class ParameterRebinder : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterRebinder(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node) => node == _from ? _to : base.VisitParameter(node);
    }

    public async Task<ProductionOrderExtraInfo?> GetExtraInfoAsync(int productionOrderId)
    {
        return await _context.ProductionOrderExtraInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ProductionOrderId == productionOrderId);
    }
}
