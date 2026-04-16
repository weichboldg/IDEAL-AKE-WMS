using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

public class BdeDefaultWorkOperationService : IBdeDefaultWorkOperationService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAppSettingRepository _settings;

    public BdeDefaultWorkOperationService(ApplicationDbContext ctx, IAppSettingRepository settings)
    {
        _ctx = ctx;
        _settings = settings;
    }

    public async Task<int> FindOrCreateDefaultAsync(int productionOrderId, int workplaceId)
    {
        var defaultName = await _settings.GetValueAsync("BdeDefaultArbeitsgang")
            ?? throw new InvalidOperationException("BdeDefaultArbeitsgang ist nicht konfiguriert.");

        if (string.IsNullOrWhiteSpace(defaultName))
            throw new InvalidOperationException("BdeDefaultArbeitsgang ist leer.");

        // Find existing
        var existing = await _ctx.WorkOperations
            .FirstOrDefaultAsync(wo => wo.ProductionOrderId == productionOrderId && wo.Name == defaultName);

        if (existing != null)
            return existing.Id;

        // Create new
        var wo = new WorkOperation
        {
            ProductionOrderId = productionOrderId,
            OperationNumber = "01",
            Name = defaultName,
            ProductionWorkplaceId = workplaceId,
            Sequence = 1,
            IsReportable = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "BDE-AutoCreate",
            CreatedByWindows = "BDE-AutoCreate"
        };

        _ctx.WorkOperations.Add(wo);
        await _ctx.SaveChangesAsync();
        return wo.Id;
    }
}
