using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Features.V1.SystemManagements.Department.Command;
using Nexora.Application.Features.V1.SystemManagements.Department.Model;
using Nexora.Application.Features.V1.SystemManagements.Department.Query;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[ApiController]
[Authorize]
[Route("api/departments")]
public sealed class DepartmentController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken) => Ok(await sender.Send(new GetDepartmentsQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new CreateDepartmentCommand(request.Code, request.Name, request.ParentDepartmentId, request.ManagerUserId), cancellationToken));

    [HttpGet("{departmentId:guid}")]
    public async Task<IActionResult> GetDepartment(Guid departmentId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GetDepartmentQuery(departmentId), cancellationToken));

    [HttpPut("{departmentId:guid}")]
    public async Task<IActionResult> UpdateDepartment(Guid departmentId, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new UpdateDepartmentCommand(departmentId, request.Name, request.ParentDepartmentId, request.ManagerUserId), cancellationToken));

    [HttpPost("{departmentId:guid}/archive")]
    public async Task<IActionResult> ArchiveDepartment(Guid departmentId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new ArchiveDepartmentCommand(departmentId), cancellationToken));

    [HttpPut("{departmentId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> AddMember(Guid departmentId, Guid userId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new AddDepartmentMemberCommand(departmentId, userId), cancellationToken));

    [HttpDelete("{departmentId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid departmentId, Guid userId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new RemoveDepartmentMemberCommand(departmentId, userId), cancellationToken));

    [HttpGet("{departmentId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid departmentId, CancellationToken cancellationToken) => Ok(await sender.Send(new GetDepartmentMembersQuery(departmentId), cancellationToken));
}
