using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Nexora.Application.Common.Permissions;
using Nexora.WebApi.Middleware;

namespace Nexora.Tests.WebApi;

public sealed class ApiPermissionMiddlewareTests
{
    [Fact]
    public async Task AllowAnonymousEndpoint_ShouldSkipPermissionChecker()
    {
        var checker = new TestPermissionChecker(false);
        var nextCalls = 0;
        var middleware = new ApiPermissionMiddleware(_ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });
        var context = CreateContext(
            new AllowAnonymousAttribute(),
            new HttpMethodMetadata(["GET"]));

        await middleware.InvokeAsync(context, checker);

        Assert.Equal(1, nextCalls);
        Assert.Null(checker.Request);
    }

    [Fact]
    public async Task AuthorizedEndpoint_ShouldUseCanonicalRouteAndContinueWhenAllowed()
    {
        var checker = new TestPermissionChecker(true);
        var nextCalls = 0;
        var middleware = new ApiPermissionMiddleware(_ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });
        var context = CreateContext(new HttpMethodMetadata(["GET"]));
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Test"));

        await middleware.InvokeAsync(context, checker);

        Assert.Equal(1, nextCalls);
        Assert.Equal(new ApiRequestDescriptor("GET", "/api/users/{userId:guid}"), checker.Request);
    }

    private static DefaultHttpContext CreateContext(params object[] metadata)
    {
        var endpointMetadata = new List<object> { new ControllerActionDescriptor() };
        endpointMetadata.AddRange(metadata);
        var endpoint = new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("api/users/{userId:guid}"),
            0,
            new EndpointMetadataCollection(endpointMetadata),
            "test");
        var context = new DefaultHttpContext();
        context.SetEndpoint(endpoint);
        context.Request.Method = "GET";
        return context;
    }

    private sealed class TestPermissionChecker(bool allowed) : IApiRequestPermissionChecker
    {
        public ApiRequestDescriptor? Request { get; private set; }

        public Task<bool> HasAccessAsync(ApiRequestDescriptor request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(allowed);
        }
    }
}
