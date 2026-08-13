using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.User.Model;
using Nexora.Domain.Organizations;
using Nexora.Infrastructure.Identity;

namespace Nexora.Application.Features.V1.SystemManagements.User.Query;

public sealed record GetUsersQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<UserResponse>>;

public sealed class GetUsersQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetUsersQuery, PagedResult<UserResponse>>
{
    public async Task<PagedResult<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var users = await unitOfWork.UserRepository.PaginationAsync(
            page,
            pageSize,
            orderBy: query => query.OrderBy(user => user.Email),
            asNoTracking: true,
            cancellationToken: cancellationToken);
        var profiles = new Dictionary<Guid, UserProfile>();
        foreach (var user in users.Data)
        {
            var profile = await unitOfWork.UserProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
            if (profile is not null)
            {
                profiles[user.Id] = profile;
            }
        }

        var items = users.Data.Select(user => ToResponse(user, profiles.GetValueOrDefault(user.Id))).ToArray();
        return new PagedResult<UserResponse>(items, page, pageSize, users.Total);
    }

    private static UserResponse ToResponse(ApplicationUser user, UserProfile? profile) =>
        new(
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            user.UserName ?? user.Email ?? string.Empty,
            user.PhoneNumber,
            user.IsActive,
            profile?.DisplayName,
            profile?.PreferredLanguage.Value,
            profile?.TimeZone?.Value);
}

public sealed record GetUserQuery(Guid UserId) : IQuery<Result<UserResponse>>;

public sealed class GetUserQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetUserQuery, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetAsync(
            item => item.Id == request.UserId,
            asNoTracking: true,
            cancellationToken: cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        var profile = await unitOfWork.UserProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return Result<UserResponse>.Success(
            new UserResponse(
                user.Id,
                user.Email ?? user.UserName ?? string.Empty,
                user.UserName ?? user.Email ?? string.Empty,
                user.PhoneNumber,
                user.IsActive,
                profile?.DisplayName,
                profile?.PreferredLanguage.Value,
                profile?.TimeZone?.Value));
    }
}

public sealed record GetUserRolesQuery(Guid UserId) : IQuery<IReadOnlyList<UserRoleResponse>>;

public sealed class GetUserRolesQueryHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider) : IRequestHandler<GetUserRolesQuery, IReadOnlyList<UserRoleResponse>>
{
    public async Task<IReadOnlyList<UserRoleResponse>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Array.Empty<UserRoleResponse>();
        }

        var assignments = await unitOfWork.OrganizationRoleAssignmentRepository.GetByUserAsync(organizationId.Value, request.UserId, cancellationToken);
        return assignments.Select(assignment => new UserRoleResponse(assignment.RoleId, assignment.Status.ToString(), assignment.ValidFrom, assignment.ExpiresAt)).ToArray();
    }
}
