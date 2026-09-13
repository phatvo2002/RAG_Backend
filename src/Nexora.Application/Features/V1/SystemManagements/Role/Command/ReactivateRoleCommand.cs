using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Infrastructure.Identity;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Command;

public sealed record ReactivateRoleCommand(Guid RoleId) : ITransactionalCommand<Result>;

public sealed class ReactivateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ReactivateRoleCommand, Result>
{
    public async Task<Result> Handle(ReactivateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id == request.RoleId,
            cancellationToken: cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        role.IsActive = true;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        return Result.Success();
    }
}
