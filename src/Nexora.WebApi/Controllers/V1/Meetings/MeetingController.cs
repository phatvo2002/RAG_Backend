using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Application.Features.V1.Meetings.Command;
using Nexora.Application.Features.V1.Meetings.Model;
using Nexora.Application.Features.V1.Meetings.Query;

namespace Nexora.WebApi.Controllers.V1.Meetings;

[ApiController]
[Route("api/meetings")]
public sealed class MeetingController(ISender sender) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new ListMeetingsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new GetMeetingQuery(id), cancellationToken));

    [HttpGet("eligible-participants")]
    public async Task<IActionResult> EligibleParticipants([FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        this.ToActionResult(await sender.Send(new FindEligibleMeetingUsersQuery(search, page, pageSize), cancellationToken));

    [HttpPost]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Create([FromBody] CreateMeetingRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new CreateMeetingCommand(request.Title, request.ScheduledStart, request.ScheduledEnd, request.ParticipantUserIds), cancellationToken));

    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMeetingRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new UpdateMeetingCommand(id, request.Title, request.ScheduledStart,
            request.ScheduledEnd, request.ParticipantUserIds, request.ExpectedRowVersion), cancellationToken));

    [HttpPost("{id:guid}/start")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new StartMeetingCommand(id), cancellationToken));

    [HttpPost("{id:guid}/end")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> End(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new EndMeetingCommand(id), cancellationToken));

    [HttpPost("{id:guid}/join-token")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Join(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new JoinMeetingCommand(id), cancellationToken));

    [HttpPut("{id:guid}/my-source-language")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> SetMySourceLanguage(Guid id, [FromBody] SetMeetingSourceLanguageRequest request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new SetMeetingSourceLanguageCommand(id, request.Language), cancellationToken));

    [HttpGet("{id:guid}/transcript")]
    public async Task<IActionResult> Transcript(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new GetMeetingTranscriptQuery(id), cancellationToken));
}
