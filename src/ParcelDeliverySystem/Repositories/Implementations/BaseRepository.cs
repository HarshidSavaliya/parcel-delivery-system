using System.Linq.Expressions;
using MongoDB.Driver;
using ParcelDeliverySystem.Models.Common;
using ParcelDeliverySystem.Repositories.Interfaces;

namespace ParcelDeliverySystem.Repositories.Implementations;

/// <summary>
/// Generic repository base implementation for MongoDB Atlas collections.
/// Encapsulates direct MongoDB Driver collection operations with async/await.
/// </summary>
/// <typeparam name="T">MongoDB entity inheriting from BaseEntity</typeparam>
public class BaseRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _collection;

    public BaseRepository(IMongoCollection<T> collection)
    {
        _collection = collection;
    }

    public virtual async Task<T?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var filter = Builders<T>.Filter.Eq(e => e.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync()
    {
        return await _collection.Find(FilterDefinition<T>.Empty).ToListAsync();
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _collection.Find(predicate).ToListAsync();
    }

    public virtual async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate)
    {
        return await _collection.Find(predicate).FirstOrDefaultAsync();
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    public virtual async Task CreateManyAsync(IEnumerable<T> entities)
    {
        var list = entities.ToList();
        foreach (var item in list)
        {
            item.CreatedAt = DateTime.UtcNow;
        }
        if (list.Count > 0)
        {
            await _collection.InsertManyAsync(list);
        }
    }

    public virtual async Task<bool> UpdateAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);
        var result = await _collection.ReplaceOneAsync(filter, entity);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public virtual async Task<bool> DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        var filter = Builders<T>.Filter.Eq(e => e.Id, id);
        var result = await _collection.DeleteOneAsync(filter);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    public virtual async Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return await _collection.CountDocumentsAsync(FilterDefinition<T>.Empty);
        }
        return await _collection.CountDocumentsAsync(predicate);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _collection.Find(predicate).AnyAsync();
    }
}
