using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.User.Model;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;

namespace Nexora.Application.Features.V1.SystemManagements.User.Command;

public sealed record CreateUserCommand(string Email, string UserName, string Password, string? PhoneNumber, string DisplayName, string PreferredLanguage, string? TimeZone) : ITransactionalCommand<Result<UserResponse>>;

public sealed class CreateUserCommandHandler(IIdentityService identityService, IUserProfileRepository profileRepository, IOrganizationMembershipRepository membershipRepository, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<CreateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        //if (!userContext.UserId.HasValue)
        //{
        //    return Result<UserResponse>.Failure(Error.Unauthorized(CommonMessages.User.LoginRequiredToCreate));
        //}

        //var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        //if (!organizationId.HasValue)
        //{
        //    return Result<UserResponse>.Failure(Error.Conflict(CommonMessages.User.DefaultOrganizationNotConfigured));
        //}

        var userResult = await identityService.CreateUserAsync(request.Email, request.UserName, request.Password, request.PhoneNumber, cancellationToken);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            return Result<UserResponse>.Failure(userResult.Error ?? new Error(CommonMessages.ErrorCodes.IdentityOperationFailed, CommonMessages.User.CannotCreate));
        }

        var now = clock.UtcNow;
        var profile = UserProfile.Create(userResult.Value.UserId, request.DisplayName, LanguageCode.Create(request.PreferredLanguage), request.TimeZone is null ? null : TimeZoneId.Create(request.TimeZone), now);
        await profileRepository.AddAsync(profile, cancellationToken);

    //    var membership = OrganizationMembership.Create(organizationId.Value, userResult.Value.UserId, userContext.UserId.Value, now);
        //membership.Activate(now);
        //await membershipRepository.AddAsync(membership, cancellationToken);

        return Result<UserResponse>.Success(ToResponse(userResult.Value, profile));
    }

    private static UserResponse ToResponse(IdentityUserSnapshot user, UserProfile profile) => new(user.UserId, user.Email, user.UserName, user.PhoneNumber, user.IsActive, profile.DisplayName, profile.PreferredLanguage.Value, profile.TimeZone?.Value);
}

public sealed record UpdateUserCommand(Guid UserId, string Email, string UserName, string? PhoneNumber, string DisplayName, string PreferredLanguage, string? TimeZone) : ITransactionalCommand<Result<UserResponse>>;

public sealed class UpdateUserCommandHandler(IIdentityService identityService, IUserProfileRepository profileRepository, IClock clock) : IRequestHandler<UpdateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var userResult = await identityService.UpdateUserAsync(request.UserId, request.Email, request.UserName, request.PhoneNumber, cancellationToken);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            return Result<UserResponse>.Failure(userResult.Error ?? new Error(CommonMessages.ErrorCodes.IdentityOperationFailed, CommonMessages.User.CannotUpdate));
        }

        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken, asTracking: true);
        if (profile is null)
        {
            profile = UserProfile.Create(request.UserId, request.DisplayName, LanguageCode.Create(request.PreferredLanguage), request.TimeZone is null ? null : TimeZoneId.Create(request.TimeZone), clock.UtcNow);
            await profileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.UpdateDisplayName(request.DisplayName);
            profile.UpdatePreferredLanguage(LanguageCode.Create(request.PreferredLanguage));
            profile.UpdateTimeZone(request.TimeZone is null ? null : TimeZoneId.Create(request.TimeZone));
        }

        return Result<UserResponse>.Success(new UserResponse(userResult.Value.UserId, userResult.Value.Email, userResult.Value.UserName, userResult.Value.PhoneNumber, userResult.Value.IsActive, profile.DisplayName, profile.PreferredLanguage.Value, profile.TimeZone?.Value));
    }
}

public sealed record DeactivateUserCommand(Guid UserId) : ITransactionalCommand<Result>;

public sealed class DeactivateUserCommandHandler(IIdentityService identityService, IAccessTokenRepository accessTokenRepository, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<DeactivateUserCommand, Result>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.DeactivateUserAsync(request.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result;
        }

        if (userContext.UserId.HasValue && await organizationProvider.GetOrganizationIdAsync(cancellationToken) is Guid organizationId)
        {
            await accessTokenRepository.RevokeRefreshTokensAsync(request.UserId, organizationId, userContext.UserId.Value, clock.UtcNow, CommonMessages.TokenReasons.UserDeactivated, cancellationToken);
        }

        return Result.Success();
    }
}

public sealed record ReactivateUserCommand(Guid UserId) : ITransactionalCommand<Result>;

public sealed class ReactivateUserCommandHandler(IIdentityService identityService) : IRequestHandler<ReactivateUserCommand, Result>
{
    public Task<Result> Handle(ReactivateUserCommand request, CancellationToken cancellationToken) => identityService.ReactivateUserAsync(request.UserId, cancellationToken);
}

public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword) : ITransactionalCommand<Result>;

public sealed class ResetUserPasswordCommandHandler(IIdentityService identityService) : IRequestHandler<ResetUserPasswordCommand, Result>
{
    public Task<Result> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken) => identityService.ResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
}

public sealed record AssignUserRoleCommand(Guid UserId, Guid RoleId) : ITransactionalCommand<Result>;

public sealed class AssignUserRoleCommandHandler(IIdentityService identityService, IOrganizationRoleAssignmentRepository assignmentRepository, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<AssignUserRoleCommand, Result>
{
    public async Task<Result> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.User.LoginRequiredToAssignRole));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var user = await identityService.GetUserForManagementAsync(request.UserId, cancellationToken);
        var role = await identityService.GetRoleByIdAsync(request.RoleId, cancellationToken);
        if (!organizationId.HasValue || user is null || role is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.RoleOrOrganizationNotFound));
        }

        var existing = await assignmentRepository.GetAsync(organizationId.Value, request.UserId, request.RoleId, asTracking: false, cancellationToken: cancellationToken);
        if (existing is not null)
        {
            return Result.Failure(Error.Conflict(CommonMessages.User.RoleAlreadyAssigned));
        }

        await assignmentRepository.AddAsync(OrganizationRoleAssignment.Create(organizationId.Value, request.UserId, request.RoleId, userContext.UserId.Value, clock.UtcNow), cancellationToken);
        return Result.Success();
    }
}

public sealed record RevokeUserRoleCommand(Guid UserId, Guid RoleId) : ITransactionalCommand<Result>;

public sealed class RevokeUserRoleCommandHandler(IOrganizationRoleAssignmentRepository assignmentRepository, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<RevokeUserRoleCommand, Result>
{
    public async Task<Result> Handle(RevokeUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.User.LoginRequiredToRevokeRole));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Result.Failure(Error.Conflict(CommonMessages.User.DefaultOrganizationNotConfigured));
        }

        var assignment = await assignmentRepository.GetAsync(organizationId.Value, request.UserId, request.RoleId, asTracking: true, cancellationToken: cancellationToken);
        if (assignment is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.RoleAssignmentNotFound));
        }

        assignment.Revoke(userContext.UserId.Value, clock.UtcNow);
        return Result.Success();
    }
}
