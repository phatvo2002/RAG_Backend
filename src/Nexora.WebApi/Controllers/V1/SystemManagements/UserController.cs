using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Application.Features.V1.SystemManagements.User.Command;
using Nexora.Application.Features.V1.SystemManagements.User.Model;
using Nexora.Application.Features.V1.SystemManagements.User.Query;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[ApiController]
[Route("api/users")]
public sealed class UserController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) => this.ToSuccessResponse(await sender.Send(new GetUsersQuery(page, pageSize), cancellationToken), "Lấy danh sách User thành công.");

    [HttpPost]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateUserCommand(request.Email, request.UserName, request.Password, request.PhoneNumber, request.DisplayName, request.PreferredLanguage, request.TimeZone), cancellationToken);
        return this.ToActionResult(result, "Tạo User thành công.");
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GetUserQuery(userId), cancellationToken));

    [HttpPut("{userId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateUserCommand(userId, request.Email, request.UserName, request.PhoneNumber, request.DisplayName, request.PreferredLanguage, request.TimeZone), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{userId:guid}/deactivate")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DeactivateUser(Guid userId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new DeactivateUserCommand(userId), cancellationToken));

    [HttpPost("{userId:guid}/reactivate")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ReactivateUser(Guid userId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new ReactivateUserCommand(userId), cancellationToken));

    [HttpPost("{userId:guid}/reset-password")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ResetPassword(Guid userId, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new ResetUserPasswordCommand(userId, request.NewPassword), cancellationToken));

    [HttpPost("{userId:guid}/roles/{roleId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> AssignRole(Guid userId, Guid roleId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new AssignUserRoleCommand(userId, roleId), cancellationToken));

    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> RevokeRole(Guid userId, Guid roleId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new RevokeUserRoleCommand(userId, roleId), cancellationToken));

    [HttpGet("{userId:guid}/roles")]
    public async Task<IActionResult> GetRoles(Guid userId, CancellationToken cancellationToken) => this.ToSuccessResponse(await sender.Send(new GetUserRolesQuery(userId), cancellationToken), "Lấy danh sách Role của User thành công.");
}
