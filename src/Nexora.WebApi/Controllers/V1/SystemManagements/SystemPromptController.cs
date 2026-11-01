using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Features.V1.SystemPrompts.Command;
using Nexora.Application.Features.V1.SystemPrompts.Model;
using Nexora.Application.Features.V1.SystemPrompts.Query;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[ApiController]
[Route("api/system-prompts")]
public sealed class SystemPromptController(ISender sender) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new GetSystemPromptsQuery(), cancellationToken));

    [HttpGet("{promptType}/versions")]
    public async Task<IActionResult> Versions(string promptType, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new GetSystemPromptVersionsQuery(promptType.ToUpperInvariant()), cancellationToken));

    [HttpPut("{promptType}/draft")]
    public async Task<IActionResult> SaveDraft(string promptType, [FromBody] SaveSystemPromptDraftRequest request,
        CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(
            new SaveSystemPromptDraftCommand(promptType.ToUpperInvariant(), request.Instructions), cancellationToken));

    [HttpPost("{promptType}/preview")]
    public async Task<IActionResult> Preview(string promptType, [FromBody] PreviewSystemPromptRequest request,
        CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(
            new PreviewSystemPromptCommand(promptType.ToUpperInvariant(), request.Question, request.Instructions), cancellationToken));

    [HttpPost("{promptType}/publish")]
    public async Task<IActionResult> Publish(string promptType, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new PublishSystemPromptCommand(promptType.ToUpperInvariant()), cancellationToken));

    [HttpPost("{promptType}/versions/{versionId:guid}/restore")]
    public async Task<IActionResult> Restore(string promptType, Guid versionId, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new RestoreSystemPromptVersionCommand(promptType.ToUpperInvariant(), versionId), cancellationToken));
}
