using System.Linq.Expressions;
using AKEBDELight.Models;

namespace AKEBDELight.Data.Repositories;

public interface IRepository<T> where T : AuditableEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}
