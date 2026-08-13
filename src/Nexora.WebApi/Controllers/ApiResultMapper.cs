using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;
using Nexora.WebApi.Responses;

namespace Nexora.WebApi.Controllers;

internal static class ApiResultMapper
{
    private const string DefaultSuccessMessage = "Yêu cầu đã được xử lý thành công.";

    public static IActionResult ToActionResult(this ControllerBase controller, Result result, string successMessage = DefaultSuccessMessage)
    {
        return result.IsSuccess ? Success(controller, successMessage) : Failure(controller, result.Error);
    }

    public static IActionResult ToActionResult<TValue>(this ControllerBase controller, Result<TValue> result, string successMessage = DefaultSuccessMessage)
    {
        return result.IsSuccess ? Success(controller, result.Value, successMessage) : Failure(controller, result.Error);
    }

    public static IActionResult ToSuccessResponse<TValue>(this ControllerBase controller, TValue data, string message = DefaultSuccessMessage) =>
        Success(controller, data, message);

    public static IActionResult ToSuccessResponse(this ControllerBase controller, string message = DefaultSuccessMessage) =>
        Success(controller, message);

    private static IActionResult Success(ControllerBase controller, string message)
    {
        const int statusCode = StatusCodes.Status200OK;
        return controller.StatusCode(statusCode, ApiResponseFactory.Success(controller.HttpContext, statusCode, message));
    }

    private static IActionResult Success<TValue>(ControllerBase controller, TValue? data, string message)
    {
        const int statusCode = StatusCodes.Status200OK;
        return controller.StatusCode(statusCode, ApiResponseFactory.Success(controller.HttpContext, statusCode, message, data));
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
            CommonMessages.ErrorCodes.ConcurrencyConflict => StatusCodes.Status409Conflict,
            CommonMessages.ErrorCodes.Server => StatusCodes.Status500InternalServerError,
            _ when error.Code.StartsWith(CommonMessages.ErrorCodes.AuthPrefix, StringComparison.Ordinal) => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };

        return controller.StatusCode(statusCode, ApiResponseFactory.Failure(controller.HttpContext, statusCode, error.Message, error.Code));
    }
}
