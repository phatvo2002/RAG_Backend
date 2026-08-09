using System.Net;
using System.Text.Json;
using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Security;

public class ActivityLog : EntityBase
{
    protected ActivityLog()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid ActorUserId { get; private set; }

    public Guid? ApiFunctionId { get; private set; }

    public string Module { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string ResourceType { get; private set; } = string.Empty;

    public Guid? ResourceId { get; private set; }

    public string Result { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public IPAddress? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public JsonDocument? MetadataJson { get; private set; }

    public string? PreviousLogHash { get; private set; }

    public string LogHash { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }
}
