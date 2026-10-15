using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.User.Command;

public sealed record DeactivateUserCommand(Guid UserId) : ITransactionalCommand<Result>;

public sealed class DeactivateUserCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<DeactivateUserCommand, Result>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        await unitOfWork.UserRepository.SetActiveAsync(request.UserId, false, cancellationToken);

        if (userContext.UserId.HasValue && await organizationProvider.GetOrganizationIdAsync(cancellationToken) is Guid organizationId)
        {
            await unitOfWork.AccessTokenRepository.RevokeRefreshTokensAsync(request.UserId, organizationId, userContext.UserId.Value, clock.UtcNow, CommonMessages.TokenReasons.UserDeactivated, cancellationToken);
        }

        return Result.Success();
    }
}
