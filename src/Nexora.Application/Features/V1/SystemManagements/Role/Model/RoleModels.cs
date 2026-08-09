using Nexora.Domain.Authorization.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Model;

public sealed record RoleResponse(Guid RoleId, string Name, bool IsActive);

public sealed record RolePermissionResponse(Guid FunctionId, string Effect, DateTimeOffset ValidFrom, DateTimeOffset? ExpiresAt, DateTimeOffset? RevokedAt);

public sealed record CreateRoleRequest(string Name);

public sealed record UpdateRoleRequest(string Name);

public sealed record GrantRolePermissionRequest(PermissionEffect Effect = PermissionEffect.Allow, DateTimeOffset? ValidFrom = null, DateTimeOffset? ExpiresAt = null);
