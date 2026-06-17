using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionWorkplaceRepository : IRepository<ProductionWorkplace>
{
    Task<List<ProductionWorkplace>> GetAllOrderedAsync();
    Task<ProductionWorkplace?> GetByIdWithUsersAsync(int id);
    Task<List<ProductionWorkplace>> GetAllWithUsersOrderedAsync();
    Task<List<ProductionWorkplace>> GetBdeActiveAsync();
    Task<List<ProductionWorkplace>> GetByUserIdAsync(int userId);
    Task SetProductionWorkplaceUsersAsync(int workplaceId, List<int> userIds, string createdBy, string createdByWindows);

    /// <summary>WorkStep-Ids (Arbeitsgaenge), die der Werkbank zugeordnet sind.</summary>
    Task<List<int>> GetWorkStepIdsAsync(int workplaceId);

    /// <summary>Delta-Sync der Werkbank↔Arbeitsgang-Junction: fehlende adden, ueberzaehlige entfernen.</summary>
    Task SetWorkStepsAsync(int workplaceId, List<int> workStepIds, string createdBy = "system", string createdByWindows = "system");
}
