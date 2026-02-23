using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IHolidayRepository : IRepository<Holiday>
{
    Task<List<Holiday>> GetAllOrderedAsync();
    Task<HashSet<DateTime>> GetHolidayDatesAsync();
}
