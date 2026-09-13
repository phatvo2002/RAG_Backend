using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Organization.Model;
using Nexora.Domain.Common.ValueObjects;
using OrganizationEntity = Nexora.Domain.Organizations.Organization;

namespace Nexora.Application.Features.V1.SystemManagements.Organization.Command;

public sealed record CreateOrganizationCommand(string Slug, string Name, string? DataResidency) : ITransactionalCommand<Result<OrganizationResponse>>;

public sealed class CreateOrganizationCommandHandler(IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<CreateOrganizationCommand, Result<OrganizationResponse>>
{
    public async Task<Result<OrganizationResponse>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var slug = OrganizationSlug.Create(request.Slug);
        if (await unitOfWork.OrganizationRepository.SlugExistsAsync(slug, cancellationToken))
        {
            return Result<OrganizationResponse>.Failure(Error.Conflict(CommonMessages.Organization.SlugAlreadyExists));
        }

        var organization = OrganizationEntity.Create(slug.Value, request.Name, clock.UtcNow, request.DataResidency);
        unitOfWork.OrganizationRepository.Insert(organization);
        return Result<OrganizationResponse>.Success(ToResponse(organization));
    }

    private static OrganizationResponse ToResponse(OrganizationEntity organization) => new(organization.Id, organization.Slug.Value, organization.Name, organization.Status.ToString(), organization.DataResidency);
}
