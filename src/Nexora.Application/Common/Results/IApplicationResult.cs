namespace Nexora.Application.Common.Results;

public interface IApplicationResult
{
    bool IsSuccess { get; }

    Error? Error { get; }
}
