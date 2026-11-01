using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Chat;
using Nexora.Domain.Organizations;
using Nexora.Infrastructure.Services.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationSystemPromptConfiguration : IEntityTypeConfiguration<OrganizationSystemPrompt>
{
    public void Configure(EntityTypeBuilder<OrganizationSystemPrompt> builder)
    {
        builder.ToTable("OrganizationSystemPrompts", SchemaNames.Organization, table =>
        {
            table.HasCheckConstraint("ck_organization_system_prompt_type", "\"promptType\" IN ('CHAT', 'MEETING_TRANSLATION')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.PromptType).HasColumnName("promptType").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ActiveVersionId).HasColumnName("activeVersionId");
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.PromptType }).IsUnique();
        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ActiveVersion).WithMany().HasForeignKey(x => x.ActiveVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SystemPromptVersionConfiguration : IEntityTypeConfiguration<SystemPromptVersion>
{
    public void Configure(EntityTypeBuilder<SystemPromptVersion> builder)
    {
        builder.ToTable("OrganizationSystemPromptVersions", SchemaNames.Organization, table =>
        {
            table.HasCheckConstraint("ck_organization_system_prompt_version_status", "\"status\" IN ('DRAFT', 'PUBLISHED')");
            table.HasCheckConstraint("ck_organization_system_prompt_version_number", "\"versionNo\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SystemPromptId).HasColumnName("systemPromptId").IsRequired();
        builder.Property(x => x.VersionNo).HasColumnName("versionNo").IsRequired();
        builder.Property(x => x.Instructions).HasColumnName("instructions").HasMaxLength(8000).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("createdByUserId");
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.PublishedByUserId).HasColumnName("publishedByUserId");
        builder.Property(x => x.PublishedAt).HasColumnName("publishedAt").HasColumnType("timestamptz");

        builder.HasIndex(x => new { x.SystemPromptId, x.VersionNo }).IsUnique();
        builder.HasIndex(x => x.SystemPromptId).IsUnique().HasFilter("\"status\" = 'DRAFT'");
        builder.HasOne(x => x.SystemPrompt).WithMany().HasForeignKey(x => x.SystemPromptId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
