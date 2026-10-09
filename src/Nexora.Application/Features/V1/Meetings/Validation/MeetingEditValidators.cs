using FluentValidation;
using Nexora.Application.Features.V1.Meetings.Command;
using Nexora.Application.Features.V1.Meetings.Query;

namespace Nexora.Application.Features.V1.Meetings.Validation;

public sealed class UpdateMeetingCommandValidator : AbstractValidator<UpdateMeetingCommand>
{
    public UpdateMeetingCommandValidator()
    {
        RuleFor(request => request.MeetingId).NotEmpty();
        RuleFor(request => request.Title).NotEmpty().MaximumLength(300);
        RuleFor(request => request.ScheduledEnd).GreaterThan(request => request.ScheduledStart);
        RuleFor(request => request.ExpectedRowVersion).GreaterThan(0);
        RuleFor(request => request.ParticipantUserIds).NotNull()
            .Must(ids => ids is not null && ids.All(id => id != Guid.Empty))
            .WithMessage("Danh sách người tham gia không hợp lệ.");
    }
}

public sealed class FindEligibleMeetingUsersQueryValidator : AbstractValidator<FindEligibleMeetingUsersQuery>
{
    public FindEligibleMeetingUsersQueryValidator()
    {
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Search).MaximumLength(100);
    }
}
