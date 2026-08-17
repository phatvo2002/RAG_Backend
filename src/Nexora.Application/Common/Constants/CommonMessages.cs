namespace Nexora.Application.Common.Constants;

public static class CommonMessages
{
    public static class ErrorCodes
    {
        public const string Validation = "validation.error";
        public const string Unauthorized = "authorization.unauthorized";
        public const string Forbidden = "authorization.forbidden";
        public const string NotFound = "resource.not_found";
        public const string Conflict = "conflict";
        public const string Server = "server.error";
        public const string ConcurrencyConflict = "concurrency.conflict";
        public const string RateLimitExceeded = "rate_limit.exceeded";
        public const string IdentityOperationFailed = "identity.operation_failed";
        public const string AuthPrefix = "auth.";
        public const string InvalidCredentials = "auth.invalid_credentials";
        public const string InvalidRefreshToken = "auth.invalid_refresh_token";
        public const string UserNotFound = "auth.user_not_found";
    }

    public static class Auth
    {
        public const string InvalidCredentials = "Email hoặc mật khẩu không đúng.";
        public const string InvalidRefreshToken = "Refresh token không hợp lệ hoặc đã hết hạn.";
        public const string UserNotFound = "Tài khoản không còn tồn tại hoặc đã bị vô hiệu hóa.";
        public const string LoginRequiredToViewCurrentUser = "Cần đăng nhập để xem thông tin tài khoản.";
        public const string UserNotValid = "Tài khoản không còn hợp lệ.";
    }

    public static class User
    {
        public const string NotFound = "User không tồn tại.";
        public const string DefaultOrganizationNotConfigured = "Chưa cấu hình Organization mặc định.";
        public const string CannotCreate = "Không thể tạo User.";
        public const string CannotUpdate = "Không thể cập nhật User.";
        public const string LoginRequiredToCreate = "Cần đăng nhập để tạo User.";
        public const string LoginRequiredToAssignRole = "Cần đăng nhập để gán Role.";
        public const string LoginRequiredToRevokeRole = "Cần đăng nhập để thu hồi Role.";
        public const string RoleOrOrganizationNotFound = "User, Role hoặc Organization không tồn tại.";
        public const string RoleAlreadyAssigned = "Role đã được gán cho User.";
        public const string RoleAssignmentNotFound = "Role assignment không tồn tại.";
    }

    public static class Role
    {
        public const string NotFound = "Role không tồn tại.";
        public const string CannotCreate = "Không thể tạo Role.";
        public const string CannotUpdate = "Không thể cập nhật Role.";
        public const string LoginRequiredToGrantPermission = "Cần đăng nhập để cấp permission.";
        public const string LoginRequiredToRevokePermission = "Cần đăng nhập để thu hồi permission.";
        public const string RoleOrganizationFunctionNotFound = "Role, Organization hoặc ApiFunction không tồn tại.";
        public const string OrganizationFunctionNotFound = "Organization hoặc ApiFunction không tồn tại.";
        public const string PermissionAlreadyGranted = "Role đã được cấp permission này.";
        public const string PermissionCannotBeGrantedAgain = "Permission đã từng được cấp và không thể cấp lại trong cùng assignment.";
        public const string PermissionNotFound = "Role permission không tồn tại.";
    }

    public static class ApiFunction
    {
        public const string NotFound = "ApiFunction không tồn tại.";
        public const string CodeAlreadyExists = "ApiFunction code đã tồn tại.";
        public const string RouteAlreadyExists = "HTTP method và route template của ApiFunction đã tồn tại.";
        public const string CannotUpdate = "Không thể cập nhật ApiFunction.";
    }

    public static class Department
    {
        public const string NotFound = "Department không tồn tại.";
        public const string CodeAlreadyExists = "Department code đã tồn tại.";
        public const string ParentNotFound = "Parent Department không tồn tại.";
        public const string ParentInvalid = "Parent Department không hợp lệ.";
        public const string ManagerNotFoundOrInactive = "Manager User không tồn tại hoặc đã bị vô hiệu hóa.";
        public const string DepartmentOrUserNotFound = "Department hoặc User không tồn tại.";
        public const string UserAlreadyMember = "User đã thuộc Department.";
        public const string LoginRequiredToRemoveMember = "Cần đăng nhập để xóa thành viên Department.";
        public const string MembershipNotFound = "Department membership không tồn tại.";
    }

    public static class Organization
    {
        public const string NotFound = "Organization không tồn tại.";
        public const string SlugAlreadyExists = "Organization slug đã tồn tại.";
    }

    public static class Infrastructure
    {
        public const string ConnectionStringRequired = "Connection string 'Postgres' is required. Configure ConnectionStrings:Postgres or ConnectionStrings__Postgres.";
        public const string DesignTimeConnectionStringRequired = "Connection string 'Postgres' is required for EF Core design-time operations.";
        public const string WebApiConfigurationDirectoryNotFound = "Could not locate the Nexora.WebApi configuration directory.";
        public const string JwtIssuerAudienceRequired = "JWT issuer and audience are required.";
        public const string JwtConfigurationInvalid = "JWT configuration is invalid.";
        public const string JwtSigningKeyTooShort = "JWT signature private key must contain at least 32 characters.";
        public const string JwtAccessTokenLifetimeInvalid = "JWT access token lifetime must be between 1 and 1440 minutes.";
        public const string JwtRefreshTokenLifetimeInvalid = "JWT refresh token lifetime must be between 1 and 365 days.";
        public const string PageSizeInvalid = "Page size must be between 1 and 1000.";
        public const string AuthenticatedUserRequiredForSoftDelete = "An authenticated user is required for soft delete.";
        public const string AuthenticatedUserContextRequired = "Authenticated user context is required.";
        public const string OrganizationContextRequired = "Organization context is required.";
        public const string PropertyNotMappedTemplate = "Property '{0}' is not a mapped scalar property of {1}.";
        public const string IdentityOperationFailed = "Không thể thực hiện thao tác Identity.";
    }

    public static class Api
    {
        public const string AuthenticationRequired = "Cần đăng nhập để truy cập API này.";
        public const string EndpointNotFound = "Không tìm thấy API được yêu cầu.";
        public const string MethodNotAllowed = "HTTP method không được hỗ trợ cho API này.";
        public const string TooManyRequests = "Quá nhiều yêu cầu. Vui lòng thử lại sau.";
        public const string RequestValidationFailed = "Request validation failed.";
        public const string Forbidden = "The requested operation is not permitted.";
        public const string ConcurrencyConflict = "The resource was changed by another operation.";
        public const string UnexpectedError = "An unexpected error occurred.";
        public const string DefaultError = "Không thể xử lý yêu cầu.";
        public const string PermissionNotGrantedTemplate = "Permission '{0}' was not granted.";
        public const string ApiFunctionCatalogConflictTemplate = "ApiFunction code '{0}' đã tồn tại nhưng route hoặc HTTP method không khớp catalog.";
        public const string UnhandledRequestLogTemplate = "Unhandled exception while processing request {TraceIdentifier}.";
    }

    public static class TokenReasons
    {
        public const string UserLogout = "user_logout";
        public const string UserDeactivated = "user_deactivated";
        public const string RefreshTokenReuseDetected = "refresh_token_reuse_detected";
        public const string RefreshTokenExpired = "refresh_token_expired";
        public const string RefreshTokenIdentityInvalid = "refresh_token_identity_invalid";
        public const string RefreshTokenRotated = "refresh_token_rotated";
    }

    public static class IdentityCodes
    {
        public const string DuplicateUserName = "DuplicateUserName";
        public const string DuplicateEmail = "DuplicateEmail";
        public const string DuplicateRoleName = "DuplicateRoleName";
    }
}
