using System.Linq.Expressions;
using ParcelDeliverySystem.Models.Common;

namespace ParcelDeliverySystem.Repositories.Interfaces;

/// <summary>
/// Generic repository interface defining asynchronous CRUD operations over MongoDB collections.
/// Decouples higher service layers from MongoDB driver APIs.
/// </summary>
/// <typeparam name="T">Document entity deriving from BaseEntity</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(string id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate);
    Task<T> CreateAsync(T entity);
    Task CreateManyAsync(IEnumerable<T> entities);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(string id);
    Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}
