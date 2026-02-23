using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IWorkstationRepository : IRepository<Workstation>
{
    Task<Workstation?> GetByIdWithUsersAsync(int id);
    Task<List<Workstation>> GetAllWithUsersAsync();
    Task SetWorkstationUsersAsync(int workstationId, List<int> userIds, string createdBy, string createdByWindows);
}
