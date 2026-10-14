namespace Nexora.Application.Common.Abstractions.Auth;

public class IdentityUserSnapshot
{
    public IdentityUserSnapshot()
    {
    }

    public IdentityUserSnapshot(Guid userId, string email, string userName, string? phoneNumber = null, bool isActive = true)
    {
        UserId = userId;
        Email = email;
        UserName = userName;
        PhoneNumber = phoneNumber;
        IsActive = isActive;
    }

    public Guid UserId { get; set; }
    public Guid Id => UserId;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}

public class IdentityRoleSnapshot
{
    public IdentityRoleSnapshot()
    {
    }

    public IdentityRoleSnapshot(Guid roleId, string name, bool isActive)
    {
        RoleId = roleId;
        Name = name;
        IsActive = isActive;
    }

    public Guid RoleId { get; set; }
    public Guid Id => RoleId;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
