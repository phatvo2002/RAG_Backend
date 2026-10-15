using MediatR;
using Nexora.Application.Common.Abstractions.Meetings;

namespace Nexora.Application.Features.V1.Meetings.Query;

public sealed record GetAgentTranscriptionJobsQuery() : IRequest<IReadOnlyList<AgentTranscriptionJob>>;

public sealed record AgentTranscriptionJob(Guid MeetingId, string RoomName, string Url, string Token);

public sealed class GetAgentTranscriptionJobsQueryHandler(IMeetingTranscriptStore transcripts, IMeetingRoomProvider rooms) :
    IRequestHandler<GetAgentTranscriptionJobsQuery, IReadOnlyList<AgentTranscriptionJob>>
{
    public async Task<IReadOnlyList<AgentTranscriptionJob>> Handle(GetAgentTranscriptionJobsQuery request,
        CancellationToken cancellationToken)
    {
        var meetings = await transcripts.ListActiveForAgentAsync(cancellationToken);
        return meetings.Select(meeting =>
        {
            var access = rooms.IssueTranscriptWorkerToken(meeting.RoomExternalId!, meeting.Id);
            return new AgentTranscriptionJob(meeting.Id, meeting.RoomExternalId!, access.Url, access.Token);
        }).ToArray();
    }
}
