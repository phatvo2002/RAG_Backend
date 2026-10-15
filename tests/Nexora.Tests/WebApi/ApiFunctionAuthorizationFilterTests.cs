using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;
using Nexora.WebApi.Middleware;

namespace Nexora.Tests.WebApi;

public sealed class ApiFunctionAuthorizationFilterTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Attribute_ResolvesFilterWithRequiredCodes()
    {
        var services = new ServiceCollection();
        services.AddSingleton(StubProxy<IUnitOfWork>.Create((method, _) => throw new NotSupportedException(method.Name)));
        services.AddSingleton<IDefaultOrganizationProvider>(new TestOrganizationProvider());
        services.AddSingleton<IUserContext>(new TestUserContext(UserId));
        services.AddSingleton<IClock>(new TestClock());
        using var provider = services.BuildServiceProvider();

        var filter = new RequireApiFunctionAttribute("ORGANIZATION.READ", "USER.UPDATE").CreateInstance(provider);

        Assert.IsType<ApiFunctionAuthorizationFilter>(filter);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ReturnsUnauthorized()
    {
        var filter = new ApiFunctionAuthorizationFilter(["ORGANIZATION.READ"], null!, null!, new TestUserContext(null), new TestClock());
        var context = CreateContext(authenticated: false);

        await filter.OnAuthorizationAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, Assert.IsType<JsonResult>(context.Result).StatusCode);
    }

    [Fact]
    public async Task MissingGrant_ReturnsForbidden()
    {
        var filter = CreateFilter([]);
        var context = CreateContext(authenticated: true);

        await filter.OnAuthorizationAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<JsonResult>(context.Result).StatusCode);
    }

    [Fact]
    public async Task AnyRequiredGrant_AllowsRequest()
    {
        var filter = CreateFilter([CreateFunction("USER.UPDATE")]);
        var context = CreateContext(authenticated: true);

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    private static ApiFunctionAuthorizationFilter CreateFilter(List<ApiFunction> grants)
    {
        var repository = StubProxy<IApiFunctionRepository>.Create((method, _) =>
            method.Name == nameof(IApiFunctionRepository.GetEffectiveByUserAsync)
                ? Task.FromResult(grants)
                : throw new NotSupportedException(method.Name));
        var unitOfWork = StubProxy<IUnitOfWork>.Create((method, _) =>
            method.Name == "get_ApiFunctionRepository" ? repository : throw new NotSupportedException(method.Name));
        return new ApiFunctionAuthorizationFilter(["ORGANIZATION.READ", "USER.UPDATE"], unitOfWork,
            new TestOrganizationProvider(), new TestUserContext(UserId), new TestClock());
    }

    private static ApiFunction CreateFunction(string code) => ApiFunction.Create(
        code, "Organization", code, ApiHttpMethod.Get, "/api/organizations/current",
        ApiActionType.Read, ApiRiskLevel.Low, Now);

    private static AuthorizationFilterContext CreateContext(bool authenticated)
    {
        var httpContext = new DefaultHttpContext();
        if (authenticated)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())], "test"));
        }
        return new AuthorizationFilterContext(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), []);
    }

    private sealed class TestOrganizationProvider : IDefaultOrganizationProvider
    {
        public Task<Guid?> GetOrganizationIdAsync(CancellationToken cancellationToken) => Task.FromResult<Guid?>(OrganizationId);
    }

    private sealed class TestUserContext(Guid? userId) : IUserContext
    {
        public Guid? UserId => userId;
        public bool IsAuthenticated => userId.HasValue;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    public class StubProxy<T> : DispatchProxy where T : class
    {
        private Func<MethodInfo, object?[]?, object?> _handler = null!;

        public static T Create(Func<MethodInfo, object?[]?, object?> handler)
        {
            var proxy = DispatchProxy.Create<T, StubProxy<T>>();
            ((StubProxy<T>)(object)proxy)._handler = handler;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            _handler(targetMethod ?? throw new InvalidOperationException(), args);
    }
}
