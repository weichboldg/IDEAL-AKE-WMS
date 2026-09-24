using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IWorkOperationRepository : IRepository<WorkOperation>
{
    Task<List<WorkOperation>> GetByProductionOrderIdAsync(int productionOrderId);
    Task<List<WorkOperation>> GetByProductionOrderIdWithWorkplaceAsync(int productionOrderId);
    Task<List<WorkOperation>> GetAllWithOrderAndWorkplaceAsync();
    Task<List<WorkOperation>> GetByWorkplaceIdAsync(int workplaceId);
    /// <summary>
    /// excludePackedOrders (Fold 2, Spec §10.6): true filtert FAs mit Sage-Status
    /// verpackt/abgeholt aus (NUR der BDE-Aufrufer). Default false erhaelt das
    /// Tracking-Verhalten (TrackingController.ByWorkplace bleibt ungefiltert).
    /// </summary>
    Task<List<WorkOperation>> GetOpenByWorkplaceIdAsync(int workplaceId, bool excludePackedOrders = false);
    Task<WorkOperation?> GetByFaAndOperationAsync(string faNumber, string operationNumber);

    /// <summary>
    /// Mengenwertige Variante (Teil 7 Etappe D, AK 10): liefert ALLE Arbeitsgänge zu FA+Operation.
    /// Im hierarchischen Modus ist <c>OrderNumber</c> nicht mehr eindeutig — der Einzel-Lookup
    /// <see cref="GetByFaAndOperationAsync"/> kann dann mehrdeutig sein. Teil 7 liefert nur diese
    /// Naht + protokolliert Mehrfachtreffer; die Umstellung der Aufrufer ist Teil 8.
    /// </summary>
    Task<List<WorkOperation>> GetAllByFaAndOperationAsync(string faNumber, string operationNumber);

    /// <summary>
    /// FA-Struktur-Arbeitsgaenge (Stufe 1, Seitenaufbau): welche der uebergebenen HauptFA-Nummern
    /// (<c>ProductionOrder.OrderNumber</c>) haben mindestens einen Arbeitsgang? Nur Schluessel, EINE Abfrage.
    /// </summary>
    Task<HashSet<string>> GetOrderNumbersWithWorkOperationsAsync(IReadOnlyCollection<string> orderNumbers);

    /// <summary>
    /// FA-Struktur-Arbeitsgaenge (Stufe 2, Modal): alle Arbeitsgaenge zu den Sub-FA-Nummern EINER Struktur,
    /// mit ProductionOrder + ProductionWorkplace, sortiert nach SubOrderNumber, dann Sequence.
    /// </summary>
    Task<List<WorkOperation>> GetBySubOrderNumbersWithWorkplaceAsync(IReadOnlyCollection<string> subOrderNumbers);
}
