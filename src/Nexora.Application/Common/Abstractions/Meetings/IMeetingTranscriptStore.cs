using Nexora.Domain.Meetings;

namespace Nexora.Application.Common.Abstractions.Meetings;

public interface IMeetingTranscriptStore
{
    Task<string?> GetMyLanguageAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken);
    Task<bool> SetMyLanguageAsync(Guid organizationId, Guid meetingId, Guid userId, string language, CancellationToken cancellationToken);
    Task<IReadOnlyList<MeetingTranscriptSegment>> ListAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken);
    Task<Meeting?> FindMeetingForAgentAsync(Guid meetingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Meeting>> ListActiveForAgentAsync(CancellationToken cancellationToken);
    Task<string?> GetSpeakerLanguageForAgentAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken);
    Task<bool> SaveFinalAsync(MeetingTranscriptSegment segment, CancellationToken cancellationToken);
}
