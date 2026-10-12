using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public interface IOperationConfigRepository
{
    Task<List<OperationConfig>> GetAllAsync();
    Task<OperationConfig?> GetByIdAsync(int id);
    Task<OperationConfig?> GetByNameAsync(string operationName);
    Task<Dictionary<string, OperationConfig>> GetAllAsDictionaryAsync();
    Task<List<string>> GetUnconfiguredOperationNamesAsync();
    Task AddAsync(OperationConfig config);
    Task UpdateAsync(OperationConfig config);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(string operationName);
}

public class OperationConfigRepository : IOperationConfigRepository
{
    private readonly ApplicationDbContext _context;

    public OperationConfigRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OperationConfig>> GetAllAsync()
    {
        return await _context.OperationConfigs
            .OrderBy(c => c.OperationName)
            .ToListAsync();
    }

    public async Task<OperationConfig?> GetByIdAsync(int id)
    {
        return await _context.OperationConfigs.FindAsync(id);
    }

    public async Task<OperationConfig?> GetByNameAsync(string operationName)
    {
        return await _context.OperationConfigs
            .FirstOrDefaultAsync(c => c.OperationName == operationName);
    }

    public async Task<Dictionary<string, OperationConfig>> GetAllAsDictionaryAsync()
    {
        return await _context.OperationConfigs
            .ToDictionaryAsync(c => c.OperationName, c => c);
    }

    /// <summary>
    /// Findet AG-Namen aus OSEON-Daten, die noch keine Konfiguration haben.
    /// </summary>
    public async Task<List<string>> GetUnconfiguredOperationNamesAsync()
    {
        var configuredNames = await _context.OperationConfigs
            .Select(c => c.OperationName)
            .ToListAsync();

        return await _context.OseonWorkOperations
            .Select(op => op.Name)
            .Distinct()
            .Where(name => !configuredNames.Contains(name))
            .OrderBy(name => name)
            .ToListAsync();
    }

    public async Task AddAsync(OperationConfig config)
    {
        _context.OperationConfigs.Add(config);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OperationConfig config)
    {
        _context.OperationConfigs.Update(config);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var config = await _context.OperationConfigs.FindAsync(id);
        if (config != null)
        {
            _context.OperationConfigs.Remove(config);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string operationName)
    {
        return await _context.OperationConfigs
            .AnyAsync(c => c.OperationName == operationName);
    }
}
