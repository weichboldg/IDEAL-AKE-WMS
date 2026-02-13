using AKEBDELight.Models.ViewModels;

namespace AKEBDELight.Data.Repositories;

public interface IBomRepository
{
    Task<List<BomItem>> GetBomItemsAsync(string productionOrderArticleNumber);
}
