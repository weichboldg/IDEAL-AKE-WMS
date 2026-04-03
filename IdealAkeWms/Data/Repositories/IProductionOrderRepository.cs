using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderRepository : IRepository<ProductionOrder>
{
    Task<List<ProductionOrder>> GetAllOrderedAsync();
    Task<List<ProductionOrder>> GetOpenOrdersAsync();
    Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber);
    Task<List<ProductionOrder>> SearchAsync(string? query, int limit = 20);
    Task<List<ProductionOrder>> GetReleasedForPickingAsync();
    Task<int> GetReleasedForPickingCountAsync();
}
