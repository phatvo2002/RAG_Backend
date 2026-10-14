using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Department.Model;
using Nexora.Domain.Organizations;

namespace Nexora.Application.Features.V1.SystemManagements.Department.Command;

public sealed record UpdateDepartmentCommand(Guid DepartmentId, string Name, Guid? ParentDepartmentId, Guid? ManagerUserId) : ITransactionalCommand<Result<DepartmentResponse>>;

public sealed class UpdateDepartmentCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider) : IRequestHandler<UpdateDepartmentCommand, Result<DepartmentResponse>>
{
    public async Task<Result<DepartmentResponse>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var department = organizationId.HasValue ? await unitOfWork.DepartmentRepository.GetByIdAsync(request.DepartmentId, asTracking: true, cancellationToken: cancellationToken) : null;
        if (!organizationId.HasValue || department is null || department.OrganizationId != organizationId.Value)
        {
            return Result<DepartmentResponse>.Failure(Error.NotFound(CommonMessages.Department.NotFound));
        }

        if (request.ParentDepartmentId.HasValue && (request.ParentDepartmentId.Value == request.DepartmentId || !await unitOfWork.DepartmentRepository.ExistsAsync(request.ParentDepartmentId.Value, organizationId.Value, cancellationToken)))
        {
            return Result<DepartmentResponse>.Failure(Error.Conflict(CommonMessages.Department.ParentInvalid));
        }

        if (request.ManagerUserId.HasValue)
        {
            var manager = await unitOfWork.UserRepository.GetByIdAsync(request.ManagerUserId.Value, cancellationToken);
            if (manager is null || !manager.IsActive)
            {
                return Result<DepartmentResponse>.Failure(Error.NotFound(CommonMessages.Department.ManagerNotFoundOrInactive));
            }
        }

        department.UpdateName(request.Name);
        department.UpdateParent(request.ParentDepartmentId);
        if (request.ManagerUserId.HasValue)
        {
            department.UpdateManager(request.ManagerUserId.Value);
        }
        else
        {
            department.RemoveManager();
        }

        return Result<DepartmentResponse>.Success(new DepartmentResponse(department.Id, department.ParentDepartmentId, department.Code.Value, department.Name, department.ManagerUserId, department.Status.ToString()));
    }
}
