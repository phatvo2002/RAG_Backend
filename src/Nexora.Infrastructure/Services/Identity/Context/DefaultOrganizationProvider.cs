using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions;
using Nexora.Domain.Organizations.Enums;
using Nexora.Infrastructure.Services.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Services.Identity.Context;

internal sealed class DefaultOrganizationProvider(NexoraDbContext dbContext, IOptions<CompanyOptions> options) : IDefaultOrganizationProvider
{
    public async Task<Guid?> GetOrganizationIdAsync(CancellationToken cancellationToken)
    {
        var configuredId = options.Value.TryGetDefaultOrganizationId();
        if (configuredId.HasValue)
        {
            return configuredId;
        }

        return await dbContext.Organizations
            .IgnoreQueryFilters()
            .Where(organization => organization.Status == OrganizationStatus.Active)
            .OrderBy(organization => organization.CreatedAt)
            .Select(organization => (Guid?)organization.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
