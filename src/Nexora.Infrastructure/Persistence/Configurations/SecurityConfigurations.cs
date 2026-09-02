using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;
using Nexora.Domain.Projects;
using Nexora.Domain.Security;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

internal sealed class ApiFunctionConfiguration : IEntityTypeConfiguration<ApiFunction>
{
    public void Configure(EntityTypeBuilder<ApiFunction> builder)
    {
        builder.ToTable("ApiFunctions", SchemaNames.Security);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasConversion(x => x.Value, x => ApiFunctionCode.Create(x))
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(x => x.Module).HasColumnName("module").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.HttpMethod)
            .HasColumnName("httpMethod")
            .HasConversion(EnumStringConversion.Create<ApiHttpMethod>())
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(x => x.RouteTemplate)
            .HasColumnName("routeTemplate")
            .HasConversion(x => x.Value, x => RouteTemplate.Create(x))
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(x => x.ActionType)
            .HasColumnName("actionType")
            .HasConversion(EnumStringConversion.Create<ApiActionType>())
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.RiskLevel)
            .HasColumnName("riskLevel")
            .HasConversion(EnumStringConversion.Create<ApiRiskLevel>())
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.RequiresConfirmation).HasColumnName("requiresConfirmation").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(EnumStringConversion.Create<ApiFunctionStatus>())
            .HasMaxLength(20)
            .HasDefaultValue(ApiFunctionStatus.Active)
            .IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.Module, x.Status });
        builder.HasIndex(x => new { x.HttpMethod, x.RouteTemplate, x.Status })
            .HasDatabaseName("ix_api_functions_http_method_route_status");
        builder.HasMany(x => x.RoleApiFunctions).WithOne(x => x.Function).HasForeignKey(x => x.FunctionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RoleApiFunctionConfiguration : IEntityTypeConfiguration<RoleApiFunction>
{
    public void Configure(EntityTypeBuilder<RoleApiFunction> builder)
    {
        builder.ToTable("RoleApiFunctions", SchemaNames.Security, table =>
        {
            table.HasCheckConstraint("ck_role_api_function_effect", "\"effect\" IN ('ALLOW', 'DENY')");
            table.HasCheckConstraint("ck_role_api_function_valid_range", "\"expiresAt\" IS NULL OR \"validFrom\" < \"expiresAt\"");
            table.HasCheckConstraint("ck_role_api_function_revoke_actor", "(\"revokedAt\" IS NULL AND \"revokedByUserId\" IS NULL) OR (\"revokedAt\" IS NOT NULL AND \"revokedByUserId\" IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.RoleId).HasColumnName("roleId").IsRequired();
        builder.Property(x => x.FunctionId).HasColumnName("functionId").IsRequired();
        builder.Property(x => x.Effect)
            .HasColumnName("effect")
            .HasConversion(EnumStringConversion.Create<PermissionEffect>())
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("validFrom").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expiresAt").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedAt).HasColumnName("revokedAt").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedByUserId).HasColumnName("revokedByUserId");
        builder.Property(x => x.GrantedByUserId).HasColumnName("grantedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.RoleId, x.FunctionId }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.FunctionId, x.Effect });
        builder.HasIndex(x => new { x.OrganizationId, x.RoleId });

        builder.HasOne(x => x.Organization).WithMany(x => x.RoleApiFunctions).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Function).WithMany(x => x.RoleApiFunctions).HasForeignKey(x => x.FunctionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.GrantedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AccessTokenConfiguration : IEntityTypeConfiguration<AccessToken>
{
    public void Configure(EntityTypeBuilder<AccessToken> builder)
    {
        builder.ToTable("AccessTokens", SchemaNames.Security, table =>
        {
            table.HasCheckConstraint("ck_access_token_hash_length", "octet_length(\"tokenHash\") = 32");
            table.HasCheckConstraint("ck_access_token_valid_range", "\"issuedAt\" < \"expiresAt\"");
            table.HasCheckConstraint("ck_access_token_revoke_actor", "(\"revokedAt\" IS NULL AND \"revokedByUserId\" IS NULL) OR (\"revokedAt\" IS NOT NULL AND \"revokedByUserId\" IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("userId").IsRequired();
        builder.Property(x => x.TokenHash).HasColumnName("tokenHash").HasColumnType("bytea").HasMaxLength(32).IsRequired();
        builder.Property(x => x.TokenType).HasColumnName("tokenType").HasMaxLength(20).IsRequired();
        builder.Property(x => x.TokenName).HasColumnName("tokenName").HasMaxLength(100);
        builder.Property(x => x.Jti).HasColumnName("jti").HasMaxLength(150);
        builder.Property(x => x.IssuedAt).HasColumnName("issuedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expiresAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.LastUsedAt).HasColumnName("lastUsedAt").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedAt).HasColumnName("revokedAt").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedByUserId).HasColumnName("revokedByUserId");
        builder.Property(x => x.RevokeReason).HasColumnName("revokeReason").HasMaxLength(300);
        builder.Property(x => x.CreatedIp).HasColumnName("createdIp").HasColumnType("inet");
        builder.Property(x => x.LastUsedIp).HasColumnName("lastUsedIp").HasColumnType("inet");
        builder.Property(x => x.UserAgent).HasColumnName("userAgent").HasMaxLength(500);
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.Jti).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.UserId, x.RevokedAt, x.ExpiresAt });
        builder.HasIndex(x => new { x.OrganizationId, x.TokenType, x.ExpiresAt });
        builder.HasIndex(x => x.ExpiresAt);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs", SchemaNames.Security);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId");
        builder.Property(x => x.ProjectId).HasColumnName("projectId");
        builder.Property(x => x.ActorUserId).HasColumnName("actorUserId");
        builder.Property(x => x.ApiFunctionId).HasColumnName("apiFunctionId");
        builder.Property(x => x.Module).HasColumnName("module").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ResourceType).HasColumnName("resourceType").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ResourceId).HasColumnName("resourceId");
        builder.Property(x => x.Result).HasColumnName("result").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasColumnName("ipAddress").HasColumnType("inet");
        builder.Property(x => x.UserAgent).HasColumnName("userAgent").HasMaxLength(500);
        builder.Property(x => x.CorrelationId).HasColumnName("correlationId").HasMaxLength(100);
        builder.Property(x => x.MetadataJson).HasColumnName("metadataJson").HasColumnType("jsonb");
        builder.Property(x => x.PreviousLogHash).HasColumnName("previousLogHash").HasColumnType("char(64)").HasMaxLength(64);
        builder.Property(x => x.LogHash).HasColumnName("logHash").HasColumnType("char(64)").HasMaxLength(64);
        builder.Property(x => x.OccurredAt).HasColumnName("occurredAt").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.ActorUserId, x.OccurredAt });
        builder.HasIndex(x => new { x.OrganizationId, x.ProjectId, x.OccurredAt });
        builder.HasIndex(x => new { x.OrganizationId, x.Module, x.Action, x.OccurredAt });
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.LogHash).IsUnique();

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApiFunction>().WithMany().HasForeignKey(x => x.ApiFunctionId).OnDelete(DeleteBehavior.Restrict);
    }
}
