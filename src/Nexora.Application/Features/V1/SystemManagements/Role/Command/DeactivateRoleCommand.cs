using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Command;

public sealed record DeactivateRoleCommand(Guid RoleId) : ITransactionalCommand<Result>;

public sealed class DeactivateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeactivateRoleCommand, Result>
{
    public async Task<Result> Handle(DeactivateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RoleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        await unitOfWork.RoleRepository.SetActiveAsync(request.RoleId, false, cancellationToken);
        return Result.Success();
    }
}
