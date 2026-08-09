namespace Nexora.Application.Common.Results;

public sealed class Result<TValue> : IApplicationResult
{
    private Result(bool isSuccess, TValue? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public TValue? Value { get; }

    public Error? Error { get; }

    public static Result<TValue> Success(TValue value) => new(true, value, null);

    public static Result<TValue> Failure(Error error) => new(false, default, error ?? throw new ArgumentNullException(nameof(error)));
}
