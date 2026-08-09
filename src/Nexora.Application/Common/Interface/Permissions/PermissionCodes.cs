namespace Nexora.Application.Common.Permissions;

public static class PermissionCodes
{
    public const string UserRead = "user.read";
    public const string UserReadById = "user.read.by_id";
    public const string UserReadRoles = "user.read.roles";
    public const string UserCreate = "user.create";
    public const string UserUpdate = "user.update";
    public const string UserDeactivate = "user.deactivate";
    public const string UserReactivate = "user.reactivate";
    public const string UserResetPassword = "user.reset_password";
    public const string UserRoleAssign = "user.role.assign";
    public const string UserRoleRevoke = "user.role.revoke";
    public const string RoleRead = "role.read";
    public const string RoleReadById = "role.read.by_id";
    public const string RoleReadPermissions = "role.read.permissions";
    public const string RoleCreate = "role.create";
    public const string RoleUpdate = "role.update";
    public const string RoleReactivate = "role.reactivate";
    public const string RoleDeactivate = "role.deactivate";
    public const string RoleAssign = UserRoleAssign;
    public const string RolePermissionManage = "role.permission_manage";
    public const string RolePermissionGrant = "role.permission.grant";
    public const string RolePermissionRevoke = "role.permission.revoke";
    public const string DepartmentRead = "department.read";
    public const string DepartmentReadById = "department.read.by_id";
    public const string DepartmentMemberRead = "department.member.read";
    public const string DepartmentCreate = "department.create";
    public const string DepartmentUpdate = "department.update";
    public const string DepartmentArchive = "department.archive";
    public const string DepartmentMemberManage = "department.member_manage";
    public const string DepartmentMemberAdd = "department.member.add";
    public const string DepartmentMemberRemove = "department.member.remove";
    public const string OrganizationRead = "organization.read";
    public const string OrganizationReadById = "organization.read.by_id";
    public const string OrganizationCreate = "organization.create";
    public const string OrganizationUpdate = "organization.update";
    public const string OrganizationSuspend = "organization.suspend";
    public const string OrganizationActivate = "organization.activate";
    public const string OrganizationArchive = "organization.archive";
}
