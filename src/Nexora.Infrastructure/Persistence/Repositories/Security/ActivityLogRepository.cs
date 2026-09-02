using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Domain.Security;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class ActivityLogRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<ActivityLog>(dbContext, userContext, clock), IActivityLogRepository
{
}
