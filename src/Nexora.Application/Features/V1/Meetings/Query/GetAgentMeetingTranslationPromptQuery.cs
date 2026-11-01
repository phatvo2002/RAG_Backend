using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemPrompts.Model;

namespace Nexora.Application.Features.V1.Meetings.Query;

public sealed record GetAgentMeetingTranslationPromptQuery(Guid MeetingId) : IRequest<Result<MeetingAgentSystemPromptResponse>>;

public sealed class GetAgentMeetingTranslationPromptQueryHandler(IMeetingTranscriptStore meetings, ISystemPromptStore prompts)
    : IRequestHandler<GetAgentMeetingTranslationPromptQuery, Result<MeetingAgentSystemPromptResponse>>
{
    public async Task<Result<MeetingAgentSystemPromptResponse>> Handle(GetAgentMeetingTranslationPromptQuery request,
        CancellationToken cancellationToken)
    {
        var meeting = await meetings.FindMeetingForAgentAsync(request.MeetingId, cancellationToken);
        if (meeting is null || meeting.Status is not ("Live" or "Ending"))
            return Result<MeetingAgentSystemPromptResponse>.Failure(Error.NotFound("Không tìm thấy cuộc họp đang diễn ra."));
        var version = await prompts.GetActiveVersionAsync(meeting.OrganizationId,
            SystemPromptTypes.MeetingTranslation, cancellationToken);
        return Result<MeetingAgentSystemPromptResponse>.Success(new MeetingAgentSystemPromptResponse(version.Id, version.Instructions));
    }
}
