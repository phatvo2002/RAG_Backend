using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Role.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Command;

public sealed record UpdateRoleCommand(Guid RoleId, string Name) : ITransactionalCommand<Result<RoleResponse>>;

public sealed class UpdateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RoleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result<RoleResponse>.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        var name = request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        if (await unitOfWork.RoleRepository.ExistsWithNormalizedNameAsync(normalizedName, request.RoleId, cancellationToken))
        {
            return Result<RoleResponse>.Failure(Error.Conflict(CommonMessages.Role.CannotUpdate));
        }

        await unitOfWork.RoleRepository.RenameAsync(request.RoleId, name, normalizedName, cancellationToken);
        role.Name = name;

        return Result<RoleResponse>.Success(new RoleResponse(role.Id, role.Name, role.IsActive));
    }
}
