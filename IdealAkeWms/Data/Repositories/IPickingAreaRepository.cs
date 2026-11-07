using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

/// <summary>v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E1): Kommissionierbereiche und Zuordnungsregeln. Ungecacht
/// (Erzeugung und Pflege brauchen den aktuellen Stand).</summary>
public interface IPickingAreaRepository
{
    Task<List<PickingArea>> GetAllAsync();
    Task<PickingArea?> GetByIdAsync(int id);
    /// <summary>Alle Regeln mit geladenem Bereich (Grundlage fuer <c>KommissionierbereichRegel.Resolve</c> und die Regelliste).</summary>
    Task<List<PickingAreaRule>> GetRulesAsync();
    Task<PickingAreaRule?> GetRuleAsync(int id);
    void Add(PickingArea area);
    void AddRule(PickingAreaRule rule);
    void RemoveRule(PickingAreaRule rule);
    Task SaveChangesAsync();

    // Stammdaten-Auswahl fuer Regelwerte (ADR 0020: Wert = Auswahl aus Stammdaten, der Server prueft)
    Task<string?> GetArticleNumberAsync(int articleId);
    Task<bool> ArticleExistsAsync(string articleNumber);
    Task<List<string>> GetKommissionierzieleAsync();
    Task<List<string>> GetLagerorteAsync();
    Task<List<string>> GetArtikelgruppenAsync();
}
