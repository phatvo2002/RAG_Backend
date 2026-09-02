using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;
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

public sealed class RoleDetailResponse
{
    public RoleDetailResponse()
    {
    }

    public RoleDetailResponse(Guid roleId, string name, bool isActive, IReadOnlyCollection<ApiFunctionResponse> apiFunctions, IReadOnlyCollection<RoleUserResponse> users)
    {
        RoleId = roleId;
        Name = name;
        IsActive = isActive;
        ApiFunctions = apiFunctions;
        Users = users;
    }

    public Guid RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IReadOnlyCollection<ApiFunctionResponse> ApiFunctions { get; set; } = Array.Empty<ApiFunctionResponse>();
    public IReadOnlyCollection<RoleUserResponse> Users { get; set; } = Array.Empty<RoleUserResponse>();
}

public sealed class RoleUserResponse
{
    public RoleUserResponse()
    {
    }

    public RoleUserResponse(Guid userId, string email, string userName, string? phoneNumber, bool isActive)
    {
        UserId = userId;
        Email = email;
        UserName = userName;
        PhoneNumber = phoneNumber;
        IsActive = isActive;
    }

    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
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
