using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Nexora.Application.Common.Abstractions;

namespace Nexora.Infrastructure.Context;

internal sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid? OrganizationId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue("organizationId");
            return Guid.TryParse(value, out var organizationId) ? organizationId : null;
        }
    }
}
