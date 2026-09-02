using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Application.Features.V1.SystemManagements.Role.Command;
using Nexora.Application.Features.V1.SystemManagements.Role.Model;
using Nexora.Application.Features.V1.SystemManagements.Role.Query;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[ApiController]
[Route("api/roles")]
public class RoleController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoles([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) => this.ToSuccessResponse(await sender.Send(new GetRolesQuery(page, pageSize), cancellationToken), "Lấy danh sách Role thành công.");

    [HttpGet("{roleId:guid}")]
    public async Task<IActionResult> GetRole(Guid roleId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GetRoleQuery(roleId), cancellationToken));

    [HttpGet("{roleId:guid}/permissions")]
    public async Task<IActionResult> GetPermissions(Guid roleId, CancellationToken cancellationToken) => this.ToSuccessResponse(await sender.Send(new GetRolePermissionsQuery(roleId), cancellationToken), "Lấy danh sách permission của Role thành công.");

    [HttpPost]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new CreateRoleCommand(request.Name), cancellationToken));

    [HttpPost("{roleId:guid}/deactivate")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DeactivateRole(Guid roleId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new DeactivateRoleCommand(roleId), cancellationToken));

    [HttpPost("{roleId:guid}/reactivate")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ReactivateRole(Guid roleId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new ReactivateRoleCommand(roleId), cancellationToken));

    [HttpPut("{roleId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new UpdateRoleCommand(roleId, request.Name), cancellationToken));

    [HttpPut("{roleId:guid}/permissions/{functionCode}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> GrantPermission(Guid roleId, string functionCode, [FromBody] GrantRolePermissionRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GrantRolePermissionCommand(roleId, functionCode, request.Effect, request.ValidFrom, request.ExpiresAt), cancellationToken));

    [HttpPut("{roleId:guid}/permissions")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> SyncPermissions(Guid roleId, [FromBody] SetRolePermissionsRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new SyncRolePermissionsCommand(roleId, request.FunctionIds), cancellationToken));

    [HttpDelete("{roleId:guid}/permissions/{functionCode}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> RevokePermission(Guid roleId, string functionCode, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new RevokeRolePermissionCommand(roleId, functionCode), cancellationToken));
}
