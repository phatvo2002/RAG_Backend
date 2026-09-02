using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Application.Features.V1.SystemManagements.Department.Command;
using Nexora.Application.Features.V1.SystemManagements.Department.Model;
using Nexora.Application.Features.V1.SystemManagements.Department.Query;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[Route("api/departments")]
public sealed class DepartmentController(ISender sender) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken) => this.ToSuccessResponse(await sender.Send(new GetDepartmentsQuery(), cancellationToken), "Lấy danh sách Department thành công.");

    [HttpGet("{departmentId:guid}")]
    public async Task<IActionResult> GetDepartment(Guid departmentId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GetDepartmentQuery(departmentId), cancellationToken));

    [HttpGet("{departmentId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid departmentId, CancellationToken cancellationToken) => this.ToSuccessResponse(await sender.Send(new GetDepartmentMembersQuery(departmentId), cancellationToken), "Lấy danh sách thành viên Department thành công.");

    [HttpPost]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new CreateDepartmentCommand(request.Code, request.Name, request.ParentDepartmentId, request.ManagerUserId), cancellationToken));

    [HttpPost("{departmentId:guid}/archive")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ArchiveDepartment(Guid departmentId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new ArchiveDepartmentCommand(departmentId), cancellationToken));

    [HttpPut("{departmentId:guid}/members/{userId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> AddMember(Guid departmentId, Guid userId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new AddDepartmentMemberCommand(departmentId, userId), cancellationToken));

    [HttpPut("{departmentId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateDepartment(Guid departmentId, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new UpdateDepartmentCommand(departmentId, request.Name, request.ParentDepartmentId, request.ManagerUserId), cancellationToken));

    [HttpDelete("{departmentId:guid}/members/{userId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> RemoveMember(Guid departmentId, Guid userId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new RemoveDepartmentMemberCommand(departmentId, userId), cancellationToken));

}
