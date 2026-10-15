using MediatR;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Domain.Meetings;

namespace Nexora.Application.Features.V1.Meetings.Command;

public sealed record SaveMeetingTranscriptCommand(Guid MeetingId, Guid SegmentId, Guid SpeakerUserId,
    DateTimeOffset StartedAt, DateTimeOffset EndedAt, string SourceLanguage, string Text) : IRequest<Result>;

public sealed class SaveMeetingTranscriptCommandHandler(IMeetingTranscriptStore transcripts) :
    IRequestHandler<SaveMeetingTranscriptCommand, Result>
{
    public async Task<Result> Handle(SaveMeetingTranscriptCommand request, CancellationToken cancellationToken)
    {
        var meeting = await transcripts.FindMeetingForAgentAsync(request.MeetingId, cancellationToken);
        if (meeting is null || meeting.Status is not ("Live" or "Ending" or "Ended"))
            return Result.Failure(Error.NotFound("Không tìm thấy cuộc họp."));
        if (meeting.ActualStartedAt is not { } actualStart || request.StartedAt < actualStart.AddSeconds(-5) ||
            (meeting.ActualEndedAt is { } actualEnd && request.EndedAt > actualEnd.AddSeconds(5)))
            return Result.Failure(Error.Validation("Thời gian đoạn nói nằm ngoài cuộc họp."));
        if (request.SegmentId == Guid.Empty || request.SpeakerUserId == Guid.Empty ||
            request.SourceLanguage is not ("vi" or "en") || string.IsNullOrWhiteSpace(request.Text) ||
            request.Text.Length > 5000 || request.StartedAt >= request.EndedAt)
            return Result.Failure(Error.Validation("Đoạn transcript không hợp lệ."));
        var language = await transcripts.GetSpeakerLanguageForAgentAsync(meeting.OrganizationId, meeting.Id,
            request.SpeakerUserId, cancellationToken);
        if (language is null) return Result.Failure(Error.Forbidden("Người nói không thuộc cuộc họp."));
        var segment = MeetingTranscriptSegment.Create(request.SegmentId, meeting.OrganizationId, meeting.Id,
            request.SpeakerUserId, request.StartedAt, request.EndedAt, request.SourceLanguage, request.Text);
        if (!await transcripts.SaveFinalAsync(segment, cancellationToken))
            return Result.Failure(Error.Conflict("ID đoạn nói đã được dùng với nội dung khác."));
        return Result.Success();
    }
}
