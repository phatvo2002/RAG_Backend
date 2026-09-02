using Nexora.Application.Common.Results;

namespace Nexora.Application.Common.Abstractions.Documents;

public sealed record FolderDto(Guid Id, Guid? DepartmentId, Guid? ParentFolderId, string Name, Guid OwnerUserId);
public sealed record FolderGrantDto(Guid Id, Guid FolderId, Guid? DepartmentId, Guid? UserId, string AccessLevel, DateTimeOffset ValidFrom, DateTimeOffset? ExpiresAt);
public sealed record CreateFolderRequest(Guid DepartmentId, string Name, Guid? ParentFolderId);
public sealed record UpdateFolderRequest(string Name);
public sealed record CreateFolderGrantRequest(Guid? DepartmentId, Guid? UserId, string AccessLevel, DateTimeOffset? ExpiresAt);

public interface IDocumentFolderService
{
    Task<IReadOnlyList<FolderDto>> ListAsync(Guid? departmentId, CancellationToken cancellationToken);
    Task<FolderDto?> GetAsync(Guid folderId, CancellationToken cancellationToken);
    Task<Result<FolderDto>> CreateAsync(Guid userId, CreateFolderRequest request, CancellationToken cancellationToken);
    Task<Result<FolderDto>> UpdateAsync(Guid userId, Guid folderId, UpdateFolderRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(Guid userId, Guid folderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FolderGrantDto>> GrantsAsync(Guid folderId, CancellationToken cancellationToken);
    Task<Result<FolderGrantDto>> GrantAsync(Guid userId, Guid folderId, CreateFolderGrantRequest request, CancellationToken cancellationToken);
    Task<Result> RevokeAsync(Guid userId, Guid folderId, Guid grantId, CancellationToken cancellationToken);
}
