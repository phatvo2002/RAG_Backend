using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Application.Features.V1.SystemManagements.Auth.Command;
using Nexora.Application.Features.V1.SystemManagements.Auth.Model;
using Nexora.Application.Features.V1.SystemManagements.Auth.Query;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentUserQuery(), cancellationToken);

        return this.ToActionResult(result, "Lấy thông tin tài khoản thành công.");
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

        return this.ToActionResult(result, "Đăng nhập thành công.");
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);

        return this.ToActionResult(result, "Làm mới access token thành công.");
    }

    [HttpPost("logout")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);

        return this.ToSuccessResponse("Đăng xuất thành công.");
    }
}
