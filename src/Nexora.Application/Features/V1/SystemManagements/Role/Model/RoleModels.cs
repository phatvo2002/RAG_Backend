using Nexora.Domain.Authorization.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Model;

public class RoleResponse
{
    public RoleResponse()
    {
    }

    public RoleResponse(Guid roleId, string name, bool isActive)
    {
        RoleId = roleId;
        Name = name;
        IsActive = isActive;
    }

    public Guid RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class RolePermissionResponse
{
    public RolePermissionResponse()
    {
    }

    public RolePermissionResponse(Guid functionId, string effect, DateTimeOffset validFrom, DateTimeOffset? expiresAt, DateTimeOffset? revokedAt)
    {
        FunctionId = functionId;
        Effect = effect;
        ValidFrom = validFrom;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
    }

    public Guid FunctionId { get; set; }
    public string Effect { get; set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public class CreateRoleRequest
{
    public CreateRoleRequest()
    {
    }

    public CreateRoleRequest(string name)
    {
        Name = name;
    }

    public string Name { get; set; } = string.Empty;
}

public class UpdateRoleRequest
{
    public UpdateRoleRequest()
    {
    }

    public UpdateRoleRequest(string name)
    {
        Name = name;
    }

    public string Name { get; set; } = string.Empty;
}

public class GrantRolePermissionRequest
{
    public GrantRolePermissionRequest()
    {
    }

    public GrantRolePermissionRequest(PermissionEffect effect = PermissionEffect.Allow, DateTimeOffset? validFrom = null, DateTimeOffset? expiresAt = null)
    {
        Effect = effect;
        ValidFrom = validFrom;
        ExpiresAt = expiresAt;
    }

    public PermissionEffect Effect { get; set; } = PermissionEffect.Allow;
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public sealed class SetRolePermissionsRequest
{
    public IReadOnlyCollection<Guid> FunctionIds { get; set; } = Array.Empty<Guid>();
}

public sealed record RolePermissionsResponse(Guid RoleId, IReadOnlyCollection<Guid> FunctionIds);
