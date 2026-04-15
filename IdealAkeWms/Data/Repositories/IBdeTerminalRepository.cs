using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IBdeTerminalRepository
{
    Task<List<BdeTerminal>> GetAllAsync();
    Task<BdeTerminal?> GetByIdAsync(int id);
    Task<BdeTerminal?> GetByUserIdAsync(int userId);
    Task AddAsync(BdeTerminal terminal);
    Task UpdateAsync(BdeTerminal terminal);
    Task DeleteAsync(BdeTerminal terminal);
}
