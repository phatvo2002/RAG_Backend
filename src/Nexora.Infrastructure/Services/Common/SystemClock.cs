using Nexora.Application.Common.Abstractions;

namespace Nexora.Infrastructure.Services.Common;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
