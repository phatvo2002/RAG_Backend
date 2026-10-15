using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Meetings;
using Nexora.Infrastructure.Services.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

internal sealed class MeetingTranscriptConfiguration : IEntityTypeConfiguration<MeetingTranscriptSegment>
{
    public void Configure(EntityTypeBuilder<MeetingTranscriptSegment> builder)
    {
        builder.ToTable("MeetingTranscriptSegments", SchemaNames.Meeting);
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(s => s.MeetingId).HasColumnName("meetingId").IsRequired();
        builder.Property(s => s.SpeakerUserId).HasColumnName("speakerUserId").IsRequired();
        builder.Property(s => s.StartedAt).HasColumnName("startedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.EndedAt).HasColumnName("endedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.SourceLanguage).HasColumnName("sourceLanguage").HasMaxLength(2).IsRequired();
        builder.Property(s => s.Text).HasColumnName("text").HasMaxLength(5000).IsRequired();
        builder.HasIndex(s => new { s.MeetingId, s.Id }).IsUnique();
        builder.HasIndex(s => new { s.OrganizationId, s.MeetingId, s.StartedAt });
        builder.HasOne<Meeting>().WithMany().HasForeignKey(s => s.MeetingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(s => s.SpeakerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
