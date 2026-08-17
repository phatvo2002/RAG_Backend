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

public sealed class CreateUserCommandHandler(IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<CreateUserCommand, Result<UserResponse>>
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

        var userResult = await unitOfWork.UserRepository.CreateWithPasswordAsync(request.Email, request.UserName, request.Password, request.PhoneNumber, cancellationToken);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            return Result<UserResponse>.Failure(userResult.Error ?? new Error(CommonMessages.ErrorCodes.IdentityOperationFailed, CommonMessages.User.CannotCreate));
        }

        var now = clock.UtcNow;
        var profile = UserProfile.Create(userResult.Value.UserId, request.DisplayName, LanguageCode.Create(request.PreferredLanguage), request.TimeZone is null ? null : TimeZoneId.Create(request.TimeZone), now);
        unitOfWork.UserProfileRepository.Insert(profile);

        //    var membership = OrganizationMembership.Create(organizationId.Value, userResult.Value.UserId, userContext.UserId.Value, now);
        //membership.Activate(now);
        //membershipRepository.Insert(membership);

        return Result<UserResponse>.Success(ToResponse(userResult.Value, profile));
    }

    private static UserResponse ToResponse(IdentityUserSnapshot user, UserProfile profile) => new(user.UserId, user.Email, user.UserName, user.PhoneNumber, user.IsActive, profile.DisplayName, profile.PreferredLanguage.Value, profile.TimeZone?.Value);
}

public sealed record UpdateUserCommand(Guid UserId, string Email, string UserName, string? PhoneNumber, string DisplayName, string PreferredLanguage, string? TimeZone) : ITransactionalCommand<Result<UserResponse>>;

public sealed class UpdateUserCommandHandler(IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<UpdateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetAsync(
            item => item.Id == request.UserId,
            cancellationToken: cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        var email = request.Email.Trim();
        var userName = request.UserName.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var normalizedUserName = userName.ToUpperInvariant();
        var duplicateUser = await unitOfWork.UserRepository.GetAsync(
            item => item.Id != request.UserId &&
                (item.NormalizedEmail == normalizedEmail || item.NormalizedUserName == normalizedUserName),
            asNoTracking: true,
            cancellationToken: cancellationToken);
        if (duplicateUser is not null)
        {
            return Result<UserResponse>.Failure(Error.Conflict(CommonMessages.User.CannotUpdate));
        }

        user.Email = email;
        user.NormalizedEmail = normalizedEmail;
        user.UserName = userName;
        user.NormalizedUserName = normalizedUserName;
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        var profile = await unitOfWork.UserProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken, asTracking: true);
        if (profile is null)
        {
            profile = UserProfile.Create(request.UserId, request.DisplayName, LanguageCode.Create(request.PreferredLanguage), request.TimeZone is null ? null : TimeZoneId.Create(request.TimeZone), clock.UtcNow);
            unitOfWork.UserProfileRepository.Insert(profile);
        }
        else
        {
            profile.UpdateDisplayName(request.DisplayName);
            profile.UpdatePreferredLanguage(LanguageCode.Create(request.PreferredLanguage));
            profile.UpdateTimeZone(request.TimeZone is null ? null : TimeZoneId.Create(request.TimeZone));
        }

        return Result<UserResponse>.Success(new UserResponse(user.Id, user.Email, user.UserName, user.PhoneNumber, user.IsActive, profile.DisplayName, profile.PreferredLanguage.Value, profile.TimeZone?.Value));
    }
}

public sealed record DeactivateUserCommand(Guid UserId) : ITransactionalCommand<Result>;

public sealed class DeactivateUserCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<DeactivateUserCommand, Result>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetAsync(
            item => item.Id == request.UserId,
            cancellationToken: cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        user.IsActive = false;
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        if (userContext.UserId.HasValue && await organizationProvider.GetOrganizationIdAsync(cancellationToken) is Guid organizationId)
        {
            await unitOfWork.AccessTokenRepository.RevokeRefreshTokensAsync(request.UserId, organizationId, userContext.UserId.Value, clock.UtcNow, CommonMessages.TokenReasons.UserDeactivated, cancellationToken);
        }

        return Result.Success();
    }
}

public sealed record ReactivateUserCommand(Guid UserId) : ITransactionalCommand<Result>;

public sealed class ReactivateUserCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ReactivateUserCommand, Result>
{
    public async Task<Result> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetAsync(
            item => item.Id == request.UserId,
            cancellationToken: cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        user.IsActive = true;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        return Result.Success();
    }
}

public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword) : ITransactionalCommand<Result>;

public sealed class ResetUserPasswordCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ResetUserPasswordCommand, Result>
{
    public Task<Result> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken) => unitOfWork.UserRepository.ResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
}

public sealed record AssignUserRoleCommand(Guid UserId, Guid RoleId) : ITransactionalCommand<Result>;

public sealed class AssignUserRoleCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<AssignUserRoleCommand, Result>
{
    public async Task<Result> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.User.LoginRequiredToAssignRole));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var user = await unitOfWork.UserRepository.GetAsync(
            item => item.Id == request.UserId,
            asNoTracking: true,
            cancellationToken: cancellationToken);
        var role = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id == request.RoleId,
            asNoTracking: true,
            cancellationToken: cancellationToken);
        if (!organizationId.HasValue || user is null || role is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.RoleOrOrganizationNotFound));
        }

        var existing = await unitOfWork.OrganizationRoleAssignmentRepository.GetAsync(organizationId.Value, request.UserId, request.RoleId, asTracking: false, cancellationToken: cancellationToken);
        if (existing is not null)
        {
            return Result.Failure(Error.Conflict(CommonMessages.User.RoleAlreadyAssigned));
        }

        unitOfWork.OrganizationRoleAssignmentRepository.Insert(OrganizationRoleAssignment.Create(organizationId.Value, request.UserId, request.RoleId, userContext.UserId.Value, clock.UtcNow));
        return Result.Success();
    }
}

public sealed record RevokeUserRoleCommand(Guid UserId, Guid RoleId) : ITransactionalCommand<Result>;

public sealed class RevokeUserRoleCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<RevokeUserRoleCommand, Result>
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

        var assignment = await unitOfWork.OrganizationRoleAssignmentRepository.GetAsync(organizationId.Value, request.UserId, request.RoleId, asTracking: true, cancellationToken: cancellationToken);
        if (assignment is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.RoleAssignmentNotFound));
        }

        assignment.Revoke(userContext.UserId.Value, clock.UtcNow);
        return Result.Success();
    }
}
