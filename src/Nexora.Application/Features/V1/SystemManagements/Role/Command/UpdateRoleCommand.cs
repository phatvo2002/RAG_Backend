using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Role.Model;
using Nexora.Infrastructure.Identity;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Command;

public sealed record UpdateRoleCommand(Guid RoleId, string Name) : ITransactionalCommand<Result<RoleResponse>>;

public sealed class UpdateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id == request.RoleId,
            cancellationToken: cancellationToken);
        if (role is null)
        {
            return Result<RoleResponse>.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        var name = request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        var duplicateRole = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id != request.RoleId && item.NormalizedName == normalizedName,
            asNoTracking: true,
            cancellationToken: cancellationToken);
        if (duplicateRole is not null)
        {
            return Result<RoleResponse>.Failure(Error.Conflict(CommonMessages.Role.CannotUpdate));
        }

        role.Name = name;
        role.NormalizedName = normalizedName;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();

        return Result<RoleResponse>.Success(new RoleResponse(role.Id, role.Name, role.IsActive));
    }
}
