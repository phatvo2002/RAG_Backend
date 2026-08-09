using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Domain.Common.Entities;

namespace Nexora.Infrastructure.Persistence.Repositories;

public class BaseRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : class
{
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public BaseRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Entities = dbContext.Set<TEntity>();
    }

    protected NexoraDbContext DbContext { get; }

    protected DbSet<TEntity> Entities { get; }

    protected IQueryable<TEntity> Query(bool asTracking = false) =>
        asTracking ? Entities : Entities.AsNoTracking();

    protected async Task AddEntityAsync(TEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Entities.AddAsync(entity, cancellationToken);
    }

    public async Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Expression<Func<TEntity, object>>[]? includes = null,
        Expression<Func<TEntity, TEntity>>? selector = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter, includes, selector, asNoTracking);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Expression<Func<TEntity, object>>[]? includeProperties = null,
        Expression<Func<TEntity, TEntity>>? predicate = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter, includeProperties, predicate, asNoTracking);
        return orderBy is null
            ? await query.ToListAsync(cancellationToken)
            : await orderBy(query).ToListAsync(cancellationToken);
    }

    public async Task<BasePaginationEntity<TEntity>> PaginationAsync(
        int page = 0,
        int pageSize = 20,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Expression<Func<TEntity, TEntity>>? predicate = null,
        Expression<Func<TEntity, object>>[]? includeProperties = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        if (pageSize is < 1 or > 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 1000.");
        }

        var normalizedPage = page < 1 ? 1 : page;
        var query = BuildQuery(filter, includeProperties, predicate, asNoTracking);
        var total = await query.CountAsync(cancellationToken);
        var orderedQuery = orderBy is null ? query : orderBy(query);
        var data = await orderedQuery
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new BasePaginationEntity<TEntity>
        {
            Total = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Data = data
        };
    }

    public void Insert(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Add(entity);
    }

    public void InsertMultiple(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Entities.AddRange(entities);
    }

    public void Delete(TEntity entityToDelete)
    {
        ArgumentNullException.ThrowIfNull(entityToDelete);

        if (DbContext.Entry(entityToDelete).State == EntityState.Detached)
        {
            Entities.Attach(entityToDelete);
        }

        if (entityToDelete is SoftDeletableEntityBase softDeletable)
        {
            softDeletable.MarkDeleted(GetCurrentUserId(), _clock.UtcNow);
            return;
        }

        if (entityToDelete is CompositeSoftDeletableEntityBase compositeSoftDeletable)
        {
            compositeSoftDeletable.MarkDeleted(GetCurrentUserId(), _clock.UtcNow);
            return;
        }

        if (entityToDelete is UserProfileEntityBase userProfile)
        {
            userProfile.MarkDeleted(GetCurrentUserId(), _clock.UtcNow);
            return;
        }

        Entities.Remove(entityToDelete);
    }

    public void DeleteEntities(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (var entity in entities)
        {
            Delete(entity);
        }
    }

    public void Update(TEntity entityToUpdate, string[]? propertiesToUpdate = null, Action<TEntity>? updateAction = null)
    {
        ArgumentNullException.ThrowIfNull(entityToUpdate);

        updateAction?.Invoke(entityToUpdate);

        if (DbContext.Entry(entityToUpdate).State == EntityState.Detached)
        {
            Entities.Attach(entityToUpdate);
        }

        if (propertiesToUpdate is { Length: > 0 })
        {
            foreach (var propertyName in propertiesToUpdate)
            {
                if (DbContext.Entry(entityToUpdate).Metadata.FindProperty(propertyName) is null)
                {
                    throw new ArgumentException(
                        $"Property '{propertyName}' is not a mapped scalar property of {typeof(TEntity).Name}.",
                        nameof(propertiesToUpdate));
                }

                DbContext.Entry(entityToUpdate).Property(propertyName).IsModified = true;
            }
        }
        else
        {
            DbContext.Entry(entityToUpdate).State = EntityState.Modified;
        }
    }

    public void UpdateEntities(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Entities.UpdateRange(entities);
    }

    private IQueryable<TEntity> BuildQuery(
        Expression<Func<TEntity, bool>>? filter,
        Expression<Func<TEntity, object>>[]? includes,
        Expression<Func<TEntity, TEntity>>? selector,
        bool asNoTracking)
    {
        IQueryable<TEntity> query = asNoTracking ? Entities.AsNoTracking() : Entities;

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        if (includes is not null)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }

        if (selector is not null)
        {
            query = query.Select(selector);
        }

        return query;
    }

    private Guid GetCurrentUserId() =>
        _userContext.UserId
        ?? throw new InvalidOperationException("An authenticated user is required for soft delete.");
}
