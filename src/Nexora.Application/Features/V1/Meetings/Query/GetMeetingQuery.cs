using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Meetings.Model;

namespace Nexora.Application.Features.V1.Meetings.Query;

public sealed record GetMeetingQuery(Guid MeetingId) : IRequest<Result<MeetingDetailResponse>>;

public sealed class GetMeetingQueryHandler(IMeetingStore store, IMeetingTranscriptStore transcripts, IUserContext user, ITenantContext tenant) :
    IRequestHandler<GetMeetingQuery, Result<MeetingDetailResponse>>
{
    public async Task<Result<MeetingDetailResponse>> Handle(GetMeetingQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId)
            return Result<MeetingDetailResponse>.Failure(Error.Unauthorized("Vui lòng đăng nhập."));
        var meeting = await store.GetAsync(organizationId, request.MeetingId, cancellationToken);
        if (meeting is null) return Result<MeetingDetailResponse>.Failure(Error.NotFound("Không tìm thấy cuộc họp."));
        if (!await store.IsParticipantAsync(organizationId, meeting.Id, userId, cancellationToken))
            return Result<MeetingDetailResponse>.Failure(Error.Forbidden("Không có quyền xem cuộc họp."));
        var participants = await store.GetParticipantUsersAsync(organizationId, meeting.Id, cancellationToken);
        var language = await transcripts.GetMyLanguageAsync(organizationId, meeting.Id, userId, cancellationToken);
        return Result<MeetingDetailResponse>.Success(MeetingDetailResponse.FromMeeting(meeting, participants) with { MySourceLanguage = language });
    }
}
