using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Constants;
using Nexora.WebApi.Responses;

namespace Nexora.WebApi.Middleware;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireApiFunctionAttribute : TypeFilterAttribute
{
    public RequireApiFunctionAttribute(params string[] codes) : base(typeof(ApiFunctionAuthorizationFilter))
    {
        Arguments = new object[] { codes };
    }
}

internal sealed class ApiFunctionAuthorizationFilter(
    string[] codes,
    IUnitOfWork unitOfWork,
    IDefaultOrganizationProvider organizationProvider,
    IUserContext userContext,
    IClock clock) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        if (httpContext.User.Identity?.IsAuthenticated != true || !userContext.UserId.HasValue)
        {
            context.Result = new JsonResult(ApiResponseFactory.Failure(httpContext, StatusCodes.Status401Unauthorized,
                CommonMessages.Api.AuthenticationRequired, CommonMessages.ErrorCodes.Unauthorized))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(httpContext.RequestAborted);
        var functions = organizationId.HasValue
            ? await unitOfWork.ApiFunctionRepository.GetEffectiveByUserAsync(organizationId.Value, userContext.UserId.Value, clock.UtcNow, httpContext.RequestAborted)
            : [];
        if (!functions.Any(function => codes.Contains(function.Code.Value, StringComparer.OrdinalIgnoreCase)))
        {
            context.Result = new JsonResult(ApiResponseFactory.Failure(httpContext, StatusCodes.Status403Forbidden,
                CommonMessages.Api.Forbidden, CommonMessages.ErrorCodes.Forbidden))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
