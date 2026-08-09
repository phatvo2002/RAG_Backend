using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;

namespace Nexora.WebApi.Controllers;

internal static class ApiResultMapper
{
    public static IActionResult ToActionResult(this ControllerBase controller, Result result)
    {
        return result.IsSuccess ? controller.NoContent() : Failure(controller, result.Error);
    }

    public static IActionResult ToActionResult<TValue>(this ControllerBase controller, Result<TValue> result)
    {
        return result.IsSuccess ? controller.Ok(result.Value) : Failure(controller, result.Error);
    }

    private static IActionResult Failure(ControllerBase controller, Error? error)
    {
        error ??= new Error(CommonMessages.ErrorCodes.Server, CommonMessages.Api.DefaultError);
        var statusCode = error.Code switch
        {
            CommonMessages.ErrorCodes.Validation => StatusCodes.Status400BadRequest,
            CommonMessages.ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            CommonMessages.ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            CommonMessages.ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            CommonMessages.ErrorCodes.Conflict => StatusCodes.Status409Conflict,
            _ when error.Code.StartsWith(CommonMessages.ErrorCodes.AuthPrefix, StringComparison.Ordinal) => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };

        return controller.Problem(statusCode: statusCode, title: error.Message, extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
