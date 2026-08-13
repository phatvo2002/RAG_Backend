using Nexora.Application.Common.Constants;

namespace Nexora.Application.Common.Results;

public sealed record Error(string Code, string Message)
{
    public static Error Validation(string message) => new(CommonMessages.ErrorCodes.Validation, message);

    public static Error Unauthorized(string message) => new(CommonMessages.ErrorCodes.Unauthorized, message);

    public static Error Forbidden(string message) => new(CommonMessages.ErrorCodes.Forbidden, message);

    public static Error NotFound(string message) => new(CommonMessages.ErrorCodes.NotFound, message);

    public static Error Conflict(string message) => new(CommonMessages.ErrorCodes.Conflict, message);
}
