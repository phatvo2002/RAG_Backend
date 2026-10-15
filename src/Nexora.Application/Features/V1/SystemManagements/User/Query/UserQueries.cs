using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.User.Model;
using Nexora.Domain.Organizations;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.User.Query;

public sealed record GetUsersQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<UserResponse>>;

public sealed class GetUsersQueryHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IClock clock) : IRequestHandler<GetUsersQuery, PagedResult<UserResponse>>
{
    public async Task<PagedResult<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var users = await unitOfWork.UserRepository.ListPageAsync(page, pageSize, cancellationToken);
        var profiles = new Dictionary<Guid, UserProfile>();
        foreach (var user in users.Data)
        {
            var profile = await unitOfWork.UserProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
            if (profile is not null)
            {
                profiles[user.Id] = profile;
            }
        }

        var rolesByUser = new Dictionary<Guid, IReadOnlyCollection<UserRoleResponse>>();
        var organizationsByUser = new Dictionary<Guid, Guid>();
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (organizationId.HasValue && users.Data.Count > 0)
        {
            var userIds = users.Data.Select(user => user.Id).ToArray();
            organizationsByUser = (await unitOfWork.OrganizationMembershipRepository.GetActiveByUsersAsync(organizationId.Value, userIds, cancellationToken))
                .ToDictionary(membership => membership.UserId, membership => membership.OrganizationId);
            var now = clock.UtcNow;
            var assignments = (await unitOfWork.OrganizationRoleAssignmentRepository.GetByUsersAsync(organizationId.Value, userIds, cancellationToken))
                .Where(assignment => assignment.Status == OrganizationRoleAssignmentStatus.Active && assignment.ValidFrom <= now && (assignment.ExpiresAt == null || assignment.ExpiresAt > now) && assignment.RevokedAt == null)
                .ToArray();
            var roleIds = assignments.Select(assignment => assignment.RoleId).Distinct().ToArray();
            var roles = await unitOfWork.RoleRepository.GetByIdsAsync(roleIds, cancellationToken);
            var roleNames = roles.ToDictionary(role => role.Id, role => role.Name ?? string.Empty);
            rolesByUser = assignments
                .Where(assignment => roleNames.ContainsKey(assignment.RoleId))
                .GroupBy(assignment => assignment.UserId)
                .ToDictionary(group => group.Key, group => (IReadOnlyCollection<UserRoleResponse>)group.Select(assignment => ToRoleResponse(assignment, roleNames[assignment.RoleId])).ToArray());
        }

        var items = users.Data.Select(user => ToResponse(user, profiles.GetValueOrDefault(user.Id), rolesByUser.GetValueOrDefault(user.Id),
            organizationsByUser.TryGetValue(user.Id, out var memberOrganizationId) ? memberOrganizationId : null)).ToArray();
        return new PagedResult<UserResponse>(items, page, pageSize, users.Total);
    }

    private static UserResponse ToResponse(IdentityUserSnapshot user, UserProfile? profile, IReadOnlyCollection<UserRoleResponse>? roles = null, Guid? organizationId = null) =>
        new(
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            user.UserName ?? user.Email ?? string.Empty,
            user.PhoneNumber,
            user.IsActive,
            profile?.DisplayName,
            profile?.PreferredLanguage.Value,
            profile?.TimeZone?.Value,
            roles,
            organizationId);

    private static UserRoleResponse ToRoleResponse(OrganizationRoleAssignment assignment, string roleName) => new(assignment.RoleId, roleName, assignment.Status.ToString(), assignment.ValidFrom, assignment.ExpiresAt);
}

public sealed record GetUserQuery(Guid UserId) : IQuery<Result<UserResponse>>;

public sealed class GetUserQueryHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider) : IRequestHandler<GetUserQuery, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        var profile = await unitOfWork.UserProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var membership = organizationId.HasValue
            ? (await unitOfWork.OrganizationMembershipRepository.GetActiveByUsersAsync(organizationId.Value, [request.UserId], cancellationToken)).FirstOrDefault()
            : null;
        return Result<UserResponse>.Success(
            new UserResponse(
                user.Id,
                user.Email ?? user.UserName ?? string.Empty,
                user.UserName ?? user.Email ?? string.Empty,
                user.PhoneNumber,
                user.IsActive,
                profile?.DisplayName,
                profile?.PreferredLanguage.Value,
                profile?.TimeZone?.Value,
                organizationId: membership?.OrganizationId));
    }
}

public sealed record GetUserRolesQuery(Guid UserId) : IQuery<IReadOnlyList<UserRoleResponse>>;

public sealed class GetUserRolesQueryHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IClock clock) : IRequestHandler<GetUserRolesQuery, IReadOnlyList<UserRoleResponse>>
{
    public async Task<IReadOnlyList<UserRoleResponse>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Array.Empty<UserRoleResponse>();
        }

        var now = clock.UtcNow;
        var assignments = (await unitOfWork.OrganizationRoleAssignmentRepository.GetByUserAsync(organizationId.Value, request.UserId, cancellationToken))
            .Where(assignment => assignment.Status == OrganizationRoleAssignmentStatus.Active && assignment.ValidFrom <= now && (assignment.ExpiresAt == null || assignment.ExpiresAt > now) && assignment.RevokedAt == null)
            .ToArray();
        var roleIds = assignments.Select(assignment => assignment.RoleId).Distinct().ToArray();
        if (roleIds.Length == 0)
        {
            return Array.Empty<UserRoleResponse>();
        }

        var roles = await unitOfWork.RoleRepository.GetByIdsAsync(roleIds, cancellationToken);
        var roleNames = roles.ToDictionary(role => role.Id, role => role.Name ?? string.Empty);
        return assignments.Where(assignment => roleNames.ContainsKey(assignment.RoleId)).Select(assignment => new UserRoleResponse(assignment.RoleId, roleNames[assignment.RoleId], assignment.Status.ToString(), assignment.ValidFrom, assignment.ExpiresAt)).ToArray();
    }
}
