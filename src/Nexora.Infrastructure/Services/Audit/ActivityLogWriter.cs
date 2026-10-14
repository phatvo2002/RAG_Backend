using System.Text.Json;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Logging;
using Nexora.Domain.Security;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Services.Audit;

internal sealed class ActivityLogWriter(NexoraDbContext dbContext, IClock clock) : IActivityLogWriter
{
    public async Task WriteAsync(ActivityLogEntry entry, CancellationToken cancellationToken)
    {
        var occurredAt = clock.UtcNow;
        var metadata = JsonSerializer.SerializeToDocument(new
        {
            entry.StatusCode,
            entry.ElapsedMilliseconds,
            entry.RouteTemplate
        });
        var ipAddress = System.Net.IPAddress.TryParse(entry.IpAddress, out var parsedIpAddress) ? parsedIpAddress : null;
        var activityLog = ActivityLog.Create(Guid.NewGuid(), null, entry.ActorUserId, entry.ApiFunctionId, entry.Module, entry.Action, entry.ResourceType, entry.Result, entry.Description, ipAddress, metadata, occurredAt);
        dbContext.ActivityLogs.Add(activityLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
