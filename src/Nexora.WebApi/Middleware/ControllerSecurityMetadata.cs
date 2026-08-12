using Microsoft.AspNetCore.Mvc.Controllers;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Middleware;

internal static class ControllerSecurityMetadata
{
    public static bool RequiresSecurity(HttpContext context)
    {
        var descriptor = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        return descriptor is not null && typeof(BaseController).IsAssignableFrom(descriptor.ControllerTypeInfo.AsType());
    }
}
