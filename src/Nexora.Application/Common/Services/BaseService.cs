using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Abstractions;

namespace Nexora.Application.Common.Services;

public abstract class BaseService
{
    protected BaseService(IUserContext userContext, ITenantContext tenantContext, IClock clock)
    {
        UserContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        TenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    protected IUserContext UserContext { get; }

    protected ITenantContext TenantContext { get; }

    protected IClock Clock { get; }

    protected Guid CurrentUserId =>
        UserContext.UserId
        ?? throw new InvalidOperationException(CommonMessages.Infrastructure.AuthenticatedUserContextRequired);

    protected Guid CurrentOrganizationId =>
        TenantContext.OrganizationId
        ?? throw new InvalidOperationException(CommonMessages.Infrastructure.OrganizationContextRequired);

    protected DateTimeOffset UtcNow => Clock.UtcNow;
}
