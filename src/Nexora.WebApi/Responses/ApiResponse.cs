using System.Text.Json.Serialization;

namespace Nexora.WebApi.Responses;

public sealed record ApiResponse<T>(
    bool Success,
    int StatusCode,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] T? Data,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ApiError? Error,
    ApiMeta Meta);

public sealed record ApiError(
    string Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<ApiErrorDetail>? Details = null);

public sealed record ApiErrorDetail(string Field, string Message);

public sealed record ApiMeta(DateTime Timestamp, string Path, string RequestId);
