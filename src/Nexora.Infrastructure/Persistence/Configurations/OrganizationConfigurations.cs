using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;
using Nexora.Domain.Organizations.Enums;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations", SchemaNames.Organization);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasConversion(x => x.Value, x => OrganizationSlug.Create(x))
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(EnumStringConversion.Create<OrganizationStatus>())
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.DataResidency).HasColumnName("dataResidency").HasMaxLength(50);
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deletedAt").HasColumnType("timestamptz");
        builder.Property(x => x.DeleteBy).HasColumnName("deleteBy");
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Memberships)
            .WithOne(x => x.Organization)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.RoleAssignments)
            .WithOne(x => x.Organization)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Settings)
            .WithOne(x => x.Organization)
            .HasForeignKey<OrganizationSetting>(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Departments)
            .WithOne(x => x.Organization)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.RoleApiFunctions)
            .WithOne(x => x.Organization)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.DeleteBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles", SchemaNames.Organization);
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).HasColumnName("userId");
        builder.Property(x => x.DisplayName).HasColumnName("displayName").HasMaxLength(200).IsRequired();
        builder.Property(x => x.PreferredLanguage)
            .HasColumnName("preferredLanguage")
            .HasConversion(x => x.Value, x => LanguageCode.Create(x))
            .HasMaxLength(10)
            .HasDefaultValue(LanguageCode.Create("en"))
            .IsRequired();
        builder.Property(x => x.TimeZone)
            .HasColumnName("timeZone")
            .HasConversion(x => x == null ? null : x.Value, x => x == null ? null : TimeZoneId.Create(x))
            .HasMaxLength(50);
        builder.Property(x => x.AvatarObjectKey).HasColumnName("avatarObjectKey").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deletedAt").HasColumnType("timestamptz");
        builder.Property(x => x.DeleteBy).HasColumnName("deleteBy");
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.DeleteBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.ToTable("OrganizationMemberships", SchemaNames.Organization);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("userId").IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(EnumStringConversion.Create<OrganizationMembershipStatus>())
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.JoinedAt).HasColumnName("joinedAt").HasColumnType("timestamptz");
        builder.Property(x => x.RemovedAt).HasColumnName("removedAt").HasColumnType("timestamptz");
        builder.Property(x => x.RemovedByUserId).HasColumnName("removedByUserId");
        builder.Property(x => x.InvitedByUserId).HasColumnName("invitedByUserId");
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Id }).IsUnique().HasDatabaseName("ux_org_membership_tenant_id");
        builder.HasIndex(x => new { x.OrganizationId, x.Status });
        builder.HasIndex(x => new { x.OrganizationId, x.UserId }).HasDatabaseName("ix_org_membership_user");

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RemovedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.InvitedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrganizationRoleAssignmentConfiguration : IEntityTypeConfiguration<OrganizationRoleAssignment>
{
    public void Configure(EntityTypeBuilder<OrganizationRoleAssignment> builder)
    {
        builder.ToTable("OrganizationRoleAssignments", SchemaNames.Organization);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("userId").IsRequired();
        builder.Property(x => x.RoleId).HasColumnName("roleId").IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(EnumStringConversion.Create<OrganizationRoleAssignmentStatus>())
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("validFrom").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expiresAt").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedAt).HasColumnName("revokedAt").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedByUserId).HasColumnName("revokedByUserId");
        builder.Property(x => x.AssignedByUserId).HasColumnName("assignedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.UserId, x.RoleId }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.UserId, x.Status });
        builder.HasIndex(x => new { x.OrganizationId, x.RoleId, x.Status });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrganizationSettingConfiguration : IEntityTypeConfiguration<OrganizationSetting>
{
    public void Configure(EntityTypeBuilder<OrganizationSetting> builder)
    {
        builder.ToTable("OrganizationSettings", SchemaNames.Organization);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.DefaultLanguage)
            .HasColumnName("defaultLanguage")
            .HasConversion(x => x.Value, x => LanguageCode.Create(x))
            .HasMaxLength(10)
            .HasDefaultValue(LanguageCode.Create("en"))
            .IsRequired();
        builder.Property(x => x.TimeZone)
            .HasColumnName("timeZone")
            .HasConversion(x => x.Value, x => TimeZoneId.Create(x))
            .HasMaxLength(50)
            .HasDefaultValue(TimeZoneId.Create("Asia/Ho_Chi_Minh"))
            .IsRequired();
        builder.Property(x => x.SettingsJson).HasColumnName("settingsJson").HasColumnType("jsonb");
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => x.OrganizationId).IsUnique();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments", SchemaNames.Organization);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.ParentDepartmentId).HasColumnName("parentDepartmentId");
        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasConversion(x => x.Value, x => DepartmentCode.Create(x))
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ManagerUserId).HasColumnName("managerUserId");
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(EnumStringConversion.Create<DepartmentStatus>())
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deletedAt").HasColumnType("timestamptz");
        builder.Property(x => x.DeleteBy).HasColumnName("deleteBy");
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.ParentDepartmentId, x.Name }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Status });

        builder.HasOne(x => x.Organization).WithMany(x => x.Departments).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ParentDepartment).WithMany(x => x.Children).HasForeignKey(x => x.ParentDepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Memberships).WithOne(x => x.Department).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ManagerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DeleteBy).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DepartmentMembershipConfiguration : IEntityTypeConfiguration<DepartmentMembership>
{
    public void Configure(EntityTypeBuilder<DepartmentMembership> builder)
    {
        builder.ToTable("DepartmentMemberships", SchemaNames.Organization);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.DepartmentId).HasColumnName("departmentId").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("userId").IsRequired();
        builder.Property(x => x.MembershipType)
            .HasColumnName("membershipType")
            .HasConversion(EnumStringConversion.Create<DepartmentMembershipType>())
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.JoinedAt).HasColumnName("joinedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.LeftAt).HasColumnName("leftAt").HasColumnType("timestamptz");
        builder.Property(x => x.LeftByUserId).HasColumnName("leftByUserId");
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");

        builder.HasIndex(x => new { x.OrganizationId, x.DepartmentId, x.UserId }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.UserId });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Department).WithMany(x => x.Memberships).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.LeftByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
