using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Department.Model;
using Nexora.Domain.Organizations;
using DepartmentEntity = Nexora.Domain.Organizations.Department;

namespace Nexora.Application.Features.V1.SystemManagements.Department.Command;

public sealed record CreateDepartmentCommand(string Code, string Name, Guid? ParentDepartmentId, Guid? ManagerUserId) : ITransactionalCommand<Result<DepartmentResponse>>;

public sealed class CreateDepartmentCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IClock clock) : IRequestHandler<CreateDepartmentCommand, Result<DepartmentResponse>>
{
    public async Task<Result<DepartmentResponse>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Result<DepartmentResponse>.Failure(Error.Conflict(CommonMessages.User.DefaultOrganizationNotConfigured));
        }

        if (await unitOfWork.DepartmentRepository.CodeExistsAsync(organizationId.Value, request.Code, null, cancellationToken))
        {
            return Result<DepartmentResponse>.Failure(Error.Conflict(CommonMessages.Department.CodeAlreadyExists));
        }

        if (request.ParentDepartmentId.HasValue && !await unitOfWork.DepartmentRepository.ExistsAsync(request.ParentDepartmentId.Value, organizationId.Value, cancellationToken))
        {
            return Result<DepartmentResponse>.Failure(Error.NotFound(CommonMessages.Department.ParentNotFound));
        }

        if (request.ManagerUserId.HasValue)
        {
            var manager = await unitOfWork.UserRepository.GetAsync(
                user => user.Id == request.ManagerUserId.Value,
                asNoTracking: true,
                cancellationToken: cancellationToken);
            if (manager is null || !manager.IsActive)
            {
                return Result<DepartmentResponse>.Failure(Error.NotFound(CommonMessages.Department.ManagerNotFoundOrInactive));
            }
        }

        var department = DepartmentEntity.Create(organizationId.Value, request.Code, request.Name, clock.UtcNow, request.ParentDepartmentId, request.ManagerUserId);
        unitOfWork.DepartmentRepository.Insert(department);
        return Result<DepartmentResponse>.Success(ToResponse(department));
    }

    private static DepartmentResponse ToResponse(DepartmentEntity department) => new(department.Id, department.ParentDepartmentId, department.Code.Value, department.Name, department.ManagerUserId, department.Status.ToString());
}
