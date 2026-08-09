using Microsoft.AspNetCore.Http;
using Nexora.Application.Common.Abstractions;

namespace Nexora.Infrastructure.Context;

internal sealed class HttpRequestMetadata(IHttpContextAccessor httpContextAccessor)
    : IHttpRequestMetadata
{
    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
