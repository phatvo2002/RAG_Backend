using MediatR;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.Meetings.Query;

public sealed record GetAgentSpeakerLanguageQuery(Guid MeetingId, Guid SpeakerUserId) : IRequest<Result<string>>;

public sealed class GetAgentSpeakerLanguageQueryHandler(IMeetingTranscriptStore transcripts) :
    IRequestHandler<GetAgentSpeakerLanguageQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetAgentSpeakerLanguageQuery request, CancellationToken cancellationToken)
    {
        var meeting = await transcripts.FindMeetingForAgentAsync(request.MeetingId, cancellationToken);
        if (meeting is null || meeting.Status is not ("Live" or "Ending"))
            return Result<string>.Failure(Error.NotFound("Không tìm thấy cuộc họp đang diễn ra."));
        var language = await transcripts.GetSpeakerLanguageForAgentAsync(meeting.OrganizationId, meeting.Id,
            request.SpeakerUserId, cancellationToken);
        return language is null ? Result<string>.Failure(Error.Forbidden("Người nói không hợp lệ hoặc chưa chọn ngôn ngữ."))
            : Result<string>.Success(language);
    }
}
