using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Organizations;
using Nexora.Domain.Projects;
using TaskEntity = Nexora.Domain.Tasks.Task;
using Nexora.Infrastructure.Services.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

internal sealed class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("Tasks", SchemaNames.Task);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.ProjectId).HasColumnName("projectId");
        builder.Property(x => x.DepartmentId).HasColumnName("departmentId");
        builder.Property(x => x.ParentTaskId).HasColumnName("parentTaskId");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.DescriptionCiphertext).HasColumnName("descriptionCiphertext").HasColumnType("bytea");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Priority).HasColumnName("priority").HasMaxLength(20).IsRequired();
        builder.Property(x => x.AssigneeUserId).HasColumnName("assigneeUserId");
        builder.Property(x => x.CreatedByUserId).HasColumnName("createdByUserId").IsRequired();
        builder.Property(x => x.DueAt).HasColumnName("dueAt").HasColumnType("timestamptz");
        builder.Property(x => x.CompletedAt).HasColumnName("completedAt").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deletedAt").HasColumnType("timestamptz");
        builder.Property(x => x.DeleteBy).HasColumnName("deleteBy");
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.ProjectId, x.Status });
        builder.HasIndex(x => new { x.OrganizationId, x.AssigneeUserId, x.Status });
        builder.HasIndex(x => new { x.OrganizationId, x.DueAt });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaskEntity>().WithMany().HasForeignKey(x => x.ParentTaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssigneeUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DeleteBy).OnDelete(DeleteBehavior.Restrict);
    }
}
