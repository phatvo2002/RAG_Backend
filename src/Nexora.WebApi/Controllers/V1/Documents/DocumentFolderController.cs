using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Features.V1.Folders.Commands;
using Nexora.Application.Features.V1.Folders.Model;
using Nexora.Application.Features.V1.Folders.Queries;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Controllers.V1.Documents;

[ApiController, Route("api/document-folders")]
public sealed class DocumentFolderController(ISender sender) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> List(Guid? departmentId, CancellationToken cancellationToken) =>
        this.ToSuccessResponse(
            await sender.Send(new ListDocumentFoldersQuery(departmentId), cancellationToken),
            "Lấy danh sách folder thành công.");

    [HttpGet("{folderId:guid}")]
    public async Task<IActionResult> Get(Guid folderId, CancellationToken cancellationToken)
    {
        var folder = await sender.Send(new GetDocumentFolderQuery(folderId), cancellationToken);
        return folder is null ? NotFound() : Ok(folder);
    }

    [HttpGet("{folderId:guid}/access-grants")]
    public async Task<IActionResult> Grants(Guid folderId, CancellationToken cancellationToken) =>
        this.ToSuccessResponse(
            await sender.Send(new ListFolderGrantsQuery(folderId), cancellationToken),
            "Lấy danh sách quyền folder thành công.");

    [HttpPost]
    public async Task<IActionResult> Create(CreateFolderRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(
            await sender.Send(
                new CreateFolderCommand(request.DepartmentId, request.Name, request.ParentFolderId),
                cancellationToken),
            "Tạo folder thành công.");

    [HttpPost("{folderId:guid}/access-grants")]
    public async Task<IActionResult> Grant(Guid folderId, CreateFolderGrantRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(
            await sender.Send(
                new GrantFolderAccessCommand(folderId, request.DepartmentId, request.UserId, request.AccessLevel, request.ExpiresAt),
                cancellationToken),
            "Cấp quyền folder thành công.");

    [HttpPut("{folderId:guid}")]
    public async Task<IActionResult> Update(Guid folderId, UpdateFolderRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(
            await sender.Send(new UpdateFolderCommand(folderId, request.Name), cancellationToken),
            "Cập nhật folder thành công.");

    [HttpDelete("{folderId:guid}")]
    public async Task<IActionResult> Delete(Guid folderId, CancellationToken cancellationToken) =>
        this.ToActionResult(
            await sender.Send(new DeleteFolderCommand(folderId), cancellationToken),
            "Xóa folder thành công.");

    [HttpDelete("{folderId:guid}/access-grants/{grantId:guid}")]
    public async Task<IActionResult> Revoke(Guid folderId, Guid grantId, CancellationToken cancellationToken) =>
        this.ToActionResult(
            await sender.Send(new RevokeFolderAccessCommand(folderId, grantId), cancellationToken),
            "Thu hồi quyền folder thành công.");
}
