using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Meetings.Model;

namespace Nexora.Application.Features.V1.Meetings.Query;

public sealed record GetMeetingTranscriptQuery(Guid MeetingId) : IRequest<Result<IReadOnlyList<MeetingTranscriptResponse>>>;

public sealed class GetMeetingTranscriptQueryHandler(IMeetingStore meetings, IMeetingTranscriptStore transcripts,
    IUserContext user, ITenantContext tenant) : IRequestHandler<GetMeetingTranscriptQuery, Result<IReadOnlyList<MeetingTranscriptResponse>>>
{
    public async Task<Result<IReadOnlyList<MeetingTranscriptResponse>>> Handle(GetMeetingTranscriptQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId)
            return Result<IReadOnlyList<MeetingTranscriptResponse>>.Failure(Error.Unauthorized("Vui lòng đăng nhập."));
        if (await meetings.GetAsync(organizationId, request.MeetingId, cancellationToken) is null)
            return Result<IReadOnlyList<MeetingTranscriptResponse>>.Failure(Error.NotFound("Không tìm thấy cuộc họp."));
        if (!await meetings.IsParticipantAsync(organizationId, request.MeetingId, userId, cancellationToken))
            return Result<IReadOnlyList<MeetingTranscriptResponse>>.Failure(Error.Forbidden("Không có quyền xem transcript."));
        var segments = await transcripts.ListAsync(organizationId, request.MeetingId, cancellationToken);
        return Result<IReadOnlyList<MeetingTranscriptResponse>>.Success(segments.Select(MeetingTranscriptResponse.FromSegment).ToArray());
    }
}
