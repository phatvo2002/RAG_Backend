using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Command;

public sealed record LogoutCommand(string RefreshToken)
    : ITransactionalCommand<Result>;

public sealed class LogoutCommandHandler(IAuthTokenService authTokenService, IUserContext userContext)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await authTokenService.RevokeRefreshTokenAsync(request.RefreshToken, userContext.UserId, "user_logout", cancellationToken);

        return Result.Success();
    }
}
