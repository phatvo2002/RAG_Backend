using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Behaviors;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Permissions;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Organization.Model;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;
using OrganizationEntity = Nexora.Domain.Organizations.Organization;

namespace Nexora.Application.Features.V1.SystemManagements.Organization.Command;

public sealed record CreateOrganizationCommand(string Slug, string Name, string? DataResidency) : ITransactionalCommand<Result<OrganizationResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.OrganizationCreate;
}

public sealed class CreateOrganizationCommandHandler(IOrganizationRepository organizationRepository, IClock clock) : IRequestHandler<CreateOrganizationCommand, Result<OrganizationResponse>>
{
    public async Task<Result<OrganizationResponse>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var slug = OrganizationSlug.Create(request.Slug);
        if (await organizationRepository.SlugExistsAsync(slug, cancellationToken))
        {
            return Result<OrganizationResponse>.Failure(Error.Conflict(CommonMessages.Organization.SlugAlreadyExists));
        }

        var organization = OrganizationEntity.Create(slug.Value, request.Name, clock.UtcNow, request.DataResidency);
        await organizationRepository.AddAsync(organization, cancellationToken);
        return Result<OrganizationResponse>.Success(ToResponse(organization));
    }

    private static OrganizationResponse ToResponse(OrganizationEntity organization) => new(organization.Id, organization.Slug.Value, organization.Name, organization.Status.ToString(), organization.DataResidency);
}

public sealed record UpdateOrganizationCommand(Guid OrganizationId, string Name, string? DataResidency) : ITransactionalCommand<Result<OrganizationResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.OrganizationUpdate;
}

public sealed class UpdateOrganizationCommandHandler(IOrganizationRepository organizationRepository) : IRequestHandler<UpdateOrganizationCommand, Result<OrganizationResponse>>
{
    public async Task<Result<OrganizationResponse>> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken, asTracking: true);
        if (organization is null)
        {
            return Result<OrganizationResponse>.Failure(Error.NotFound(CommonMessages.Organization.NotFound));
        }

        organization.UpdateName(request.Name);
        organization.UpdateDataResidency(request.DataResidency);
        return Result<OrganizationResponse>.Success(new OrganizationResponse(organization.Id, organization.Slug.Value, organization.Name, organization.Status.ToString(), organization.DataResidency));
    }
}

public sealed record SuspendOrganizationCommand(Guid OrganizationId) : ITransactionalCommand<Result>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.OrganizationSuspend;
}

public sealed class SuspendOrganizationCommandHandler(IOrganizationRepository organizationRepository) : IRequestHandler<SuspendOrganizationCommand, Result>
{
    public async Task<Result> Handle(SuspendOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken, asTracking: true);
        if (organization is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Organization.NotFound));
        }

        organization.Suspend();
        return Result.Success();
    }
}

public sealed record ActivateOrganizationCommand(Guid OrganizationId) : ITransactionalCommand<Result>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.OrganizationActivate;
}

public sealed class ActivateOrganizationCommandHandler(IOrganizationRepository organizationRepository) : IRequestHandler<ActivateOrganizationCommand, Result>
{
    public async Task<Result> Handle(ActivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken, asTracking: true);
        if (organization is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Organization.NotFound));
        }

        organization.Activate();
        return Result.Success();
    }
}

public sealed record ArchiveOrganizationCommand(Guid OrganizationId) : ITransactionalCommand<Result>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.OrganizationArchive;
}

public sealed class ArchiveOrganizationCommandHandler(IOrganizationRepository organizationRepository) : IRequestHandler<ArchiveOrganizationCommand, Result>
{
    public async Task<Result> Handle(ArchiveOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken, asTracking: true);
        if (organization is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Organization.NotFound));
        }

        organization.Archive();
        return Result.Success();
    }
}
