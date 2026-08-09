namespace Nexora.Application.Common.Results;

public sealed class Result : IApplicationResult
{
    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error ?? throw new ArgumentNullException(nameof(error)));
}
