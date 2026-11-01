using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.Meetings.Command;

public sealed record SetMeetingTargetLanguageCommand(Guid MeetingId, string Language) : IRequest<Result<string>>;

public sealed class SetMeetingTargetLanguageCommandHandler(IMeetingStore meetings, IMeetingTranscriptStore transcripts,
    IUserContext user, ITenantContext tenant) : IRequestHandler<SetMeetingTargetLanguageCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SetMeetingTargetLanguageCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId)
            return Result<string>.Failure(Error.Unauthorized("Vui lòng đăng nhập."));
        var meeting = await meetings.GetAsync(organizationId, request.MeetingId, cancellationToken);
        if (meeting is null) return Result<string>.Failure(Error.NotFound("Không tìm thấy cuộc họp."));
        if (meeting.Status is "Ending" or "Ended")
            return Result<string>.Failure(Error.Conflict("Cuộc họp đã kết thúc."));
        if (!await meetings.IsParticipantAsync(organizationId, request.MeetingId, userId, cancellationToken))
            return Result<string>.Failure(Error.Forbidden("Bạn không có trong danh sách tham gia."));
        if (request.Language is not ("vi" or "en"))
            return Result<string>.Failure(Error.Validation("Ngôn ngữ phải là vi hoặc en."));
        if (!await transcripts.SetMyTargetLanguageAsync(organizationId, request.MeetingId, userId, request.Language, cancellationToken))
            return Result<string>.Failure(Error.Forbidden("Bạn không có trong danh sách tham gia."));
        return Result<string>.Success(request.Language);
    }
}
