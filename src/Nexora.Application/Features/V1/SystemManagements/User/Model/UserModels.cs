namespace Nexora.Application.Features.V1.SystemManagements.User.Model;

public sealed record UserResponse(Guid UserId, string Email, string UserName, string? PhoneNumber, bool IsActive, string? DisplayName, string? PreferredLanguage, string? TimeZone);

public sealed record UserRoleResponse(Guid RoleId, string Status, DateTimeOffset ValidFrom, DateTimeOffset? ExpiresAt);

public sealed record CreateUserRequest(string Email, string UserName, string Password, string? PhoneNumber, string DisplayName, string PreferredLanguage = "vi", string? TimeZone = null);

public sealed record UpdateUserRequest(string Email, string UserName, string? PhoneNumber, string DisplayName, string PreferredLanguage = "vi", string? TimeZone = null);

public sealed record ResetPasswordRequest(string NewPassword);
