using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class FaAttributeRepository : IFaAttributeRepository
{
    private readonly ApplicationDbContext _context;

    public FaAttributeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FaAttributeDefinition>> GetAllAsync()
        => await _context.FaAttributeDefinitions
            .Include(d => d.Options)
            .Include(d => d.WorkSteps)
                .ThenInclude(ws => ws.WorkStep)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .ToListAsync();

    public async Task<List<FaAttributeDefinition>> GetActiveForWorkStepsAsync(List<int> workStepIds)
        => await _context.FaAttributeDefinitions
            .Include(d => d.Options)
            .Include(d => d.WorkSteps)
            .Where(d => d.IsActive
                && _context.FaAttributeWorkSteps.Any(j =>
                    j.FaAttributeDefinitionId == d.Id && workStepIds.Contains(j.WorkStepId)))
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .ToListAsync();

    public Task<FaAttributeDefinition?> GetByIdAsync(int id)
        => _context.FaAttributeDefinitions
            .Include(d => d.Options)
            .Include(d => d.WorkSteps)
                .ThenInclude(ws => ws.WorkStep)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddDefinitionAsync(FaAttributeDefinition def)
    {
        _context.FaAttributeDefinitions.Add(def);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateDefinitionAsync(FaAttributeDefinition def)
    {
        _context.FaAttributeDefinitions.Update(def);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteDefinitionAsync(int id)
    {
        if (await _context.FaAttributeValues.AnyAsync(v => v.FaAttributeDefinitionId == id))
            return false;

        var def = await _context.FaAttributeDefinitions.FindAsync(id);
        if (def == null) return true;

        _context.FaAttributeDefinitions.Remove(def);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task AddOptionAsync(FaAttributeOption option)
    {
        _context.FaAttributeOptions.Add(option);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateOptionAsync(FaAttributeOption option)
    {
        _context.FaAttributeOptions.Update(option);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteOptionAsync(int id)
    {
        if (await _context.FaAttributeValues.AnyAsync(v => v.SelectedOptionId == id))
            return false;

        var option = await _context.FaAttributeOptions.FindAsync(id);
        if (option == null) return true;

        _context.FaAttributeOptions.Remove(option);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task SetWorkStepsAsync(int definitionId, List<int> workStepIds)
    {
        var existing = await _context.FaAttributeWorkSteps
            .Where(j => j.FaAttributeDefinitionId == definitionId)
            .ToListAsync();

        var toRemove = existing.Where(j => !workStepIds.Contains(j.WorkStepId)).ToList();
        var existingIds = existing.Select(j => j.WorkStepId).ToHashSet();
        var toAdd = workStepIds.Distinct().Where(id => !existingIds.Contains(id))
            .Select(id => new FaAttributeWorkStep { FaAttributeDefinitionId = definitionId, WorkStepId = id })
            .ToList();

        _context.FaAttributeWorkSteps.RemoveRange(toRemove);
        _context.FaAttributeWorkSteps.AddRange(toAdd);
        await _context.SaveChangesAsync();
    }

    public async Task<List<FaAttributeValue>> GetValuesByProductionOrderIdAsync(int productionOrderId)
        => await _context.FaAttributeValues
            .Include(v => v.Definition)
            .Include(v => v.SelectedOption)
            .Where(v => v.ProductionOrderId == productionOrderId)
            .ToListAsync();

    public async Task UpsertValueAsync(int productionOrderId, int definitionId, int? selectedOptionId, bool? booleanValue,
        string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.FaAttributeValues
            .FirstOrDefaultAsync(v => v.ProductionOrderId == productionOrderId && v.FaAttributeDefinitionId == definitionId);

        if (selectedOptionId == null && booleanValue == null)
        {
            // "leer" -> Wert-Zeile loeschen
            if (row != null)
            {
                _context.FaAttributeValues.Remove(row);
                await _context.SaveChangesAsync();
            }
            return;
        }

        if (row == null)
        {
            row = new FaAttributeValue
            {
                ProductionOrderId = productionOrderId,
                FaAttributeDefinitionId = definitionId,
                SelectedOptionId = selectedOptionId,
                BooleanValue = booleanValue,
                CreatedAt = DateTime.Now,
                CreatedBy = modifiedBy,
                CreatedByWindows = modifiedByWindows
            };
            _context.FaAttributeValues.Add(row);
        }
        else
        {
            row.SelectedOptionId = selectedOptionId;
            row.BooleanValue = booleanValue;
            row.ModifiedAt = DateTime.Now;
            row.ModifiedBy = modifiedBy;
            row.ModifiedByWindows = modifiedByWindows;
        }

        await _context.SaveChangesAsync();
    }
}
