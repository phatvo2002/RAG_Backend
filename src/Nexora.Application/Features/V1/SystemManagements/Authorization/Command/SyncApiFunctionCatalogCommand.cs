using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.Authorization.Command;

public sealed record SyncApiFunctionCatalogCommand : ITransactionalCommand<Result>;

public sealed class SyncApiFunctionCatalogCommandHandler(IApiFunctionRepository functionRepository, IClock clock)
    : IRequestHandler<SyncApiFunctionCatalogCommand, Result>
{
    private static readonly IReadOnlyList<ApiFunctionDefinition> Definitions =
    [
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
        new("role.read", "Role", "List roles", ApiHttpMethod.Get, "/api/roles", ApiActionType.Read, ApiRiskLevel.Low),
        new("role.create", "Role", "Create role", ApiHttpMethod.Post, "/api/roles", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.read.by_id", "Role", "Get role", ApiHttpMethod.Get, "/api/roles/{roleId:guid}", ApiActionType.Read, ApiRiskLevel.Low),
        new("role.update", "Role", "Update role", ApiHttpMethod.Put, "/api/roles/{roleId:guid}", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.deactivate", "Role", "Deactivate role", ApiHttpMethod.Post, "/api/roles/{roleId:guid}/deactivate", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.reactivate", "Role", "Reactivate role", ApiHttpMethod.Post, "/api/roles/{roleId:guid}/reactivate", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.permission.grant", "Role", "Grant role permission", ApiHttpMethod.Put, "/api/roles/{roleId:guid}/permissions/{functionCode}", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.permission.revoke", "Role", "Revoke role permission", ApiHttpMethod.Delete, "/api/roles/{roleId:guid}/permissions/{functionCode}", ApiActionType.Admin, ApiRiskLevel.High),
        new("role.read.permissions", "Role", "Get role permissions", ApiHttpMethod.Get, "/api/roles/{roleId:guid}/permissions", ApiActionType.Read, ApiRiskLevel.Medium),
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
            var existing = await functionRepository.GetByCodeAsync(definition.Code, cancellationToken);
            if (existing is null)
            {
                await functionRepository.AddAsync(definition.Create(clock.UtcNow), cancellationToken);
                continue;
            }

            if (existing.HttpMethod != definition.HttpMethod || existing.RouteTemplate.Value != definition.RouteTemplate)
            {
                return Result.Failure(Error.Conflict(string.Format(CommonMessages.Api.ApiFunctionCatalogConflictTemplate, definition.Code)));
            }
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
        public ApiFunction Create(DateTimeOffset now) => Domain.Authorization.ApiFunction.Create(Code, Module, Name, HttpMethod, RouteTemplate, ActionType, RiskLevel, now, description: Description);
    }
}
