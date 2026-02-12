using AKEBDELight.Models;

namespace AKEBDELight.Data.Repositories;

public interface IHolidayRepository : IRepository<Holiday>
{
    Task<List<Holiday>> GetAllOrderedAsync();
    Task<HashSet<DateTime>> GetHolidayDatesAsync();
}
