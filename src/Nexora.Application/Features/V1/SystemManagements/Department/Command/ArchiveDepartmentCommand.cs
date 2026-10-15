using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.Department.Command;

public sealed record ArchiveDepartmentCommand(Guid DepartmentId) : ITransactionalCommand<Result>;

public sealed class ArchiveDepartmentCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider) : IRequestHandler<ArchiveDepartmentCommand, Result>
{
    public async Task<Result> Handle(ArchiveDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var department = organizationId.HasValue ? await unitOfWork.DepartmentRepository.GetByIdAsync(request.DepartmentId, asTracking: true, cancellationToken: cancellationToken) : null;
        if (!organizationId.HasValue || department is null || department.OrganizationId != organizationId.Value)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Department.NotFound));
        }

        department.Archive();
        return Result.Success();
    }
}
