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
}
