using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Auth.Command;
using Nexora.Application.Features.V1.SystemManagements.Auth.Model;
using Nexora.Application.Features.V1.SystemManagements.Auth.Query;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password, request.OrganizationId), cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);

        return NoContent();
    }

    [HttpPost("select-organization")]
    [Authorize]
    public async Task<IActionResult> SelectOrganization([FromBody] SelectOrganizationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SelectOrganizationCommand(request.OrganizationId), cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentUserQuery(), cancellationToken);

        return ToActionResult(result);
    }

    private IActionResult ToActionResult<TValue>(Result<TValue> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var error = result.Error ?? new Error("server.error", "Không thể xử lý yêu cầu.");
        var statusCode = error.Code switch
        {
            "validation.error" => StatusCodes.Status400BadRequest,
            "authorization.unauthorized" => StatusCodes.Status401Unauthorized,
            "auth.invalid_credentials" => StatusCodes.Status401Unauthorized,
            "auth.invalid_refresh_token" => StatusCodes.Status401Unauthorized,
            "authorization.forbidden" => StatusCodes.Status403Forbidden,
            "auth.organization_access_denied" => StatusCodes.Status403Forbidden,
            "auth.organization_required" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            statusCode: statusCode,
            title: error.Message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code
            });
    }
}
