using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Domain.Common.Exceptions;
using Nexora.Infrastructure.Rag;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Controllers.V1.Chat;

[ApiController]
[Route("api/account")]
public sealed class AccountController(AccountSettingsService settings, ProviderKeyCipher cipher) : BaseController
{
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken ct) => Ok(await settings.GetProfileAsync(ct));

    [HttpPut("profile")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        try { return Ok(await settings.UpdateProfileAsync(request.DisplayName, request.PreferredLanguage, request.TimeZone, ct)); }
        catch (ArgumentException) { return BadRequest(new { message = "Thông tin cá nhân không hợp lệ." }); }
        catch (DomainException) { return BadRequest(new { message = "Thông tin cá nhân không hợp lệ." }); }
    }

    [HttpGet("provider-keys")]
    public async Task<IActionResult> ProviderKeys(CancellationToken ct) => Ok(await settings.ListKeysAsync(ct));

    [HttpPut("provider-keys/{provider}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> SaveProviderKey(string provider, [FromBody] SaveProviderKeyRequest request, CancellationToken ct)
    {
        if (!cipher.IsConfigured) return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new { message = "Chưa cấu hình khóa mã hóa API trên máy chủ." });
        try { return Ok(await settings.SaveKeyAsync(provider, request.ApiKey, request.EndpointId, ct)); }
        catch (ArgumentException) { return BadRequest(new { message = "Khóa API hoặc điểm kết nối không hợp lệ." }); }
    }

    [HttpDelete("provider-keys/{provider}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DeleteProviderKey(string provider, CancellationToken ct)
    {
        try { return await settings.DeleteKeyAsync(provider, ct) ? NoContent() : NotFound(); }
        catch (ArgumentException) { return BadRequest(new { message = "Nhà cung cấp không hợp lệ." }); }
    }
}

public sealed record UpdateProfileRequest(string DisplayName, string PreferredLanguage, string? TimeZone);
public sealed record SaveProviderKeyRequest(string ApiKey, string? EndpointId);
