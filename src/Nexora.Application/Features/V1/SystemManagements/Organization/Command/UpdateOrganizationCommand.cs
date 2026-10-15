using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Organization.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Organization.Command;

public sealed record UpdateOrganizationCommand(Guid OrganizationId, string Name, string? DataResidency) : ITransactionalCommand<Result<OrganizationResponse>>;

public sealed class UpdateOrganizationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateOrganizationCommand, Result<OrganizationResponse>>
{
    public async Task<Result<OrganizationResponse>> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await unitOfWork.OrganizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken, asTracking: true);
        if (organization is null)
        {
            return Result<OrganizationResponse>.Failure(Error.NotFound(CommonMessages.Organization.NotFound));
        }

        organization.UpdateName(request.Name);
        organization.UpdateDataResidency(request.DataResidency);
        return Result<OrganizationResponse>.Success(new OrganizationResponse(organization.Id, organization.Slug.Value, organization.Name, organization.Status.ToString(), organization.DataResidency));
    }
}
