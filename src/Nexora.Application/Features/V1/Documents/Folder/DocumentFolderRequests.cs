using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.Documents.Folder;
public sealed record ListDocumentFoldersQuery(Guid? DepartmentId) : IRequest<IReadOnlyList<FolderDto>>;
public sealed class ListDocumentFoldersHandler(IDocumentFolderService service) : IRequestHandler<ListDocumentFoldersQuery, IReadOnlyList<FolderDto>> { public Task<IReadOnlyList<FolderDto>> Handle(ListDocumentFoldersQuery r, CancellationToken c) => service.ListAsync(r.DepartmentId, c); }
public sealed record GetDocumentFolderQuery(Guid FolderId) : IRequest<FolderDto?>;
public sealed class GetDocumentFolderHandler(IDocumentFolderService service) : IRequestHandler<GetDocumentFolderQuery, FolderDto?> { public Task<FolderDto?> Handle(GetDocumentFolderQuery r, CancellationToken c) => service.GetAsync(r.FolderId, c); }
public sealed record CreateDocumentFolderCommand(Guid UserId, CreateFolderRequest Request) : IRequest<Result<FolderDto>>;
public sealed class CreateDocumentFolderHandler(IDocumentFolderService service) : IRequestHandler<CreateDocumentFolderCommand, Result<FolderDto>> { public Task<Result<FolderDto>> Handle(CreateDocumentFolderCommand r, CancellationToken c) => service.CreateAsync(r.UserId, r.Request, c); }
public sealed record UpdateDocumentFolderCommand(Guid UserId, Guid FolderId, UpdateFolderRequest Request) : IRequest<Result<FolderDto>>;
public sealed class UpdateDocumentFolderHandler(IDocumentFolderService service) : IRequestHandler<UpdateDocumentFolderCommand, Result<FolderDto>> { public Task<Result<FolderDto>> Handle(UpdateDocumentFolderCommand r, CancellationToken c) => service.UpdateAsync(r.UserId, r.FolderId, r.Request, c); }
public sealed record DeleteDocumentFolderCommand(Guid UserId, Guid FolderId) : IRequest<Result>;
public sealed class DeleteDocumentFolderHandler(IDocumentFolderService service) : IRequestHandler<DeleteDocumentFolderCommand, Result> { public Task<Result> Handle(DeleteDocumentFolderCommand r, CancellationToken c) => service.DeleteAsync(r.UserId, r.FolderId, c); }
public sealed record ListFolderGrantsQuery(Guid FolderId) : IRequest<IReadOnlyList<FolderGrantDto>>;
public sealed class ListFolderGrantsHandler(IDocumentFolderService service) : IRequestHandler<ListFolderGrantsQuery, IReadOnlyList<FolderGrantDto>> { public Task<IReadOnlyList<FolderGrantDto>> Handle(ListFolderGrantsQuery r, CancellationToken c) => service.GrantsAsync(r.FolderId, c); }
public sealed record GrantFolderCommand(Guid UserId, Guid FolderId, CreateFolderGrantRequest Request) : IRequest<Result<FolderGrantDto>>;
public sealed class GrantFolderHandler(IDocumentFolderService service) : IRequestHandler<GrantFolderCommand, Result<FolderGrantDto>> { public Task<Result<FolderGrantDto>> Handle(GrantFolderCommand r, CancellationToken c) => service.GrantAsync(r.UserId, r.FolderId, r.Request, c); }
public sealed record RevokeFolderGrantCommand(Guid UserId, Guid FolderId, Guid GrantId) : IRequest<Result>;
public sealed class RevokeFolderGrantHandler(IDocumentFolderService service) : IRequestHandler<RevokeFolderGrantCommand, Result> { public Task<Result> Handle(RevokeFolderGrantCommand r, CancellationToken c) => service.RevokeAsync(r.UserId, r.FolderId, r.GrantId, c); }
