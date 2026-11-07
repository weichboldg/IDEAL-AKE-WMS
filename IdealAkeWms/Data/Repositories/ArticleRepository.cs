using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ArticleRepository : Repository<Article>, IArticleRepository
{
    public ArticleRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Article>> GetAllOrderedAsync()
    {
        return await _dbSet.OrderBy(a => a.ArticleNumber).ToListAsync();
    }

    public async Task<List<Article>> SearchAsync(string? query, int limit = 50)
    {
        var q = _dbSet.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(a => a.ArticleNumber.Contains(query) ||
                             (a.Description != null && a.Description.Contains(query)) ||
                             (a.Matchcode != null && a.Matchcode.Contains(query)));
        }
        return await q.OrderBy(a => a.ArticleNumber).Take(limit).ToListAsync();
    }

    public async Task<ArticleSageDetail?> GetSageDetailAsync(int articleId) =>
        await _context.ArticleSageDetails.AsNoTracking().FirstOrDefaultAsync(d => d.ArticleId == articleId);

    public async Task<List<ArticleSupplierRow>> GetSuppliersAsync(int articleId) =>
        await (from s in _context.ArticleSuppliers.AsNoTracking()
               join c in _context.Creditors.AsNoTracking() on s.CreditorId equals c.Id
               where s.ArticleId == articleId
               orderby s.IstHauptlieferant descending, c.Name
               select new ArticleSupplierRow(c.Id, c.Name, c.SageKto, c.IsActive, s.SageBestellnummer, s.Mindestbestellmenge,
                   s.Einkaufsmengeneinheit, s.Wiederbeschaffungszeit, s.IstHauptlieferant))
              .ToListAsync();

    public async Task<Article?> GetByArticleNumberAsync(string articleNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.ArticleNumber == articleNumber);
    }

    public async Task<(List<Article> Items, int TotalCount)> GetPaginatedAsync(
        int page, int pageSize, string? search,
        IReadOnlyDictionary<string, string>? columnFilters = null)
    {
        var query = BuildArticleQuery(search, columnFilters);

        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(a => a.ArticleNumber)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();
        return (items, totalCount);
    }

    // internal (statt private): erlaubt einem Test, denselben Ausdrucksbaum synchron gegen einen
    // SqlServer-Context (nie verbunden) zu bauen und per ToQueryString() die SQL-Uebersetzung zu
    // pruefen — EF.Functions.Like + patterns.Any(...) ist unter InMemory nicht ausfuehrbar (siehe
    // ProductionOrderRepository.BuildLeitstandQuery, gleiches Muster). Siehe InternalsVisibleTo in
    // der csproj.
    internal IQueryable<Article> BuildArticleQuery(string? search, IReadOnlyDictionary<string, string>? columnFilters)
    {
        var query = _dbSet.Include(a => a.ArticleCategory).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.ArticleNumber.Contains(search) ||
                                     (a.Description != null && a.Description.Contains(search)) ||
                                     (a.Matchcode != null && a.Matchcode.Contains(search)));
        }

        if (columnFilters != null)
        {
            foreach (var (key, raw) in columnFilters)
            {
                var parsed = ColumnFilterSyntax.Parse(raw, ColumnFilterType.Text);
                if (parsed.IsEmpty) continue;
                query = ApplyArticleColumnFilter(query, key, parsed);
            }
        }

        return query;
    }

    /// <summary>Matchcode je Artikelnummer für einen Stapel (Anzeige + AK-14-Zählung der FA-Listen):
    /// eine Abfrage statt N. Fehlende Artikelnummer ⇒ NICHT im Dict (⇒ Fehltreffer); vorhandener,
    /// aber leerer Matchcode ⇒ Value null. Keys case-insensitiv (ArticleNumber ist UNIQUE, aber
    /// Groß/Klein soll nicht am Treffer entscheiden).</summary>
    public async Task<Dictionary<string, string?>> GetMatchcodesByArticleNumbersAsync(IReadOnlyCollection<string> articleNumbers)
    {
        var wanted = articleNumbers.Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        if (wanted.Count == 0) return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var rows = await _dbSet.Where(a => wanted.Contains(a.ArticleNumber))
                               .Select(a => new { a.ArticleNumber, a.Matchcode })
                               .ToListAsync();
        return rows.ToDictionary(r => r.ArticleNumber, r => r.Matchcode, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<List<Article>> GetByMatchcodeExactAsync(string matchcode, int limit)
    {
        // ToUpper/Trim auf beiden Seiten: kollationsunabhaengig und unter InMemory gleich (EF: UPPER(LTRIM(RTRIM()))). Sync und
        // Handpflege trimmen den Matchcode nicht. ponytail: ohne Index ein Tabellenscan je Scan; Index erst bei gemessener Latenz.
        var key = matchcode.Trim().ToUpper();
        return await _dbSet.Where(a => a.Matchcode != null && a.Matchcode.Trim().ToUpper() == key && a.ArticleNumber != Article.DummyArticleNumber)
                           .OrderBy(a => a.ArticleNumber)
                           .Take(limit)
                           .ToListAsync();
    }

    public async Task<List<Article>> GetByEanAsync(string ean, int limit)
    {
        // Spec 2026-11-07-ean-einkaufsartikel-scan: EAN steht nur im Sage-Satelliten (SData-Artikellauf, SdataArtikelReader schreibt
        // ungetrimmt) -> Trim beidseitig. ponytail: ohne Index ein Tabellenscan je Scan (wie Matchcode); Index erst bei gemessener Latenz.
        var key = ean.Trim();
        return await _dbSet.Where(a => a.SageDetail != null && a.SageDetail.EANNummer != null && a.SageDetail.EANNummer.Trim() == key
                                       && a.ArticleNumber != Article.DummyArticleNumber)
                           .OrderBy(a => a.ArticleNumber)
                           .Take(limit)
                           .ToListAsync();
    }

    /// <summary>Artikelliste: Spaltenfilter ueber den zentralen SQL-Bauer (Spaltenfilter-Suchsyntax, Etappe B). „beginnt mit“ auf
    /// der Artikelnummer bleibt indexfaehig (<c>LIKE 'x%'</c> ohne <c>LOWER</c>, AK 12).</summary>
    internal static IQueryable<Article> ApplyArticleColumnFilter(IQueryable<Article> q, string key, ParsedColumnFilter f)
    {
        var pred = key switch
        {
            "article-number" => ColumnFilterSql.Predicate<Article>(f, a => a.ArticleNumber),
            "description" => ColumnFilterSql.Predicate<Article>(f, a => a.Description),
            "matchcode" => ColumnFilterSql.Predicate<Article>(f, a => a.Matchcode),
            "unit" => ColumnFilterSql.Predicate<Article>(f, a => a.Unit),
            "article-group" => ColumnFilterSql.Predicate<Article>(f, a => a.ArticleGroup),
            "category" => ColumnFilterSql.Predicate<Article>(f, a => a.ArticleCategory!.Name),
            _ => null
        };
        return pred == null ? q : q.Where(pred);
    }
}
