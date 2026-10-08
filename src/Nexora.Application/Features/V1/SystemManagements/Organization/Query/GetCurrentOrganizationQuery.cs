using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Organization.Model;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.Organization.Query;

public sealed record GetCurrentOrganizationQuery : IQuery<Result<OrganizationResponse>>;

public sealed class GetCurrentOrganizationQueryHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider)
    : IRequestHandler<GetCurrentOrganizationQuery, Result<OrganizationResponse>>
{
    public async Task<Result<OrganizationResponse>> Handle(GetCurrentOrganizationQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var organization = organizationId.HasValue
            ? await unitOfWork.OrganizationRepository.GetByIdAsync(organizationId.Value, cancellationToken)
            : null;
        return organization is null || organization.Status != OrganizationStatus.Active
            ? Result<OrganizationResponse>.Failure(Error.NotFound(CommonMessages.Organization.NotFound))
            : Result<OrganizationResponse>.Success(new OrganizationResponse(organization.Id, organization.Slug.Value, organization.Name, organization.Status.ToString(), organization.DataResidency));
    }
}
