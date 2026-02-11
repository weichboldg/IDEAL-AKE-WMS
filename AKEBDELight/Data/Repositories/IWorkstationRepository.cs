using AKEBDELight.Models;

namespace AKEBDELight.Data.Repositories;

public interface IWorkstationRepository : IRepository<Workstation>
{
    Task<Workstation?> GetByIdWithUsersAsync(int id);
    Task<List<Workstation>> GetAllWithUsersAsync();
    Task SetWorkstationUsersAsync(int workstationId, List<int> userIds, string createdBy, string createdByWindows);
}
