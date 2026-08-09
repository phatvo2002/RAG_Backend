using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Features.V1.SystemManagements.Role.Command;
using Nexora.Application.Features.V1.SystemManagements.Role.Model;
using Nexora.Application.Features.V1.SystemManagements.Role.Query;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[ApiController]
[Authorize]
[Route("api/roles")]
public sealed class RoleController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoles([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) => Ok(await sender.Send(new GetRolesQuery(page, pageSize), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new CreateRoleCommand(request.Name), cancellationToken));

    [HttpGet("{roleId:guid}")]
    public async Task<IActionResult> GetRole(Guid roleId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GetRoleQuery(roleId), cancellationToken));

    [HttpPut("{roleId:guid}")]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new UpdateRoleCommand(roleId, request.Name), cancellationToken));

    [HttpPost("{roleId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateRole(Guid roleId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new DeactivateRoleCommand(roleId), cancellationToken));

    [HttpPost("{roleId:guid}/reactivate")]
    public async Task<IActionResult> ReactivateRole(Guid roleId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new ReactivateRoleCommand(roleId), cancellationToken));

    [HttpPut("{roleId:guid}/permissions/{functionCode}")]
    public async Task<IActionResult> GrantPermission(Guid roleId, string functionCode, [FromBody] GrantRolePermissionRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GrantRolePermissionCommand(roleId, functionCode, request.Effect, request.ValidFrom, request.ExpiresAt), cancellationToken));

    [HttpDelete("{roleId:guid}/permissions/{functionCode}")]
    public async Task<IActionResult> RevokePermission(Guid roleId, string functionCode, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new RevokeRolePermissionCommand(roleId, functionCode), cancellationToken));

    [HttpGet("{roleId:guid}/permissions")]
    public async Task<IActionResult> GetPermissions(Guid roleId, CancellationToken cancellationToken) => Ok(await sender.Send(new GetRolePermissionsQuery(roleId), cancellationToken));
}
