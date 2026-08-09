using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Abstractions;
using Nexora.Domain.Authorization;
using Nexora.Domain.Common.ValueObjects;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class ApiFunctionRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<ApiFunction>(dbContext, userContext, clock), IApiFunctionRepository
{
    public Task<ApiFunction?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Query().SingleOrDefaultAsync(
            function => function.Code == ApiFunctionCode.Create(code),
            cancellationToken);

    public Task AddAsync(ApiFunction function, CancellationToken cancellationToken) =>
        base.AddEntityAsync(function, cancellationToken);
}
