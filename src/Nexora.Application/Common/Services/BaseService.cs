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
        ?? throw new InvalidOperationException("Authenticated user context is required.");

    protected Guid CurrentOrganizationId =>
        TenantContext.OrganizationId
        ?? throw new InvalidOperationException("Organization context is required.");

    protected DateTimeOffset UtcNow => Clock.UtcNow;
}
