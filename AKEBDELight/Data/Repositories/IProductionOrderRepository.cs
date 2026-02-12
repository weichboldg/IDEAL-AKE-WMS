using AKEBDELight.Models;

namespace AKEBDELight.Data.Repositories;

public interface IProductionOrderRepository : IRepository<ProductionOrder>
{
    Task<List<ProductionOrder>> GetAllOrderedAsync();
    Task<List<ProductionOrder>> GetOpenOrdersAsync();
    Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber);
}
