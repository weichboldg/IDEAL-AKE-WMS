namespace IdealAkeWms.Services;

public interface IBdeDefaultWorkOperationService
{
    Task<int> FindOrCreateDefaultAsync(int productionOrderId, int workplaceId);
}
