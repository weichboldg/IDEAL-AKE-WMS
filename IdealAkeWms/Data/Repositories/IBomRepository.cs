using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Data.Repositories;

public interface IBomRepository
{
    Task<BomQueryResult> GetBomItemsAsync(string productionOrderArticleNumber);
}
