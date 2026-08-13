using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Net.Http.Headers;
using Nexora.Application.Common.Constants;
using Nexora.WebApi.Controllers;
using Nexora.WebApi.Middleware;

namespace Nexora.Tests.WebApi;

public sealed class JwtAuthenticationMiddlewareTests
{
    [Fact]
    public void ProtectedController_ShouldInheritApiControllerAttribute()
    {
        Assert.NotNull(typeof(ProtectedController).GetCustomAttribute<ApiControllerAttribute>(inherit: true));
    }

    [Fact]
    public async Task PublicController_WithoutAuthenticatedUser_ShouldContinue()
    {
        var nextCalls = 0;
        var middleware = new JwtAuthenticationMiddleware(_ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });
        var context = CreateContext(typeof(PublicController));

        await middleware.InvokeAsync(context);

        Assert.Equal(1, nextCalls);
    }

    [Fact]
    public async Task ProtectedController_WithAuthenticatedUser_ShouldContinue()
    {
        var nextCalls = 0;
        var middleware = new JwtAuthenticationMiddleware(_ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });
        var context = CreateContext(typeof(ProtectedController));
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Test"));

        await middleware.InvokeAsync(context);

        Assert.Equal(1, nextCalls);
    }

    [Fact]
    public async Task ProtectedController_WithoutAuthenticatedUser_ShouldReturnUnauthorized()
    {
        var nextCalls = 0;
        var middleware = new JwtAuthenticationMiddleware(_ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });
        var context = CreateContext(typeof(ProtectedController));
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("Bearer", context.Response.Headers[HeaderNames.WWWAuthenticate]);
        Assert.Equal(0, nextCalls);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(StatusCodes.Status401Unauthorized, root.GetProperty("statusCode").GetInt32());
        Assert.Equal(CommonMessages.ErrorCodes.Unauthorized, root.GetProperty("error").GetProperty("code").GetString());
        Assert.True(root.TryGetProperty("meta", out _));
    }

    private static DefaultHttpContext CreateContext(Type controllerType)
    {
        var descriptor = new ControllerActionDescriptor { ControllerTypeInfo = controllerType.GetTypeInfo() };
        var endpoint = new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("api/test"),
            0,
            new EndpointMetadataCollection(descriptor),
            "test");
        var context = new DefaultHttpContext();
        context.SetEndpoint(endpoint);
        context.Request.Method = "GET";
        return context;
    }

    private sealed class PublicController : ControllerBase;

    private sealed class ProtectedController : BaseController;
}
