using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Helpers;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.User.Model;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;

namespace Nexora.Application.Features.V1.SystemManagements.User.Command;

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

        return Result<UserResponse>.Success(new UserResponse(user.Id, user.Email, user.UserName, user.PhoneNumber, user.IsActive, profile.DisplayName, profile.PreferredLanguage.Value, profile.TimeZone?.Value));
    }
}
