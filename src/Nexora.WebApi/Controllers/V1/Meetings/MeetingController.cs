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

    [HttpPost]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Create([FromBody] CreateMeetingRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new CreateMeetingCommand(request.Title, request.ScheduledStart, request.ScheduledEnd, request.ParticipantUserIds), cancellationToken));

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
}
