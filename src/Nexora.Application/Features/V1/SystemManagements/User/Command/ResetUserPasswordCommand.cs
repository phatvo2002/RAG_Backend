using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.User.Command;

public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword) : ITransactionalCommand<Result>;

public sealed class ResetUserPasswordCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ResetUserPasswordCommand, Result>
{
    public Task<Result> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken) => unitOfWork.UserRepository.ResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
}
