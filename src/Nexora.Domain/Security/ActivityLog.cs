using System.Net;
using System.Text.Json;
using Nexora.Domain.Common;
using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Security;

public class ActivityLog : EntityBase
{
    protected ActivityLog()
    {
    }

    private ActivityLog(Guid id)
        : base(id)
    {
    }

    public Guid? OrganizationId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public Guid? ApiFunctionId { get; private set; }

    public string Module { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string ResourceType { get; private set; } = string.Empty;

    public Guid? ResourceId { get; private set; }

    public string Result { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public IPAddress? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? CorrelationId { get; private set; }

    public JsonDocument? MetadataJson { get; private set; }

    public string? PreviousLogHash { get; private set; }

    public string? LogHash { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static ActivityLog Create(Guid id, Guid? organizationId, Guid? actorUserId, Guid? apiFunctionId, string module, string action, string resourceType, string result, string? description, IPAddress? ipAddress, JsonDocument? metadataJson, DateTimeOffset occurredAt)
    {
        return new ActivityLog(DomainGuard.RequiredId(id, nameof(id)))
        {
            OrganizationId = organizationId,
            ActorUserId = actorUserId,
            ApiFunctionId = apiFunctionId,
            Module = DomainGuard.RequiredText(module, nameof(module), 80),
            Action = DomainGuard.RequiredText(action, nameof(action), 100),
            ResourceType = DomainGuard.RequiredText(resourceType, nameof(resourceType), 80),
            Result = DomainGuard.RequiredText(result, nameof(result), 20),
            Description = description is null ? null : DomainGuard.RequiredText(description, nameof(description), 500),
            IpAddress = ipAddress,
            MetadataJson = metadataJson,
            OccurredAt = DomainGuard.Utc(occurredAt, nameof(occurredAt))
        };
    }
}
