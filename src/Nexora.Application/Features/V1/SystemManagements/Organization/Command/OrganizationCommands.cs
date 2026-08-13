using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Organization.Model;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;
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

public sealed record SuspendOrganizationCommand(Guid OrganizationId) : ITransactionalCommand<Result>;

public sealed class SuspendOrganizationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SuspendOrganizationCommand, Result>
{
    public async Task<Result> Handle(SuspendOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await unitOfWork.OrganizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken, asTracking: true);
        if (organization is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Organization.NotFound));
        }

        organization.Suspend();
        return Result.Success();
    }
}

public sealed record ActivateOrganizationCommand(Guid OrganizationId) : ITransactionalCommand<Result>;

public sealed class ActivateOrganizationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ActivateOrganizationCommand, Result>
{
    public async Task<Result> Handle(ActivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await unitOfWork.OrganizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken, asTracking: true);
        if (organization is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Organization.NotFound));
        }

        organization.Activate();
        return Result.Success();
    }
}

public sealed record ArchiveOrganizationCommand(Guid OrganizationId) : ITransactionalCommand<Result>;

public sealed class ArchiveOrganizationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ArchiveOrganizationCommand, Result>
{
    public async Task<Result> Handle(ArchiveOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await unitOfWork.OrganizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken, asTracking: true);
        if (organization is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Organization.NotFound));
        }

        organization.Archive();
        return Result.Success();
    }
}
