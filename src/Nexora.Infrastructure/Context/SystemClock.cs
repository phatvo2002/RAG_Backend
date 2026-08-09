using Nexora.Application.Common.Abstractions;

namespace Nexora.Infrastructure.Context;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
