using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Calendar;
using Nexora.Domain.Meetings;
using Nexora.Domain.Organizations;
using Nexora.Domain.Projects;
using Nexora.Infrastructure.Services.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

internal sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("CalendarEvents", SchemaNames.Calendar, table => { table.HasCheckConstraint("ck_calendar_event_time_range", "\"startsAt\" < \"endsAt\""); });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.ProjectId).HasColumnName("projectId");
        builder.Property(x => x.MeetingId).HasColumnName("meetingId");
        builder.Property(x => x.OrganizerUserId).HasColumnName("organizerUserId").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.EventType).HasColumnName("eventType").HasMaxLength(30).IsRequired();
        builder.Property(x => x.StartsAt).HasColumnName("startsAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.EndsAt).HasColumnName("endsAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.AllDay).HasColumnName("allDay").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Location).HasColumnName("location").HasMaxLength(300);
        builder.Property(x => x.DescriptionCiphertext).HasColumnName("descriptionCiphertext").HasColumnType("bytea");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deletedAt").HasColumnType("timestamptz");
        builder.Property(x => x.DeleteBy).HasColumnName("deleteBy");
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.StartsAt, x.EndsAt });
        builder.HasIndex(x => new { x.OrganizationId, x.OrganizerUserId, x.StartsAt });
        builder.HasIndex(x => new { x.OrganizationId, x.ProjectId });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Meeting>().WithMany().HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OrganizerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DeleteBy).OnDelete(DeleteBehavior.Restrict);
    }
}
