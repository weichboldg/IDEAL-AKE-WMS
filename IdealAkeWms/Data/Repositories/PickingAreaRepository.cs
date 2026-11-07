using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>v1.88.0: siehe <see cref="IPickingAreaRepository"/>.</summary>
public class PickingAreaRepository : IPickingAreaRepository
{
    private readonly ApplicationDbContext _db;

    public PickingAreaRepository(ApplicationDbContext db) => _db = db;

    public Task<List<PickingArea>> GetAllAsync() =>
        _db.PickingAreas.Include(a => a.Regeln).OrderBy(a => a.Reihenfolge).ThenBy(a => a.Name).ToListAsync();

    public Task<PickingArea?> GetByIdAsync(int id) => _db.PickingAreas.FirstOrDefaultAsync(a => a.Id == id);

    public Task<List<PickingAreaRule>> GetRulesAsync() =>
        _db.PickingAreaRules.Include(r => r.PickingArea).OrderBy(r => r.RuleType).ThenBy(r => r.Value).ToListAsync();

    public Task<PickingAreaRule?> GetRuleAsync(int id) => _db.PickingAreaRules.FirstOrDefaultAsync(r => r.Id == id);

    public void Add(PickingArea area) => _db.PickingAreas.Add(area);
    public void AddRule(PickingAreaRule rule) => _db.PickingAreaRules.Add(rule);
    public void RemoveRule(PickingAreaRule rule) => _db.PickingAreaRules.Remove(rule);
    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task InTransactionAsync(Func<Task> action)
    {
        if (!_db.Database.IsRelational()) { await action(); return; }
        await using var tx = await _db.Database.BeginTransactionAsync();
        await action();
        await tx.CommitAsync();
    }

    public Task<string?> GetArticleNumberAsync(int articleId) =>
        _db.Articles.Where(a => a.Id == articleId).Select(a => (string?)a.ArticleNumber).FirstOrDefaultAsync();

    public Task<bool> ArticleExistsAsync(string articleNumber) => _db.Articles.AnyAsync(a => a.ArticleNumber == articleNumber);

    public async Task<List<string>> GetKommissionierzieleAsync() => Distinct(await _db.FaHierarchyNodes
        .Where(n => n.Kommissionieren != null).Select(n => n.Kommissionieren!).Distinct().ToListAsync());

    public async Task<List<string>> GetLagerorteAsync() => Distinct(await _db.StorageAreas
        .Where(s => s.Lagerkennung != "").Select(s => s.Lagerkennung).Distinct().ToListAsync());

    public async Task<List<string>> GetArtikelgruppenAsync() => Distinct(await _db.Articles
        .Where(a => a.ArticleGroup != null).Select(a => a.ArticleGroup!).Distinct().ToListAsync());

    private static List<string> Distinct(IEnumerable<string> werte) => werte
        .Select(v => v.Trim()).Where(v => v.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();
}
