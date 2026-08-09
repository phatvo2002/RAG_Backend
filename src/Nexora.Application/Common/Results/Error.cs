namespace Nexora.Application.Common.Results;

public sealed record Error(string Code, string Message)
{
    public static Error Validation(string message) => new("validation.error", message);

    public static Error Unauthorized(string message) => new("authorization.unauthorized", message);

    public static Error Forbidden(string message) => new("authorization.forbidden", message);

    public static Error NotFound(string message) => new("resource.not_found", message);

    public static Error Conflict(string message) => new("conflict", message);
}
