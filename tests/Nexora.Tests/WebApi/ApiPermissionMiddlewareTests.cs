using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Nexora.Application.Common.Permissions;
using Nexora.WebApi.Controllers;
using Nexora.WebApi.Middleware;

namespace Nexora.Tests.WebApi;

public sealed class ApiPermissionMiddlewareTests
{
    [Fact]
    public async Task PublicController_ShouldSkipPermissionChecker()
    {
        var checker = new TestPermissionChecker(false);
        var nextCalls = 0;
        var middleware = new ApiPermissionMiddleware(_ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });
        var context = CreateContext(typeof(PublicController), new HttpMethodMetadata(["GET"]));

        await middleware.InvokeAsync(context, checker);

        Assert.Equal(1, nextCalls);
        Assert.Null(checker.Request);
    }

    [Fact]
    public async Task ProtectedController_WhenAllowed_ShouldUseCanonicalRouteAndContinue()
    {
        var checker = new TestPermissionChecker(true);
        var nextCalls = 0;
        var middleware = new ApiPermissionMiddleware(_ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });
        var context = CreateContext(typeof(ProtectedController), new HttpMethodMetadata(["GET"]));

        await middleware.InvokeAsync(context, checker);

        Assert.Equal(1, nextCalls);
        Assert.Equal(new ApiRequestDescriptor("GET", "/api/users/{userId:guid}"), checker.Request);
    }

    [Fact]
    public async Task ProtectedController_WhenDenied_ShouldStopRequest()
    {
        var checker = new TestPermissionChecker(false);
        var nextCalls = 0;
        var middleware = new ApiPermissionMiddleware(_ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });
        var context = CreateContext(typeof(ProtectedController), new HttpMethodMetadata(["GET"]));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => middleware.InvokeAsync(context, checker));

        Assert.Equal(0, nextCalls);
        Assert.Equal(new ApiRequestDescriptor("GET", "/api/users/{userId:guid}"), checker.Request);
    }

    private static DefaultHttpContext CreateContext(Type controllerType, params object[] metadata)
    {
        var descriptor = new ControllerActionDescriptor { ControllerTypeInfo = controllerType.GetTypeInfo() };
        var endpointMetadata = new List<object> { descriptor };
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

    private sealed class PublicController : ControllerBase;

    private sealed class ProtectedController : BaseController;

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
