namespace Nexora.Application.Features.V1.Folders.Model;

public sealed record FolderDto(
    Guid Id,
    Guid? DepartmentId,
    Guid? ParentFolderId,
    string Name,
    Guid OwnerUserId);

public sealed record FolderGrantDto(
    Guid Id,
    Guid FolderId,
    Guid? DepartmentId,
    Guid? UserId,
    string AccessLevel,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ExpiresAt);

public sealed record CreateFolderRequest(
    Guid DepartmentId,
    string Name,
    Guid? ParentFolderId);

public sealed record UpdateFolderRequest(string Name);

public sealed record CreateFolderGrantRequest(
    Guid? DepartmentId,
    Guid? UserId,
    string AccessLevel,
    DateTimeOffset? ExpiresAt);
