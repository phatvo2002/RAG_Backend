using MediatR;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Auth.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Query;

public sealed record GetCurrentUserQuery : IQuery<Result<CurrentUserResponse>>;

public sealed class GetCurrentUserQueryHandler(
    IUserContext userContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
    public async Task<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<CurrentUserResponse>.Failure(
                Error.Unauthorized(CommonMessages.Auth.LoginRequiredToViewCurrentUser));
        }

        var user = await unitOfWork.UserRepository.GetAsync(
            item => item.Id == userContext.UserId.Value && item.IsActive,
            asNoTracking: true,
            cancellationToken: cancellationToken);
        if (user is null)
        {
            return Result<CurrentUserResponse>.Failure(
                Error.Unauthorized(CommonMessages.Auth.UserNotValid));
        }

        var profile = await unitOfWork.UserProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);

        return Result<CurrentUserResponse>.Success(
            new CurrentUserResponse(
                user.Id,
                user.Email ?? user.UserName ?? string.Empty,
                user.UserName ?? user.Email ?? string.Empty,
                profile?.DisplayName));
    }
}
