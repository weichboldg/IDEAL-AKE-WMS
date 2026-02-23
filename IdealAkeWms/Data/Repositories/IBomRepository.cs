using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Data.Repositories;

public interface IBomRepository
{
    Task<List<BomItem>> GetBomItemsAsync(string productionOrderArticleNumber);
}
