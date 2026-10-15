namespace Nexora.Application.Common.Abstractions.Logging;

public sealed record ActivityLogEntry(Guid? ActorUserId, Guid? ApiFunctionId, string Module, string Action, string ResourceType, string Result, string? Description, string? IpAddress, string? RouteTemplate, int StatusCode, long ElapsedMilliseconds);

public interface IActivityLogWriter
{
    Task WriteAsync(ActivityLogEntry entry, CancellationToken cancellationToken);
}
