using System.Linq.Expressions;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IBaseRepository<TEntity>
    where TEntity : class
{
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Expression<Func<TEntity, object>>[]? includes = null,
        Expression<Func<TEntity, TEntity>>? selector = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Expression<Func<TEntity, object>>[]? includeProperties = null,
        Expression<Func<TEntity, TEntity>>? predicate = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    Task<BasePaginationEntity<TEntity>> PaginationAsync(
        int page = 0,
        int pageSize = 20,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Expression<Func<TEntity, TEntity>>? predicate = null,
        Expression<Func<TEntity, object>>[]? includeProperties = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    void Insert(TEntity entity);

    void InsertMultiple(IEnumerable<TEntity> entities);

    void Delete(TEntity entityToDelete);

    void DeleteEntities(IEnumerable<TEntity> entities);

    void Update(
        TEntity entityToUpdate,
        string[]? propertiesToUpdate = null,
        Action<TEntity>? updateAction = null);

    void UpdateEntities(IEnumerable<TEntity> entities);
}

public sealed class BasePaginationEntity<TEntity>
    where TEntity : class
{
    public int Total { get; init; }

    public int TotalPages { get; init; }

    public IReadOnlyList<TEntity> Data { get; init; } = Array.Empty<TEntity>();
}
