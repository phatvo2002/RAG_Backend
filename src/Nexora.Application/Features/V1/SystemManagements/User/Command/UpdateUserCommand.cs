using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Helpers;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.User.Model;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.User.Command;

public sealed record UpdateUserCommand(Guid UserId, string Email, string UserName, string? PhoneNumber, string DisplayName, string PreferredLanguage, string? TimeZone, Guid? OrganizationId = null) : ITransactionalCommand<Result<UserResponse>>;

public sealed class UpdateUserCommandHandler(IUnitOfWork unitOfWork, IClock clock, IDefaultOrganizationProvider organizationProvider, IUserContext userContext) : IRequestHandler<UpdateUserCommand, Result<UserResponse>>
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

        Guid? organizationId = null;
        var actorUserId = Guid.Empty;
        if (request.OrganizationId.HasValue)
        {
            organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
            var organization = organizationId.HasValue && organizationId.Value == request.OrganizationId.Value
                ? await unitOfWork.OrganizationRepository.GetByIdAsync(organizationId.Value, cancellationToken)
                : null;
            if (organization is null || organization.Status != OrganizationStatus.Active)
            {
                return Result<UserResponse>.Failure(Error.Validation("Tổ chức được chọn không hợp lệ hoặc không hoạt động."));
            }
            if (!userContext.UserId.HasValue)
            {
                return Result<UserResponse>.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));
            }
            actorUserId = userContext.UserId.Value;
        }

        var email = request.Email.Trim();
        var userName = UserHelper.NormalizeUserName(request.UserName);
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

        if (organizationId.HasValue)
        {
            var membership = await unitOfWork.OrganizationMembershipRepository.GetForUpdateAsync(organizationId.Value, request.UserId, cancellationToken);
            if (membership is null)
            {
                membership = OrganizationMembership.Create(organizationId.Value, request.UserId, actorUserId, clock.UtcNow);
                membership.Activate(clock.UtcNow);
                unitOfWork.OrganizationMembershipRepository.Insert(membership);
            }
            else if (membership.Status == OrganizationMembershipStatus.Removed)
            {
                membership.Rejoin(clock.UtcNow);
            }
            else if (membership.Status is OrganizationMembershipStatus.Invited or OrganizationMembershipStatus.Suspended)
            {
                membership.Activate(clock.UtcNow);
            }
        }

        var responseOrganizationId = organizationId;
        if (!responseOrganizationId.HasValue && await organizationProvider.GetOrganizationIdAsync(cancellationToken) is Guid currentOrganizationId)
        {
            var activeMemberships = await unitOfWork.OrganizationMembershipRepository.GetActiveByUsersAsync(
                currentOrganizationId, [request.UserId], cancellationToken);
            responseOrganizationId = activeMemberships.FirstOrDefault()?.OrganizationId;
        }

        return Result<UserResponse>.Success(new UserResponse(user.Id, user.Email, user.UserName, user.PhoneNumber, user.IsActive, profile.DisplayName, profile.PreferredLanguage.Value, profile.TimeZone?.Value, organizationId: responseOrganizationId));
    }
}
