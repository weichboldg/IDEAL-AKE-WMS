using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public interface IOseonOperationConfigRepository
{
    Task<List<OseonOperationConfig>> GetAllAsync();
    Task<OseonOperationConfig?> GetByIdAsync(int id);
    Task<OseonOperationConfig?> GetByNameAsync(string operationName);
    Task<Dictionary<string, OseonOperationConfig>> GetAllAsDictionaryAsync();
    Task<List<string>> GetUnconfiguredOperationNamesAsync();
    Task AddAsync(OseonOperationConfig config);
    Task UpdateAsync(OseonOperationConfig config);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(string operationName);
}

public class OseonOperationConfigRepository : IOseonOperationConfigRepository
{
    private readonly ApplicationDbContext _context;

    public OseonOperationConfigRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OseonOperationConfig>> GetAllAsync()
    {
        return await _context.OseonOperationConfigs
            .OrderBy(c => c.OperationName)
            .ToListAsync();
    }

    public async Task<OseonOperationConfig?> GetByIdAsync(int id)
    {
        return await _context.OseonOperationConfigs.FindAsync(id);
    }

    public async Task<OseonOperationConfig?> GetByNameAsync(string operationName)
    {
        return await _context.OseonOperationConfigs
            .FirstOrDefaultAsync(c => c.OperationName == operationName);
    }

    public async Task<Dictionary<string, OseonOperationConfig>> GetAllAsDictionaryAsync()
    {
        return await _context.OseonOperationConfigs
            .ToDictionaryAsync(c => c.OperationName, c => c);
    }

    /// <summary>
    /// Findet AG-Namen aus OSEON-Daten, die noch keine Konfiguration haben.
    /// </summary>
    public async Task<List<string>> GetUnconfiguredOperationNamesAsync()
    {
        var configuredNames = await _context.OseonOperationConfigs
            .Select(c => c.OperationName)
            .ToListAsync();

        return await _context.OseonWorkOperations
            .Select(op => op.Name)
            .Distinct()
            .Where(name => !configuredNames.Contains(name))
            .OrderBy(name => name)
            .ToListAsync();
    }

    public async Task AddAsync(OseonOperationConfig config)
    {
        _context.OseonOperationConfigs.Add(config);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OseonOperationConfig config)
    {
        _context.OseonOperationConfigs.Update(config);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var config = await _context.OseonOperationConfigs.FindAsync(id);
        if (config != null)
        {
            _context.OseonOperationConfigs.Remove(config);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string operationName)
    {
        return await _context.OseonOperationConfigs
            .AnyAsync(c => c.OperationName == operationName);
    }
}
