using MediatR;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Organization.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Organization.Query;

public sealed record GetOrganizationsQuery : IQuery<IReadOnlyList<OrganizationResponse>>;

public sealed class GetOrganizationsQueryHandler(IOrganizationRepository organizationRepository) : IRequestHandler<GetOrganizationsQuery, IReadOnlyList<OrganizationResponse>>
{
    public async Task<IReadOnlyList<OrganizationResponse>> Handle(GetOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var organizations = await organizationRepository.GetAllForManagementAsync(cancellationToken);
        return organizations.Select(organization => new OrganizationResponse(organization.Id, organization.Slug.Value, organization.Name, organization.Status.ToString(), organization.DataResidency)).ToArray();
    }
}

public sealed record GetOrganizationQuery(Guid OrganizationId) : IQuery<Result<OrganizationResponse>>;

public sealed class GetOrganizationQueryHandler(IOrganizationRepository organizationRepository) : IRequestHandler<GetOrganizationQuery, Result<OrganizationResponse>>
{
    public async Task<Result<OrganizationResponse>> Handle(GetOrganizationQuery request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        return organization is null
            ? Result<OrganizationResponse>.Failure(Error.NotFound(CommonMessages.Organization.NotFound))
            : Result<OrganizationResponse>.Success(new OrganizationResponse(organization.Id, organization.Slug.Value, organization.Name, organization.Status.ToString(), organization.DataResidency));
    }
}
