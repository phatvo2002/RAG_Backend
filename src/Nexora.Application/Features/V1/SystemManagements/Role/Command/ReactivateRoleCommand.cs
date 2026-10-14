using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Command;

public sealed record ReactivateRoleCommand(Guid RoleId) : ITransactionalCommand<Result>;

public sealed class ReactivateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ReactivateRoleCommand, Result>
{
    public async Task<Result> Handle(ReactivateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RoleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        await unitOfWork.RoleRepository.SetActiveAsync(request.RoleId, true, cancellationToken);
        return Result.Success();
    }
}
