using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Behaviors;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Permissions;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.User.Model;
using Nexora.Domain.Organizations;

namespace Nexora.Application.Features.V1.SystemManagements.User.Query;

public sealed record GetUsersQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<UserResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.UserRead;
}

public sealed class GetUsersQueryHandler(IIdentityService identityService, IUserProfileRepository profileRepository) : IRequestHandler<GetUsersQuery, PagedResult<UserResponse>>
{
    public async Task<PagedResult<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await identityService.GetUsersAsync(request.Page, request.PageSize, cancellationToken);
        var profiles = new Dictionary<Guid, UserProfile>();
        foreach (var user in users.Items)
        {
            var profile = await profileRepository.GetByUserIdAsync(user.UserId, cancellationToken);
            if (profile is not null)
            {
                profiles[user.UserId] = profile;
            }
        }

        var items = users.Items.Select(user => ToResponse(user, profiles.GetValueOrDefault(user.UserId))).ToArray();
        return new PagedResult<UserResponse>(items, users.Page, users.PageSize, users.TotalCount);
    }

    private static UserResponse ToResponse(IdentityUserSnapshot user, UserProfile? profile) => new(user.UserId, user.Email, user.UserName, user.PhoneNumber, user.IsActive, profile?.DisplayName, profile?.PreferredLanguage.Value, profile?.TimeZone?.Value);
}

public sealed record GetUserQuery(Guid UserId) : IQuery<Result<UserResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.UserReadById;
}

public sealed class GetUserQueryHandler(IIdentityService identityService, IUserProfileRepository profileRepository) : IRequestHandler<GetUserQuery, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await identityService.GetUserForManagementAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return Result<UserResponse>.Success(new UserResponse(user.UserId, user.Email, user.UserName, user.PhoneNumber, user.IsActive, profile?.DisplayName, profile?.PreferredLanguage.Value, profile?.TimeZone?.Value));
    }
}

public sealed record GetUserRolesQuery(Guid UserId) : IQuery<IReadOnlyList<UserRoleResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.UserReadRoles;
}

public sealed class GetUserRolesQueryHandler(IOrganizationRoleAssignmentRepository assignmentRepository, IDefaultOrganizationProvider organizationProvider) : IRequestHandler<GetUserRolesQuery, IReadOnlyList<UserRoleResponse>>
{
    public async Task<IReadOnlyList<UserRoleResponse>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Array.Empty<UserRoleResponse>();
        }

        var assignments = await assignmentRepository.GetByUserAsync(organizationId.Value, request.UserId, cancellationToken);
        return assignments.Select(assignment => new UserRoleResponse(assignment.RoleId, assignment.Status.ToString(), assignment.ValidFrom, assignment.ExpiresAt)).ToArray();
    }
}
