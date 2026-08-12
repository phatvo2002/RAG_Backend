namespace Nexora.WebApi.Responses;

internal static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(HttpContext context, int statusCode, string message, T? data) =>
        new(true, statusCode, message, data, null, CreateMeta(context));

    public static ApiResponse<object?> Success(HttpContext context, int statusCode, string message) =>
        new(true, statusCode, message, null, null, CreateMeta(context));

    public static ApiResponse<object?> Failure(HttpContext context, int statusCode, string message, string code, IReadOnlyList<ApiErrorDetail>? details = null) =>
        new(false, statusCode, message, null, new ApiError(code, details), CreateMeta(context));

    private static ApiMeta CreateMeta(HttpContext context) =>
        new(DateTime.UtcNow, context.Request.Path.Value ?? string.Empty, context.TraceIdentifier);
}
