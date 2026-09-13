using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;
using DomainApiFunction = Nexora.Domain.Authorization.ApiFunction;

namespace Nexora.Application.Features.V1.SystemManagements.Authorization.Command;

public sealed record SyncApiFunctionCatalogCommand : ITransactionalCommand<Result>;

public sealed class SyncApiFunctionCatalogCommandHandler(IUnitOfWork unitOfWork, IClock clock)
    : IRequestHandler<SyncApiFunctionCatalogCommand, Result>
{
    private static readonly IReadOnlyDictionary<string, MenuMetadata> MenuMetadataByCode =
        new Dictionary<string, MenuMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["auth"] = new(null, 1, true, false, "#"),
            ["user"] = new(null, 2, true, false, "#"),
            ["activity_log"] = new(null, 3, true, false, "#"),
            ["role"] = new(null, 4, true, false, "#"),
            ["document"] = new(null, 5, true, false, "#"),
            ["authorization"] = new(null, 6, true, false, "#"),
            ["department"] = new(null, 7, true, false, "#"),
            ["organization"] = new(null, 8, true, false, "#"),
            ["auth.login"] = new("AUTH", 1, false),
            ["auth.refresh"] = new("AUTH", 2, false),
            ["auth.logout"] = new("AUTH", 3, false),
            ["auth.select_organization"] = new("AUTH", 4, false),
            ["auth.me"] = new("AUTH", 5, false),
            ["user.read"] = new("USER", 10),
            ["user.create"] = new("USER", 20),
            ["user.read.by_id"] = new("USER", 21),
            ["user.update"] = new("USER", 30),
            ["user.deactivate"] = new("USER", 40),
            ["user.reactivate"] = new("USER", 50),
            ["user.reset_password"] = new("USER", 60),
            ["user.role.assign"] = new("USER", 70),
            ["user.role.revoke"] = new("USER", 80),
            ["user.read.roles"] = new("USER", 22),
            ["activity_log.read"] = new("ACTIVITY_LOG", 1),
            ["role.read"] = new("ROLE", 10),
            ["role.create"] = new("ROLE", 20),
            ["role.read.by_id"] = new("ROLE", 21),
            ["role.update"] = new("ROLE", 30),
            ["role.deactivate"] = new("ROLE", 40),
            ["role.reactivate"] = new("ROLE", 50),
            ["role.permission.grant"] = new("ROLE", 60),
            ["role.permission.revoke"] = new("ROLE", 70),
            ["role.permission.sync"] = new("ROLE", 80),
            ["role.read.permissions"] = new("ROLE", 22),
            ["document.upload"] = new("DOCUMENT", 1),
            ["document.read"] = new("DOCUMENT", 2),
            ["document.status"] = new("DOCUMENT", 3),
            ["document.folder.read"] = new("DOCUMENT", 10),
            ["document.folder.read.by_id"] = new("DOCUMENT", 11),
            ["document.folder.create"] = new("DOCUMENT", 20),
            ["document.folder.update"] = new("DOCUMENT", 30),
            ["document.folder.delete"] = new("DOCUMENT", 40),
            ["document.folder.grants.read"] = new("DOCUMENT", 50),
            ["document.folder.grants.create"] = new("DOCUMENT", 60),
            ["document.folder.grants.revoke"] = new("DOCUMENT", 70),
            ["api_function.read"] = new("AUTHORIZATION", 1),
            ["api_function.create"] = new("AUTHORIZATION", 2),
            ["api_function.read.by_id"] = new("AUTHORIZATION", 3),
            ["api_function.update"] = new("AUTHORIZATION", 4),
            ["api_function.deactivate"] = new("AUTHORIZATION", 5),
            ["api_function.reactivate"] = new("AUTHORIZATION", 6),
            ["department.read"] = new("DEPARTMENT", 10),
            ["department.create"] = new("DEPARTMENT", 20),
            ["department.read.by_id"] = new("DEPARTMENT", 21),
            ["department.update"] = new("DEPARTMENT", 30),
            ["department.archive"] = new("DEPARTMENT", 40),
            ["department.member.add"] = new("DEPARTMENT", 50),
            ["department.member.remove"] = new("DEPARTMENT", 60),
            ["department.member.read"] = new("DEPARTMENT", 51),
            ["organization.read"] = new("ORGANIZATION", 10),
            ["organization.create"] = new("ORGANIZATION", 20),
            ["organization.read.by_id"] = new("ORGANIZATION", 21),
            ["organization.update"] = new("ORGANIZATION", 30),
            ["organization.suspend"] = new("ORGANIZATION", 40),
            ["organization.activate"] = new("ORGANIZATION", 50),
            ["organization.archive"] = new("ORGANIZATION", 60)
        };

    private static readonly IReadOnlyList<ApiFunctionDefinition> Definitions =
    [
        new("AUTH", "Auth", "Auth", ApiHttpMethod.Any, "#", ApiActionType.Read, ApiRiskLevel.Low, "Auth controller"),
        new("USER", "User", "User", ApiHttpMethod.Any, "#", ApiActionType.Read, ApiRiskLevel.Low, "User controller"),
        new("ACTIVITY_LOG", "ActivityLog", "Activity Log", ApiHttpMethod.Any, "#", ApiActionType.Read, ApiRiskLevel.Low, "Activity log controller"),
        new("ROLE", "Role", "Role", ApiHttpMethod.Any, "#", ApiActionType.Read, ApiRiskLevel.Low, "Role controller"),
        new("DOCUMENT", "Document", "Document", ApiHttpMethod.Any, "#", ApiActionType.Read, ApiRiskLevel.Low, "Document controller"),
        new("AUTHORIZATION", "Authorization", "Authorization", ApiHttpMethod.Any, "#", ApiActionType.Read, ApiRiskLevel.Low, "ApiFunction controller"),
        new("DEPARTMENT", "Department", "Department", ApiHttpMethod.Any, "#", ApiActionType.Read, ApiRiskLevel.Low, "Department controller"),
        new("ORGANIZATION", "Organization", "Organization", ApiHttpMethod.Any, "#", ApiActionType.Read, ApiRiskLevel.Low, "Organization controller"),
        new("auth.login", "Auth", "Login", ApiHttpMethod.Post, "/api/auth/login", ApiActionType.Write, ApiRiskLevel.Medium),
        new("auth.refresh", "Auth", "Refresh token", ApiHttpMethod.Post, "/api/auth/refresh", ApiActionType.Write, ApiRiskLevel.Medium),
        new("auth.logout", "Auth", "Logout", ApiHttpMethod.Post, "/api/auth/logout", ApiActionType.Write, ApiRiskLevel.Medium),
        new("auth.select_organization", "Auth", "Select organization", ApiHttpMethod.Post, "/api/auth/select-organization", ApiActionType.Write, ApiRiskLevel.Medium),
        new("auth.me", "Auth", "Current user", ApiHttpMethod.Get, "/api/auth/me", ApiActionType.Read, ApiRiskLevel.Low),
        new("user.read", "User", "List users", ApiHttpMethod.Get, "/api/users", ApiActionType.Read, ApiRiskLevel.Low),
        new("user.create", "User", "Create user", ApiHttpMethod.Post, "/api/users", ApiActionType.Write, ApiRiskLevel.High),
        new("user.read.by_id", "User", "Get user", ApiHttpMethod.Get, "/api/users/{userId:guid}", ApiActionType.Read, ApiRiskLevel.Low),
        new("user.update", "User", "Update user", ApiHttpMethod.Put, "/api/users/{userId:guid}", ApiActionType.Write, ApiRiskLevel.Medium),
        new("user.deactivate", "User", "Deactivate user", ApiHttpMethod.Post, "/api/users/{userId:guid}/deactivate", ApiActionType.Admin, ApiRiskLevel.High),
        new("user.reactivate", "User", "Reactivate user", ApiHttpMethod.Post, "/api/users/{userId:guid}/reactivate", ApiActionType.Admin, ApiRiskLevel.High),
        new("user.reset_password", "User", "Reset user password", ApiHttpMethod.Post, "/api/users/{userId:guid}/reset-password", ApiActionType.Admin, ApiRiskLevel.High),
        new("user.role.assign", "User", "Assign user role", ApiHttpMethod.Post, "/api/users/{userId:guid}/roles/{roleId:guid}", ApiActionType.Admin, ApiRiskLevel.High),
        new("user.role.revoke", "User", "Revoke user role", ApiHttpMethod.Delete, "/api/users/{userId:guid}/roles/{roleId:guid}", ApiActionType.Admin, ApiRiskLevel.High),
        new("user.read.roles", "User", "Get user roles", ApiHttpMethod.Get, "/api/users/{userId:guid}/roles", ApiActionType.Read, ApiRiskLevel.Low),
        new("activity_log.read", "ActivityLog", "List activity logs", ApiHttpMethod.Get, "/api/activity-logs", ApiActionType.Read, ApiRiskLevel.Medium),
        new("role.read", "Role", "List roles", ApiHttpMethod.Get, "/api/roles", ApiActionType.Read, ApiRiskLevel.Low),
        new("role.create", "Role", "Create role", ApiHttpMethod.Post, "/api/roles", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.read.by_id", "Role", "Get role", ApiHttpMethod.Get, "/api/roles/{roleId:guid}", ApiActionType.Read, ApiRiskLevel.Low),
        new("role.update", "Role", "Update role", ApiHttpMethod.Put, "/api/roles/{roleId:guid}", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.deactivate", "Role", "Deactivate role", ApiHttpMethod.Post, "/api/roles/{roleId:guid}/deactivate", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.reactivate", "Role", "Reactivate role", ApiHttpMethod.Post, "/api/roles/{roleId:guid}/reactivate", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.permission.grant", "Role", "Grant role permission", ApiHttpMethod.Put, "/api/roles/{roleId:guid}/permissions/{functionCode}", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.permission.revoke", "Role", "Revoke role permission", ApiHttpMethod.Delete, "/api/roles/{roleId:guid}/permissions/{functionCode}", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.permission.sync", "Role", "Sync role permissions", ApiHttpMethod.Put, "/api/roles/{roleId:guid}/permissions", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.read.permissions", "Role", "Get role permissions", ApiHttpMethod.Get, "/api/roles/{roleId:guid}/permissions", ApiActionType.Read, ApiRiskLevel.Medium),
        new("document.upload", "Document", "Upload document", ApiHttpMethod.Post, "/api/documents", ApiActionType.Write, ApiRiskLevel.Medium),
        new("document.read", "Document", "Get document status", ApiHttpMethod.Get, "/api/documents/{documentId:guid}", ApiActionType.Read, ApiRiskLevel.Low),
        new("document.status", "Document", "Get document processing status", ApiHttpMethod.Get, "/api/documents/{documentId:guid}/status", ApiActionType.Read, ApiRiskLevel.Low),
        new("document.folder.read", "Document", "List document folders", ApiHttpMethod.Get, "/api/document-folders", ApiActionType.Read, ApiRiskLevel.Low),
        new("document.folder.read.by_id", "Document", "Get document folder", ApiHttpMethod.Get, "/api/document-folders/{folderId:guid}", ApiActionType.Read, ApiRiskLevel.Low),
        new("document.folder.grants.read", "Document", "List document folder grants", ApiHttpMethod.Get, "/api/document-folders/{folderId:guid}/access-grants", ApiActionType.Read, ApiRiskLevel.Medium),
        new("document.folder.create", "Document", "Create document folder", ApiHttpMethod.Post, "/api/document-folders", ApiActionType.Write, ApiRiskLevel.Medium),
        new("document.folder.grants.create", "Document", "Grant document folder access", ApiHttpMethod.Post, "/api/document-folders/{folderId:guid}/access-grants", ApiActionType.Admin, ApiRiskLevel.High),
        new("document.folder.update", "Document", "Update document folder", ApiHttpMethod.Put, "/api/document-folders/{folderId:guid}", ApiActionType.Write, ApiRiskLevel.Medium),
        new("document.folder.delete", "Document", "Delete document folder", ApiHttpMethod.Delete, "/api/document-folders/{folderId:guid}", ApiActionType.Write, ApiRiskLevel.High),
        new("document.folder.grants.revoke", "Document", "Revoke document folder access", ApiHttpMethod.Delete, "/api/document-folders/{folderId:guid}/access-grants/{grantId:guid}", ApiActionType.Admin, ApiRiskLevel.High),
        new("api_function.read", "Authorization", "List API functions", ApiHttpMethod.Get, "/api/api-functions", ApiActionType.Read, ApiRiskLevel.Low),
        new("api_function.create", "Authorization", "Create API function", ApiHttpMethod.Post, "/api/api-functions", ApiActionType.Admin, ApiRiskLevel.High),
        new("api_function.read.by_id", "Authorization", "Get API function", ApiHttpMethod.Get, "/api/api-functions/{apiFunctionId:guid}", ApiActionType.Read, ApiRiskLevel.Low),
        new("api_function.update", "Authorization", "Update API function", ApiHttpMethod.Put, "/api/api-functions/{apiFunctionId:guid}", ApiActionType.Admin, ApiRiskLevel.High),
        new("api_function.deactivate", "Authorization", "Deactivate API function", ApiHttpMethod.Post, "/api/api-functions/{apiFunctionId:guid}/deactivate", ApiActionType.Admin, ApiRiskLevel.High),
        new("api_function.reactivate", "Authorization", "Reactivate API function", ApiHttpMethod.Post, "/api/api-functions/{apiFunctionId:guid}/reactivate", ApiActionType.Admin, ApiRiskLevel.High),
        new("department.read", "Department", "List departments", ApiHttpMethod.Get, "/api/departments", ApiActionType.Read, ApiRiskLevel.Low),
        new("department.create", "Department", "Create department", ApiHttpMethod.Post, "/api/departments", ApiActionType.Write, ApiRiskLevel.Medium),
        new("department.read.by_id", "Department", "Get department", ApiHttpMethod.Get, "/api/departments/{departmentId:guid}", ApiActionType.Read, ApiRiskLevel.Low),
        new("department.update", "Department", "Update department", ApiHttpMethod.Put, "/api/departments/{departmentId:guid}", ApiActionType.Write, ApiRiskLevel.Medium),
        new("department.archive", "Department", "Archive department", ApiHttpMethod.Post, "/api/departments/{departmentId:guid}/archive", ApiActionType.Admin, ApiRiskLevel.High),
        new("department.member.add", "Department", "Add department member", ApiHttpMethod.Put, "/api/departments/{departmentId:guid}/members/{userId:guid}", ApiActionType.Admin, ApiRiskLevel.Medium),
        new("department.member.remove", "Department", "Remove department member", ApiHttpMethod.Delete, "/api/departments/{departmentId:guid}/members/{userId:guid}", ApiActionType.Admin, ApiRiskLevel.Medium),
        new("department.member.read", "Department", "Get department members", ApiHttpMethod.Get, "/api/departments/{departmentId:guid}/members", ApiActionType.Read, ApiRiskLevel.Low),
        new("organization.read", "Organization", "List organizations", ApiHttpMethod.Get, "/api/organizations", ApiActionType.Read, ApiRiskLevel.Low),
        new("organization.create", "Organization", "Create organization", ApiHttpMethod.Post, "/api/organizations", ApiActionType.Admin, ApiRiskLevel.High),
        new("organization.read.by_id", "Organization", "Get organization", ApiHttpMethod.Get, "/api/organizations/{organizationId:guid}", ApiActionType.Read, ApiRiskLevel.Low),
        new("organization.update", "Organization", "Update organization", ApiHttpMethod.Put, "/api/organizations/{organizationId:guid}", ApiActionType.Admin, ApiRiskLevel.High),
        new("organization.suspend", "Organization", "Suspend organization", ApiHttpMethod.Post, "/api/organizations/{organizationId:guid}/suspend", ApiActionType.Admin, ApiRiskLevel.High),
        new("organization.activate", "Organization", "Activate organization", ApiHttpMethod.Post, "/api/organizations/{organizationId:guid}/activate", ApiActionType.Admin, ApiRiskLevel.High),
        new("organization.archive", "Organization", "Archive organization", ApiHttpMethod.Post, "/api/organizations/{organizationId:guid}/archive", ApiActionType.Admin, ApiRiskLevel.High)
    ];

    public async Task<Result> Handle(SyncApiFunctionCatalogCommand request, CancellationToken cancellationToken)
    {
        foreach (var definition in Definitions)
        {
            var existing = await unitOfWork.ApiFunctionRepository.GetByCodeAsync(definition.Code, cancellationToken);
            if (existing is null)
            {
                unitOfWork.ApiFunctionRepository.Insert(definition.Create(clock.UtcNow));
                continue;
            }

            if (existing.HttpMethod != definition.HttpMethod || existing.RouteTemplate.Value != definition.RouteTemplate)
            {
                return Result.Failure(Error.Conflict(string.Format(CommonMessages.Api.ApiFunctionCatalogConflictTemplate, definition.Code)));
            }

            var metadata = GetMenuMetadata(definition.Code);
            existing.UpdateMenuMetadata(metadata.ParentCode, metadata.DisplayOrder, metadata.IsMenuVisible, metadata.IsMobile, metadata.MenuUrl);
        }

        return Result.Success();
    }

    private sealed record ApiFunctionDefinition(
        string Code,
        string Module,
        string Name,
        ApiHttpMethod HttpMethod,
        string RouteTemplate,
        ApiActionType ActionType,
        ApiRiskLevel RiskLevel,
        string? Description = null)
    {
        public DomainApiFunction Create(DateTimeOffset now)
        {
            var metadata = GetMenuMetadata(Code);
            return DomainApiFunction.Create(
                Code,
                Module,
                Name,
                HttpMethod,
                RouteTemplate,
                ActionType,
                RiskLevel,
                now,
                description: Description,
                menuParentCode: metadata.ParentCode,
                displayOrder: metadata.DisplayOrder,
                isMenuVisible: metadata.IsMenuVisible,
                isMobile: metadata.IsMobile,
                menuUrl: metadata.MenuUrl);
        }
    }

    private static MenuMetadata GetMenuMetadata(string code) =>
        MenuMetadataByCode.TryGetValue(code, out var metadata)
            ? metadata
            : new MenuMetadata(null, 0, false);

    private sealed record MenuMetadata(
        string? ParentCode,
        int DisplayOrder,
        bool IsMenuVisible = true,
        bool IsMobile = false,
        string? MenuUrl = null);
}
