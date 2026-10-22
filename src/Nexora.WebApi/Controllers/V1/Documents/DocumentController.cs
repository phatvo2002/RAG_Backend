using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Application.Features.V1.Documents.Command;
using Nexora.Application.Features.V1.Documents.Model;
using Nexora.Application.Features.V1.Documents.Query;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Controllers.V1.Documents;

[ApiController]
[Route("api/documents")]
public sealed class DocumentController(ISender sender) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? folderId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? fileType = null,
        [FromQuery] string? processingStatus = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default) =>
        this.ToSuccessResponse(await sender.Send(new ListDocumentsQuery(folderId, page, pageSize, search, fileType, processingStatus, departmentId), cancellationToken), "Lấy danh sách tài liệu thành công.");

    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> Get(Guid documentId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GetDocumentStatusQuery(documentId), cancellationToken));

    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress(CancellationToken cancellationToken) =>
        this.ToSuccessResponse(await sender.Send(new GetDocumentProgressQuery(), cancellationToken), "Lấy tiến độ xử lý tài liệu thành công.");

    [HttpGet("{documentId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid documentId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GetDocumentStatusQuery(documentId), cancellationToken));

    [HttpGet("{documentId:guid}/chunks")]
    public async Task<IActionResult> GetChunks(Guid documentId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default) =>
        this.ToActionResult(await sender.Send(new GetDocumentChunksQuery(documentId, page, pageSize), cancellationToken));

    [HttpPost]
    [EnableRateLimiting("write")]
    [RequestSizeLimit(26 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentRequest request, CancellationToken cancellationToken)
    {
        await using var content = request.File?.OpenReadStream() ?? Stream.Null;
        var result = await sender.Send(new UploadDocumentCommand(request.FolderId, request.File?.FileName ?? string.Empty, request.File?.ContentType ?? string.Empty, request.File?.Length ?? 0, content), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{documentId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new DeleteDocumentCommand(documentId), cancellationToken));
}
