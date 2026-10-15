-- Seed ApiFunctions from SyncApiFunctionCatalogCommandHandler.
-- The script is idempotent: existing rows with the same code are preserved.
-- If an existing code has a different HTTP method or route template, the script fails.

BEGIN;

CREATE TEMP TABLE "_ApiFunctionCatalog" (
    "code" text PRIMARY KEY,
    "module" text NOT NULL,
    "name" text NOT NULL,
    "httpMethod" text NOT NULL,
    "routeTemplate" text NOT NULL,
    "actionType" text NOT NULL,
    "riskLevel" text NOT NULL
) ON COMMIT DROP;

INSERT INTO "_ApiFunctionCatalog"
(
    "code",
    "module",
    "name",
    "httpMethod",
    "routeTemplate",
    "actionType",
    "riskLevel"
)
VALUES
    ('AUTH', 'Auth', 'Auth', 'ANY', '#', 'READ', 'LOW'),
    ('USER', 'User', 'User', 'ANY', '#', 'READ', 'LOW'),
    ('ACTIVITY_LOG', 'ActivityLog', 'Activity Log', 'ANY', '#', 'READ', 'LOW'),
    ('ROLE', 'Role', 'Role', 'ANY', '#', 'READ', 'LOW'),
    ('DOCUMENT', 'Document', 'Document', 'ANY', '#', 'READ', 'LOW'),
    ('AUTHORIZATION', 'Authorization', 'Authorization', 'ANY', '#', 'READ', 'LOW'),
    ('DEPARTMENT', 'Department', 'Department', 'ANY', '#', 'READ', 'LOW'),
    ('ORGANIZATION', 'Organization', 'Organization', 'ANY', '#', 'READ', 'LOW'),
    ('AUTH.LOGIN', 'Auth', 'Đăng nhập', 'POST', '/api/auth/login', 'WRITE', 'MEDIUM'),
    ('AUTH.REFRESH', 'Auth', 'Làm mới access token', 'POST', '/api/auth/refresh', 'WRITE', 'MEDIUM'),
    ('AUTH.LOGOUT', 'Auth', 'Đăng xuất', 'POST', '/api/auth/logout', 'WRITE', 'MEDIUM'),
    ('AUTH.SELECT_ORGANIZATION', 'Auth', 'Chọn tổ chức', 'POST', '/api/auth/select-organization', 'WRITE', 'MEDIUM'),
    ('AUTH.ME', 'Auth', 'Xem thông tin tài khoản hiện tại', 'GET', '/api/auth/me', 'READ', 'LOW'),

    ('USER.READ', 'User', 'Xem danh sách người dùng', 'GET', '/api/users', 'READ', 'LOW'),
    ('USER.CREATE', 'User', 'Tạo người dùng', 'POST', '/api/users', 'WRITE', 'HIGH'),
    ('USER.READ.BY_ID', 'User', 'Xem thông tin người dùng', 'GET', '/api/users/{userId:guid}', 'READ', 'LOW'),
    ('USER.UPDATE', 'User', 'Cập nhật người dùng', 'PUT', '/api/users/{userId:guid}', 'WRITE', 'MEDIUM'),
    ('USER.DEACTIVATE', 'User', 'Vô hiệu hóa người dùng', 'POST', '/api/users/{userId:guid}/deactivate', 'ADMIN', 'HIGH'),
    ('USER.REACTIVATE', 'User', 'Kích hoạt lại người dùng', 'POST', '/api/users/{userId:guid}/reactivate', 'ADMIN', 'HIGH'),
    ('USER.RESET_PASSWORD', 'User', 'Đặt lại mật khẩu người dùng', 'POST', '/api/users/{userId:guid}/reset-password', 'ADMIN', 'HIGH'),
    ('USER.ROLE.ASSIGN', 'User', 'Gán vai trò cho người dùng', 'POST', '/api/users/{userId:guid}/roles/{roleId:guid}', 'ADMIN', 'HIGH'),
    ('USER.ROLE.REVOKE', 'User', 'Thu hồi vai trò của người dùng', 'DELETE', '/api/users/{userId:guid}/roles/{roleId:guid}', 'ADMIN', 'HIGH'),
    ('USER.READ.ROLES', 'User', 'Xem danh sách vai trò của người dùng', 'GET', '/api/users/{userId:guid}/roles', 'READ', 'LOW'),

    ('ROLE.READ', 'Role', 'Xem danh sách vai trò', 'GET', '/api/roles', 'READ', 'LOW'),
    ('ROLE.CREATE', 'Role', 'Tạo vai trò', 'POST', '/api/roles', 'ADMIN', 'HIGH'),
    ('ROLE.READ.BY_ID', 'Role', 'Xem thông tin vai trò', 'GET', '/api/roles/{roleId:guid}', 'READ', 'LOW'),
    ('ROLE.UPDATE', 'Role', 'Cập nhật vai trò', 'PUT', '/api/roles/{roleId:guid}', 'ADMIN', 'HIGH'),
    ('ROLE.DEACTIVATE', 'Role', 'Vô hiệu hóa vai trò', 'POST', '/api/roles/{roleId:guid}/deactivate', 'ADMIN', 'HIGH'),
    ('ROLE.REACTIVATE', 'Role', 'Kích hoạt lại vai trò', 'POST', '/api/roles/{roleId:guid}/reactivate', 'ADMIN', 'HIGH'),
    ('ROLE.PERMISSION.GRANT', 'Role', 'Cấp quyền cho vai trò', 'PUT', '/api/roles/{roleId:guid}/permissions/{functionCode}', 'ADMIN', 'HIGH'),
    ('ROLE.PERMISSION.REVOKE', 'Role', 'Thu hồi quyền của vai trò', 'DELETE', '/api/roles/{roleId:guid}/permissions/{functionCode}', 'ADMIN', 'HIGH'),
    ('ROLE.PERMISSION.SYNC', 'Role', 'Đồng bộ quyền cho vai trò', 'PUT', '/api/roles/{roleId:guid}/permissions', 'ADMIN', 'HIGH'),
    ('ROLE.READ.PERMISSIONS', 'Role', 'Xem danh sách quyền của vai trò', 'GET', '/api/roles/{roleId:guid}/permissions', 'READ', 'MEDIUM'),

    ('DOCUMENT.UPLOAD', 'Document', 'Tải tài liệu lên', 'POST', '/api/documents', 'WRITE', 'MEDIUM'),
    ('DOCUMENT.LIST', 'Document', 'Xem danh sách tài liệu', 'GET', '/api/documents', 'READ', 'LOW'),
    ('DOCUMENT.READ', 'Document', 'Xem trạng thái tài liệu', 'GET', '/api/documents/{documentId:guid}', 'READ', 'LOW'),
    ('DOCUMENT.STATUS', 'Document', 'Xem trạng thái xử lý tài liệu', 'GET', '/api/documents/{documentId:guid}/status', 'READ', 'LOW'),
    ('DOCUMENT.DELETE', 'Document', 'Xóa tài liệu', 'DELETE', '/api/documents/{documentId:guid}', 'WRITE', 'HIGH'),
    ('RAG.RETRIEVAL', 'RAG', 'Tìm kiếm đoạn tài liệu liên quan', 'POST', '/api/rag/retrieval', 'READ', 'LOW'),
    ('CHAT.MODELS', 'Chat', 'Xem mô hình hội thoại', 'GET', '/api/chat/models', 'READ', 'LOW'),
    ('CHAT.CONVERSATIONS', 'Chat', 'Xem hội thoại', 'GET', '/api/chat/conversations', 'READ', 'LOW'),
    ('CHAT.MESSAGES', 'Chat', 'Xem tin nhắn hội thoại', 'GET', '/api/chat/conversations/{conversationId:guid}/messages', 'READ', 'LOW'),
    ('CHAT.STREAM', 'Chat', 'Gửi câu hỏi hội thoại', 'POST', '/api/chat/turns/stream', 'WRITE', 'MEDIUM'),
    ('CHAT.MESSAGE.DELETE', 'Chat', 'Xóa tin nhắn hội thoại', 'DELETE', '/api/chat/conversations/{conversationId:guid}/messages/{messageId:guid}', 'WRITE', 'MEDIUM'),
    ('ACCOUNT.PROFILE.READ', 'Account', 'Xem hồ sơ cá nhân', 'GET', '/api/account/profile', 'READ', 'LOW'),
    ('ACCOUNT.PROFILE.UPDATE', 'Account', 'Cập nhật hồ sơ cá nhân', 'PUT', '/api/account/profile', 'WRITE', 'MEDIUM'),
    ('ACCOUNT.KEYS.READ', 'Account', 'Xem trạng thái khóa AI cá nhân', 'GET', '/api/account/provider-keys', 'READ', 'MEDIUM'),
    ('ACCOUNT.KEYS.SAVE', 'Account', 'Lưu khóa AI cá nhân', 'PUT', '/api/account/provider-keys/{provider}', 'WRITE', 'HIGH'),
    ('ACCOUNT.KEYS.DELETE', 'Account', 'Xóa khóa AI cá nhân', 'DELETE', '/api/account/provider-keys/{provider}', 'WRITE', 'HIGH'),

    ('API_FUNCTION.READ', 'Authorization', 'Xem danh sách chức năng API', 'GET', '/api/api-functions', 'READ', 'LOW'),
    ('API_FUNCTION.CREATE', 'Authorization', 'Tạo chức năng API', 'POST', '/api/api-functions', 'ADMIN', 'HIGH'),
    ('API_FUNCTION.READ.BY_ID', 'Authorization', 'Xem thông tin chức năng API', 'GET', '/api/api-functions/{apiFunctionId:guid}', 'READ', 'LOW'),
    ('API_FUNCTION.UPDATE', 'Authorization', 'Cập nhật chức năng API', 'PUT', '/api/api-functions/{apiFunctionId:guid}', 'ADMIN', 'HIGH'),
    ('API_FUNCTION.DEACTIVATE', 'Authorization', 'Vô hiệu hóa chức năng API', 'POST', '/api/api-functions/{apiFunctionId:guid}/deactivate', 'ADMIN', 'HIGH'),
    ('API_FUNCTION.REACTIVATE', 'Authorization', 'Kích hoạt lại chức năng API', 'POST', '/api/api-functions/{apiFunctionId:guid}/reactivate', 'ADMIN', 'HIGH'),

    ('DEPARTMENT.READ', 'Department', 'Xem danh sách phòng ban', 'GET', '/api/departments', 'READ', 'LOW'),
    ('DEPARTMENT.CREATE', 'Department', 'Tạo phòng ban', 'POST', '/api/departments', 'WRITE', 'MEDIUM'),
    ('DEPARTMENT.READ.BY_ID', 'Department', 'Xem thông tin phòng ban', 'GET', '/api/departments/{departmentId:guid}', 'READ', 'LOW'),
    ('DEPARTMENT.UPDATE', 'Department', 'Cập nhật phòng ban', 'PUT', '/api/departments/{departmentId:guid}', 'WRITE', 'MEDIUM'),
    ('DEPARTMENT.ARCHIVE', 'Department', 'Lưu trữ phòng ban', 'POST', '/api/departments/{departmentId:guid}/archive', 'ADMIN', 'HIGH'),
    ('DEPARTMENT.MEMBER.ADD', 'Department', 'Thêm thành viên vào phòng ban', 'PUT', '/api/departments/{departmentId:guid}/members/{userId:guid}', 'ADMIN', 'MEDIUM'),
    ('DEPARTMENT.MEMBER.REMOVE', 'Department', 'Xóa thành viên khỏi phòng ban', 'DELETE', '/api/departments/{departmentId:guid}/members/{userId:guid}', 'ADMIN', 'MEDIUM'),
    ('DEPARTMENT.MEMBER.READ', 'Department', 'Xem danh sách thành viên phòng ban', 'GET', '/api/departments/{departmentId:guid}/members', 'READ', 'LOW'),

    ('ORGANIZATION.READ', 'Organization', 'Xem danh sách tổ chức', 'GET', '/api/organizations', 'READ', 'LOW'),
    ('ORGANIZATION.CREATE', 'Organization', 'Tạo tổ chức', 'POST', '/api/organizations', 'ADMIN', 'HIGH'),
    ('ORGANIZATION.READ.BY_ID', 'Organization', 'Xem thông tin tổ chức', 'GET', '/api/organizations/{organizationId:guid}', 'READ', 'LOW'),
    ('ORGANIZATION.UPDATE', 'Organization', 'Cập nhật tổ chức', 'PUT', '/api/organizations/{organizationId:guid}', 'ADMIN', 'HIGH'),
    ('ORGANIZATION.SUSPEND', 'Organization', 'Tạm ngưng tổ chức', 'POST', '/api/organizations/{organizationId:guid}/suspend', 'ADMIN', 'HIGH'),
    ('ORGANIZATION.ACTIVATE', 'Organization', 'Kích hoạt tổ chức', 'POST', '/api/organizations/{organizationId:guid}/activate', 'ADMIN', 'HIGH'),
    ('ORGANIZATION.ARCHIVE', 'Organization', 'Lưu trữ tổ chức', 'POST', '/api/organizations/{organizationId:guid}/archive', 'ADMIN', 'HIGH');

DO $$
DECLARE
    conflicting_code text;
BEGIN
    SELECT catalog."code"
    INTO conflicting_code
    FROM "_ApiFunctionCatalog" catalog
    INNER JOIN "security"."ApiFunctions" existing
        ON existing."code" = catalog."code"
    WHERE existing."httpMethod" <> catalog."httpMethod"
       OR existing."routeTemplate" <> catalog."routeTemplate"
    LIMIT 1;

    IF conflicting_code IS NOT NULL THEN
        RAISE EXCEPTION
            'ApiFunction code "%" đã tồn tại nhưng HTTP method hoặc route template không khớp.',
            conflicting_code
            USING ERRCODE = 'P0001';
    END IF;
END $$;

WITH "seed_time" AS
(
    SELECT CURRENT_TIMESTAMP AS "value"
)
INSERT INTO "security"."ApiFunctions"
(
    "id",
    "code",
    "module",
    "name",
    "description",
    "httpMethod",
    "routeTemplate",
    "actionType",
    "riskLevel",
    "requiresConfirmation",
    "status",
    "version",
    "createdAt",
    "updatedByUserId",
    "updatedAt",
    "rowVersion"
)
SELECT
    md5(catalog."code")::uuid,
    catalog."code",
    catalog."module",
    catalog."name",
    NULL,
    catalog."httpMethod",
    catalog."routeTemplate",
    catalog."actionType",
    catalog."riskLevel",
    false,
    'ACTIVE',
    1,
    "seed_time"."value",
    NULL,
    "seed_time"."value",
    1
FROM "_ApiFunctionCatalog" catalog
CROSS JOIN "seed_time"
ON CONFLICT ("code") DO NOTHING;

COMMIT;
