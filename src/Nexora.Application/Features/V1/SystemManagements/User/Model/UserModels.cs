namespace Nexora.Application.Features.V1.SystemManagements.User.Model;

public class UserResponse
{
    public UserResponse()
    {
    }

    public UserResponse(Guid userId, string email, string userName, string? phoneNumber, bool isActive, string? displayName, string? preferredLanguage, string? timeZone, IReadOnlyCollection<UserRoleResponse>? roles = null)
    {
        UserId = userId;
        Email = email;
        UserName = userName;
        PhoneNumber = phoneNumber;
        IsActive = isActive;
        DisplayName = displayName;
        PreferredLanguage = preferredLanguage;
        TimeZone = timeZone;
        Roles = roles ?? Array.Empty<UserRoleResponse>();
    }

    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public string? DisplayName { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? TimeZone { get; set; }
    public IReadOnlyCollection<UserRoleResponse> Roles { get; set; } = Array.Empty<UserRoleResponse>();
}

public class UserRoleResponse
{
    public UserRoleResponse()
    {
    }

    public UserRoleResponse(Guid roleId, string roleName, string status, DateTimeOffset validFrom, DateTimeOffset? expiresAt)
    {
        RoleId = roleId;
        RoleName = roleName;
        Status = status;
        ValidFrom = validFrom;
        ExpiresAt = expiresAt;
    }

    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class CreateUserRequest
{
    public CreateUserRequest()
    {
    }

    public CreateUserRequest(string email, string userName, string password, string? phoneNumber, string displayName, string preferredLanguage = "vi", string? timeZone = null)
    {
        Email = email;
        UserName = userName;
        Password = password;
        PhoneNumber = phoneNumber;
        DisplayName = displayName;
        PreferredLanguage = preferredLanguage;
        TimeZone = timeZone;
    }

    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "vi";
    public string? TimeZone { get; set; }
}

public class UpdateUserRequest
{
    public UpdateUserRequest()
    {
    }

    public UpdateUserRequest(string email, string userName, string? phoneNumber, string displayName, string preferredLanguage = "vi", string? timeZone = null)
    {
        Email = email;
        UserName = userName;
        PhoneNumber = phoneNumber;
        DisplayName = displayName;
        PreferredLanguage = preferredLanguage;
        TimeZone = timeZone;
    }

    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "vi";
    public string? TimeZone { get; set; }
}

public class ResetPasswordRequest
{
    public ResetPasswordRequest()
    {
    }

    public ResetPasswordRequest(string newPassword)
    {
        NewPassword = newPassword;
    }

    public string NewPassword { get; set; } = string.Empty;
}
