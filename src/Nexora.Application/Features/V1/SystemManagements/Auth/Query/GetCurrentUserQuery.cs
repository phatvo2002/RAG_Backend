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
    ITenantContext tenantContext,
    IIdentityService identityService,
    IUserProfileRepository userProfileRepository)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
    public async Task<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<CurrentUserResponse>.Failure(
                Error.Unauthorized(CommonMessages.Auth.LoginRequiredToViewCurrentUser));
        }

        if (!tenantContext.OrganizationId.HasValue)
        {
            return Result<CurrentUserResponse>.Failure(
                new Error(CommonMessages.ErrorCodes.OrganizationRequired, CommonMessages.Auth.OrganizationRequired));
        }

        var user = await identityService.GetByIdAsync(userContext.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result<CurrentUserResponse>.Failure(
                Error.Unauthorized(CommonMessages.Auth.UserNotValid));
        }

        var profile = await userProfileRepository.GetByUserIdAsync(user.UserId, cancellationToken);

        return Result<CurrentUserResponse>.Success(
            new CurrentUserResponse(user.UserId, user.Email, user.UserName, profile?.DisplayName, tenantContext.OrganizationId.Value));
    }
}
