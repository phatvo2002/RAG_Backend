using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Application.Features.V1.Account;
using Nexora.Domain.Common.Exceptions;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Controllers.V1.Chat;

[ApiController]
[Route("api/account")]
public sealed class AccountController(ISender sender) : BaseController
{
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken ct) => Ok(await sender.Send(new GetAccountProfileQuery(), ct));

    [HttpPut("profile")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        try { return Ok(await sender.Send(new UpdateAccountProfileCommand(request.DisplayName, request.PreferredLanguage, request.TimeZone), ct)); }
        catch (ArgumentException) { return BadRequest(new { message = "Thông tin cá nhân không hợp lệ." }); }
        catch (DomainException) { return BadRequest(new { message = "Thông tin cá nhân không hợp lệ." }); }
    }

    [HttpGet("provider-keys")]
    public async Task<IActionResult> ProviderKeys(CancellationToken ct) => Ok(await sender.Send(new GetProviderKeysQuery(), ct));

    [HttpPut("provider-keys/{provider}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> SaveProviderKey(string provider, [FromBody] SaveProviderKeyRequest request, CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(new SaveProviderKeyCommand(provider, request.ApiKey, request.EndpointId), ct);
            return result is null
                ? StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Chưa cấu hình khóa mã hóa API trên máy chủ." })
                : Ok(result);
        }
        catch (ArgumentException) { return BadRequest(new { message = "Khóa API hoặc điểm kết nối không hợp lệ." }); }
    }

    [HttpDelete("provider-keys/{provider}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DeleteProviderKey(string provider, CancellationToken ct)
    {
        try { return await sender.Send(new DeleteProviderKeyCommand(provider), ct) ? NoContent() : NotFound(); }
        catch (ArgumentException) { return BadRequest(new { message = "Nhà cung cấp không hợp lệ." }); }
    }
}

public sealed record UpdateProfileRequest(string DisplayName, string PreferredLanguage, string? TimeZone);
public sealed record SaveProviderKeyRequest(string ApiKey, string? EndpointId);
