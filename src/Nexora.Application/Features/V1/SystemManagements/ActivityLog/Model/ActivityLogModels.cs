using System.Text.Json;

namespace Nexora.Application.Features.V1.SystemManagements.ActivityLog.Model;

public sealed record ActivityLogResponse(
    Guid Id,
    Guid? ActorUserId,
    string? ActorUserName,
    Guid? ApiFunctionId,
    string Module,
    string Action,
    string ResourceType,
    string Result,
    string? Description,
    string? Ip,
    JsonDocument? MetadataJson,
    DateTimeOffset OccurredAt);
