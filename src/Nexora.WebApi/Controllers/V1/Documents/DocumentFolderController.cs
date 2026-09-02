using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Features.V1.Documents.Folder;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Controllers.V1.Documents;

[ApiController, Route("api/document-folders")]
public sealed class DocumentFolderController(ISender sender, IUserContext userContext) : BaseController
{
    [HttpGet] public async Task<IActionResult> List(Guid? departmentId, CancellationToken ct) => this.ToSuccessResponse(await sender.Send(new ListDocumentFoldersQuery(departmentId), ct), "Lấy danh sách folder thành công.");
    [HttpGet("{folderId:guid}")] public async Task<IActionResult> Get(Guid folderId, CancellationToken ct) => await sender.Send(new GetDocumentFolderQuery(folderId), ct) is { } folder ? Ok(folder) : NotFound();
    [HttpGet("{folderId:guid}/access-grants")] public async Task<IActionResult> Grants(Guid folderId, CancellationToken ct) => this.ToSuccessResponse(await sender.Send(new ListFolderGrantsQuery(folderId), ct), "Lấy danh sách quyền folder thành công.");
    [HttpPost] public async Task<IActionResult> Create(CreateFolderRequest request, CancellationToken ct) => this.ToActionResult(await sender.Send(new CreateDocumentFolderCommand(userContext.UserId ?? Guid.Empty, request), ct));
    [HttpPost("{folderId:guid}/access-grants")] public async Task<IActionResult> Grant(Guid folderId, CreateFolderGrantRequest request, CancellationToken ct) => this.ToActionResult(await sender.Send(new GrantFolderCommand(userContext.UserId ?? Guid.Empty, folderId, request), ct));
    [HttpPut("{folderId:guid}")] public async Task<IActionResult> Update(Guid folderId, UpdateFolderRequest request, CancellationToken ct) => this.ToActionResult(await sender.Send(new UpdateDocumentFolderCommand(userContext.UserId ?? Guid.Empty, folderId, request), ct));
    [HttpDelete("{folderId:guid}")] public async Task<IActionResult> Delete(Guid folderId, CancellationToken ct) => this.ToActionResult(await sender.Send(new DeleteDocumentFolderCommand(userContext.UserId ?? Guid.Empty, folderId), ct));
    [HttpDelete("{folderId:guid}/access-grants/{grantId:guid}")] public async Task<IActionResult> Revoke(Guid folderId, Guid grantId, CancellationToken ct) => this.ToActionResult(await sender.Send(new RevokeFolderGrantCommand(userContext.UserId ?? Guid.Empty, folderId, grantId), ct));
}
