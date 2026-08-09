namespace Nexora.Domain.Common.Constants;

public static class DomainMessages
{
    public const string DomainErrorCodeRequired = "Mã lỗi Domain không được để trống.";
    public const string RequiredId = "{0} không được để trống.";
    public const string RequiredText = "{0} là bắt buộc.";
    public const string TextTooLong = "{0} không được vượt quá {1} ký tự.";
    public const string UtcRequired = "{0} phải sử dụng UTC.";

    public const string AlreadyDeleted = "Dữ liệu đã được xóa mềm trước đó.";

    public const string InvalidApiFunctionCode = "Mã function API phải bắt đầu bằng chữ cái và chỉ được chứa chữ in hoa, số, dấu chấm, dấu gạch ngang hoặc dấu gạch dưới.";
    public const string InvalidDepartmentCode = "Mã phòng ban phải bắt đầu bằng chữ cái và chỉ được chứa chữ in hoa, số, dấu gạch ngang hoặc dấu gạch dưới.";
    public const string InvalidLanguageCode = "Mã ngôn ngữ phải theo định dạng được hỗ trợ, ví dụ: vi, en hoặc en-us.";
    public const string InvalidOrganizationSlug = "Slug của tổ chức chỉ được chứa chữ thường, số và dấu gạch ngang đơn.";
    public const string InvalidRouteTemplate = "Route template phải bắt đầu bằng '/' và không được chứa query string, fragment hoặc khoảng trắng.";
    public const string InvalidTimeZoneId = "Mã múi giờ phải là định danh tương thích với IANA và không được để trống.";

    public const string DepartmentSelfParent = "Phòng ban không thể là phòng ban cha của chính nó.";
    public const string DepartmentAlreadyArchived = "Phòng ban đã được lưu trữ.";
    public const string DepartmentNotActive = "Không thể chỉnh sửa phòng ban đã lưu trữ.";
    public const string DepartmentMembershipAlreadyLeft = "Quan hệ thành viên phòng ban đã kết thúc.";
    public const string DepartmentMembershipInvalidLeftAt = "LeftAt không thể sớm hơn JoinedAt.";

    public const string OrganizationArchivedCannotSuspend = "Tổ chức đã lưu trữ không thể chuyển sang trạng thái tạm ngưng.";
    public const string OrganizationAlreadySuspended = "Tổ chức đã ở trạng thái tạm ngưng.";
    public const string OrganizationInvalidActivation = "Chỉ tổ chức đang tạm ngưng mới có thể được kích hoạt.";
    public const string OrganizationAlreadyArchived = "Tổ chức đã được lưu trữ.";
    public const string OrganizationArchived = "Tổ chức đã lưu trữ không thể chỉnh sửa.";

    public const string OrganizationMembershipInvalidActivation = "Chỉ thành viên ở trạng thái được mời hoặc tạm ngưng mới có thể kích hoạt.";
    public const string OrganizationMembershipInvalidSuspension = "Chỉ thành viên đang hoạt động mới có thể tạm ngưng.";
    public const string OrganizationMembershipAlreadyRemoved = "Quan hệ thành viên của tổ chức đã bị gỡ.";
    public const string OrganizationMembershipInvalidRemovedAt = "RemovedAt không thể sớm hơn JoinedAt.";

    public const string OrganizationRoleAssignmentNotActive = "Chỉ phân quyền vai trò đang hoạt động mới có thể thu hồi.";
    public const string OrganizationRoleAssignmentInvalidRevokedAt = "RevokedAt không thể sớm hơn ValidFrom.";
    public const string OrganizationRoleAssignmentNotExpired = "Chỉ phân quyền đang hoạt động và đã quá thời hạn mới có thể chuyển sang hết hạn.";
    public const string InvalidValidityRange = "ExpiresAt phải sau ValidFrom.";

    public const string RoleApiFunctionAlreadyRevoked = "Quyền API của role đã được thu hồi.";
    public const string RoleApiFunctionInvalidRevokedAt = "RevokedAt không thể sớm hơn ValidFrom.";
}
