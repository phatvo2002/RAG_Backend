using System.Text.Json.Serialization;

namespace Nexora.WebApi.Responses;

public class ApiResponse<T>
{
    public ApiResponse()
    {
    }

    public ApiResponse(bool success, int statusCode, string message, T? data, ApiError? error, ApiMeta meta)
    {
        Success = success;
        StatusCode = statusCode;
        Message = message;
        Data = data;
        Error = error;
        Meta = meta;
    }

    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiError? Error { get; set; }

    public ApiMeta Meta { get; set; } = new();
}

public class ApiError
{
    public ApiError()
    {
    }

    public ApiError(string code, IReadOnlyList<ApiErrorDetail>? details = null)
    {
        Code = code;
        Details = details;
    }

    public string Code { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ApiErrorDetail>? Details { get; set; }
}

public class ApiErrorDetail
{
    public ApiErrorDetail()
    {
    }

    public ApiErrorDetail(string field, string message)
    {
        Field = field;
        Message = message;
    }

    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ApiMeta
{
    public ApiMeta()
    {
    }

    public ApiMeta(DateTime timestamp, string path, string requestId)
    {
        Timestamp = timestamp;
        Path = path;
        RequestId = requestId;
    }

    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}
