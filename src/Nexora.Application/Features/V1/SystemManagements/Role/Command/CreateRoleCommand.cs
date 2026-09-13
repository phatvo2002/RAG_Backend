using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Role.Model;
using Nexora.Infrastructure.Identity;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Command;

public sealed record CreateRoleCommand(string Name) : ITransactionalCommand<Result<RoleResponse>>;

public sealed class CreateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        var existingRole = await unitOfWork.RoleRepository.GetAsync(
            role => role.NormalizedName == normalizedName,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (existingRole is not null)
        {
            return Result<RoleResponse>.Failure(Error.Conflict(CommonMessages.Role.CannotCreate));
        }

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = normalizedName,
            IsActive = true,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        unitOfWork.RoleRepository.Insert(role);
        return Result<RoleResponse>.Success(new RoleResponse(role.Id, role.Name, role.IsActive));
    }
}
