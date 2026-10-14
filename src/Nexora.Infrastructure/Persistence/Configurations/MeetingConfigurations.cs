using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Meetings;
using Nexora.Domain.Organizations;
using Nexora.Domain.Projects;
using Nexora.Infrastructure.Services.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

internal sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("Meetings", SchemaNames.Meeting, table => { table.HasCheckConstraint("ck_meeting_schedule_range", "\"scheduledStart\" < \"scheduledEnd\""); });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.OrganizerUserId).HasColumnName("organizerUserId").IsRequired();
        builder.Property(x => x.ProjectId).HasColumnName("projectId");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.ScheduledStart).HasColumnName("scheduledStart").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ScheduledEnd).HasColumnName("scheduledEnd").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.ActualStartedAt).HasColumnName("actualStartedAt").HasColumnType("timestamptz");
        builder.Property(x => x.ActualEndedAt).HasColumnName("actualEndedAt").HasColumnType("timestamptz");
        builder.Property(x => x.RoomProvider).HasColumnName("roomProvider").HasMaxLength(80);
        builder.Property(x => x.RoomExternalId).HasColumnName("roomExternalId").HasMaxLength(200);
        builder.Property(x => x.RecordingConsentStatus).HasColumnName("recordingConsentStatus").HasMaxLength(30);
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deletedAt").HasColumnType("timestamptz");
        builder.Property(x => x.DeleteBy).HasColumnName("deleteBy");
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.Id }).IsUnique().HasDatabaseName("ux_meeting_tenant_id");
        builder.HasIndex(x => new { x.OrganizationId, x.ScheduledStart });
        builder.HasIndex(x => new { x.OrganizationId, x.ProjectId });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OrganizerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DeleteBy).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MeetingParticipantConfiguration : IEntityTypeConfiguration<MeetingParticipant>
{
    public void Configure(EntityTypeBuilder<MeetingParticipant> builder)
    {
        builder.ToTable("MeetingParticipants", SchemaNames.Meeting, table =>
        {
            table.HasCheckConstraint("ck_meeting_participant_identity", "\"userId\" IS NOT NULL OR \"guestEmail\" IS NOT NULL");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.MeetingId).HasColumnName("meetingId").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("userId");
        builder.Property(x => x.GuestEmail).HasColumnName("guestEmail").HasMaxLength(320);
        builder.Property(x => x.SourceLanguage).HasColumnName("sourceLanguage").HasMaxLength(10);
        builder.Property(x => x.TargetLanguage).HasColumnName("targetLanguage").HasMaxLength(10);
        builder.Property(x => x.JoinedAt).HasColumnName("joinedAt").HasColumnType("timestamptz");
        builder.Property(x => x.LeftAt).HasColumnName("leftAt").HasColumnType("timestamptz");

        builder.HasIndex(x => new { x.OrganizationId, x.MeetingId, x.UserId }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.MeetingId });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Meeting>().WithMany().HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
