using AKEBDELight.Models;

namespace AKEBDELight.Data.Repositories;

public interface IArticleRepository : IRepository<Article>
{
    Task<List<Article>> GetAllOrderedAsync();
}
