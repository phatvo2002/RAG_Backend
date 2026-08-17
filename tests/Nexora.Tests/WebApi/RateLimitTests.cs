using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nexora.WebApi.Controllers.V1.SystemManagements;
using Nexora.WebApi.RateLimiting;
using System.Threading.RateLimiting;

namespace Nexora.Tests.WebApi;

public sealed class RateLimitTests
{
    [Fact]
    public void PartitionKeys_ShouldUseUserIdForAuthenticatedWriteRequests()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext(userId, IPAddress.Parse("192.0.2.10"));

        Assert.Equal($"global:user:{userId}", RateLimitPartitionKeys.Global(context));
        Assert.Equal($"write:user:{userId}", RateLimitPartitionKeys.Write(context));
        Assert.Equal("auth:ip:192.0.2.10", RateLimitPartitionKeys.Auth(context));
    }

    [Fact]
    public void PartitionKeys_ShouldUseIpForAnonymousRequests()
    {
        var context = CreateContext(null, IPAddress.Parse("192.0.2.11"));

        Assert.Equal("global:ip:192.0.2.11", RateLimitPartitionKeys.Global(context));
        Assert.Equal("write:ip:192.0.2.11", RateLimitPartitionKeys.Write(context));
        Assert.Equal("auth:ip:192.0.2.11", RateLimitPartitionKeys.Auth(context));
    }

    [Fact]
    public async Task GlobalLimiter_ShouldRejectAfter120RequestsInSameWindow()
    {
        using var provider = new ServiceCollection()
            .AddNexoraRateLimiting()
            .BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        var globalLimiter = options.GlobalLimiter ?? throw new InvalidOperationException("Global rate limiter is not configured.");
        var context = CreateContext(null, IPAddress.Parse("192.0.2.12"));
        var leases = new List<RateLimitLease>();

        for (var index = 0; index < 120; index++)
        {
            leases.Add(await globalLimiter.AcquireAsync(context));
        }

        Assert.All(leases, lease => Assert.True(lease.IsAcquired));
        using var rejectedLease = await globalLimiter.AcquireAsync(context);
        Assert.False(rejectedLease.IsAcquired);

        context.Response.Body = new MemoryStream();
        var onRejected = options.OnRejected ?? throw new InvalidOperationException("Rate limit rejection handler is not configured.");
        await onRejected(new OnRejectedContext { HttpContext = context, Lease = rejectedLease }, CancellationToken.None);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(context.Response.Headers.RetryAfter));
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("rate_limit.exceeded", document.RootElement.GetProperty("error").GetProperty("code").GetString());

        foreach (var lease in leases)
        {
            lease.Dispose();
        }
    }

    [Fact]
    public void Controllers_ShouldHaveExpectedNamedPolicies()
    {
        Assert.Equal("auth", GetPolicy<AuthController>(nameof(AuthController.Login)));
        Assert.Equal("auth", GetPolicy<AuthController>(nameof(AuthController.Refresh)));
        Assert.Equal("write", GetPolicy<AuthController>(nameof(AuthController.Logout)));
        Assert.Equal("write", GetPolicy<RoleController>(nameof(RoleController.SyncPermissions)));
    }

    private static string? GetPolicy<TController>(string methodName) =>
        typeof(TController).GetMethod(methodName)?.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName;

    private static DefaultHttpContext CreateContext(Guid? userId, IPAddress ipAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = ipAddress;
        if (userId.HasValue)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())], "Test"));
        }

        return context;
    }
}
