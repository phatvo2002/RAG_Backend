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
