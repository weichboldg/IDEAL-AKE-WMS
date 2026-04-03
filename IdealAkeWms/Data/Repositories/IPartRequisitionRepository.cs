using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IPartRequisitionRepository
{
    Task<PartRequisition?> GetByIdAsync(int id);
    Task AddAsync(PartRequisition requisition);
    Task AddRangeAsync(IEnumerable<PartRequisition> requisitions);
    Task UpdateAsync(PartRequisition requisition);

    Task<List<PartRequisition>> GetByProductionOrderAsync(int productionOrderId);
    Task<List<PartRequisition>> GetOpenByArticleNumberAsync(string articleNumber);
    Task<bool> HasOpenRequisitionAsync(int productionOrderId, string articleNumber);

    Task<(List<PartRequisition> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, bool showAll = false, string? searchTerm = null);

    Task<List<PartRequisition>> GetUnsentAsync();

    Task FulfillAsync(int requisitionId, int stockMovementId, string modifiedBy, string modifiedByWindows);
    Task CancelAsync(int requisitionId, string cancelledBy, string modifiedBy, string modifiedByWindows);
}
