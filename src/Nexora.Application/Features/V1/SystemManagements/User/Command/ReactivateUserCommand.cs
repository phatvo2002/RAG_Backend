using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.User.Command;

public sealed record ReactivateUserCommand(Guid UserId) : ITransactionalCommand<Result>;

public sealed class ReactivateUserCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ReactivateUserCommand, Result>
{
    public async Task<Result> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        await unitOfWork.UserRepository.SetActiveAsync(request.UserId, true, cancellationToken);
        return Result.Success();
    }
}
