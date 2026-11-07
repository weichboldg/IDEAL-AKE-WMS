using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

/// <summary>v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen): Kommissionierauftraege. Ohne Cache-Decorator (Nummernvergabe
/// braucht den aktuellen Stand, Muster <see cref="IBeschichtungsauftragRepository"/>). Einziger Schreiber: <c>PickingOrderWorkflow</c>.</summary>
public interface IPickingOrderRepository
{
    Task<int> GetMaxLaufendeNummerAsync(int jahr);

    /// <summary>Zeilen aller NICHT stornierten Auftraege des FA (Grundlage der Restmengen).</summary>
    Task<List<PickingOrderLine>> GetAktiveZeilenAsync(int productionOrderId);

    /// <summary>Lagerkennung des Hauptlagerplatzes und Artikelgruppe je Artikelnummer (Bereichsregel).</summary>
    Task<Dictionary<string, (string? Lagerort, string? Artikelgruppe)>> GetArtikelBereichsdatenAsync(IReadOnlyCollection<string> articleNumbers);

    /// <summary>Auftraege eines FA (alle Status) mit Bereich, neueste zuletzt.</summary>
    Task<List<PickingOrder>> GetByProductionOrderAsync(int productionOrderId);
    Task<Dictionary<int, List<PickingOrder>>> GetByProductionOrdersAsync(IReadOnlyCollection<int> productionOrderIds);

    /// <summary>Auftragsliste: Auftraege mit FA (inkl. Werkbank, Status) und Bereich.</summary>
    Task<List<PickingOrder>> GetListeAsync(bool nurOffene);

    Task<PickingOrder?> GetMitZeilenAsync(int id);

    void Add(PickingOrder order);
    Task SaveChangesAsync();
    void VerwerfeAenderungen();
    Task<T> InSerializableTransactionAsync<T>(Func<Task<T>> action);
}
