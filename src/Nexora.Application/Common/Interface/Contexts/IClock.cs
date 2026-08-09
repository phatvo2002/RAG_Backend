namespace Nexora.Application.Common.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
