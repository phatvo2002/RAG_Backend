using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Features.V1.Meetings.Command;
using Nexora.Application.Features.V1.Meetings.Model;
using Nexora.Application.Features.V1.Meetings.Query;
using Nexora.WebApi.Authentication;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Controllers.Internal.Meetings;

[ApiController]
[Authorize(AuthenticationSchemes = MeetingAgentAuthenticationHandler.SchemeName)]
[Route("internal/meetings/{id:guid}")]
public sealed class MeetingAgentTranscriptController(ISender sender) : ControllerBase
{
    [HttpGet("/internal/meetings/transcription-jobs")]
    public async Task<IActionResult> ListJobs(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetAgentTranscriptionJobsQuery(), cancellationToken));

    [HttpGet("speakers/{speakerId:guid}/source-language")]
    public async Task<IActionResult> GetSourceLanguage(Guid id, Guid speakerId, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new GetAgentSpeakerLanguageQuery(id, speakerId), cancellationToken));

    [HttpPut("transcript/{segmentId:guid}")]
    public async Task<IActionResult> Save(Guid id, Guid segmentId, [FromBody] SaveMeetingTranscriptRequest request,
        CancellationToken cancellationToken)
    {
        if (segmentId != request.SegmentId) return BadRequest();
        return this.ToActionResult(await sender.Send(new SaveMeetingTranscriptCommand(id, segmentId,
            request.SpeakerUserId, request.StartedAt, request.EndedAt, request.SourceLanguage, request.Text), cancellationToken));
    }
}
