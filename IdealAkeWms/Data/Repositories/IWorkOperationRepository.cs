using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

/// <summary>Arbeitsgang fuer den Status im FA-Baum (FA-Struktur Teil 2): nur die Schluessel, die die Zuordnung braucht.</summary>
public sealed record FaStatusWorkOperation(int Id, string OrderNumber, string SubOrderNumber, string OperationNumber, int? ProductionWorkplaceId);

/// <summary>BDE-Status je Arbeitsgang-Id: erledigt = IsFinal-Menge auf nicht stornierter Buchung (Praedikat wie das Terminal),
/// laeuft = Buchung mit EndedAt == null und nicht storniert (pausierte haben EndedAt gesetzt, fallstricke §6).</summary>
public sealed record FaBdeStatus(IReadOnlySet<int> Erledigt, IReadOnlySet<int> Laeuft);

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
    /// FA-Struktur-Arbeitsgaenge (Stufe 2, Modal): alle Arbeitsgaenge EINER HauptFA (<c>OrderNumber</c>) —
    /// bewusst derselbe Schluessel wie der Existenz-Check, damit Knopf und Modal nie auseinanderlaufen.
    /// Mit ProductionOrder + ProductionWorkplace, sortiert nach SubOrderNumber, dann Sequence.
    /// </summary>
    Task<List<WorkOperation>> GetByOrderNumberWithWorkplaceAsync(string orderNumber);

    /// <summary>FA-Struktur Teil 2: alle Arbeitsgaenge der uebergebenen HauptFA-Nummern als Projektion — EINE Abfrage je
    /// Seitenaufbau (Contains → OPENJSON), gleicher Schluessel <c>OrderNumber</c> wie der Existenz-Check.</summary>
    Task<List<FaStatusWorkOperation>> GetForFaStatusAsync(IReadOnlyCollection<string> orderNumbers);

    /// <summary>FA-Struktur Teil 2: erledigt-/laeuft-Mengen fuer die uebergebenen Arbeitsgang-Ids (zwei Statements, eine Methode).</summary>
    Task<FaBdeStatus> GetBdeStatusAsync(IReadOnlyCollection<int> workOperationIds);
}
