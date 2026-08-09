using MediatR;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Auth.Mapper;
using Nexora.Application.Features.V1.SystemManagements.Auth.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Command;

public sealed record RefreshTokenCommand(string RefreshToken)
    : ITransactionalCommand<Result<AuthResponse>>;

public sealed class RefreshTokenCommandHandler(IAuthTokenService authTokenService, IIdentityService identityService, IHttpRequestMetadata requestMetadata)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenSet = await authTokenService.RotateRefreshTokenAsync(request.RefreshToken, requestMetadata.IpAddress, requestMetadata.UserAgent, cancellationToken);

        if (tokenSet is null)
        {
            return Result<AuthResponse>.Failure(
                new Error(CommonMessages.ErrorCodes.InvalidRefreshToken, CommonMessages.Auth.InvalidRefreshToken));
        }

        var user = await identityService.GetByIdAsync(tokenSet.UserId, cancellationToken);
        if (user is null)
        {
            return Result<AuthResponse>.Failure(
                new Error(CommonMessages.ErrorCodes.UserNotFound, CommonMessages.Auth.UserNotFound));
        }

        return Result<AuthResponse>.Success(AuthMapper.ToResponse(user, tokenSet));
    }
}
