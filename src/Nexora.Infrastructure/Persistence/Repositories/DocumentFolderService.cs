using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Results;
using Nexora.Domain.Documents;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class DocumentFolderService(NexoraDbContext db, IDefaultOrganizationProvider organizations, IClock clock) : IDocumentFolderService
{
    public async Task<IReadOnlyList<FolderDto>> ListAsync(Guid? departmentId, CancellationToken ct) => await db.DocumentFolders.AsNoTracking().Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).OrderBy(x => x.Name).Select(x => new FolderDto(x.Id, x.DepartmentId, x.ParentFolderId, x.Name, x.OwnerUserId)).ToListAsync(ct);
    public async Task<FolderDto?> GetAsync(Guid id, CancellationToken ct) => await db.DocumentFolders.AsNoTracking().Where(x => x.Id == id).Select(x => new FolderDto(x.Id, x.DepartmentId, x.ParentFolderId, x.Name, x.OwnerUserId)).SingleOrDefaultAsync(ct);
    public async Task<Result<FolderDto>> CreateAsync(Guid userId, CreateFolderRequest r, CancellationToken ct)
    {
        var org = await organizations.GetOrganizationIdAsync(ct);
        if (!org.HasValue || !await db.Departments.AnyAsync(x => x.Id == r.DepartmentId && x.Status.ToString() == "Active", ct) || !await db.DepartmentMemberships.AnyAsync(x => x.DepartmentId == r.DepartmentId && x.UserId == userId && x.LeftAt == null, ct)) return Result<FolderDto>.Failure(Error.Forbidden("Bạn không có quyền với department."));
        if (r.ParentFolderId.HasValue && !await db.DocumentFolders.AnyAsync(x => x.Id == r.ParentFolderId && x.DepartmentId == r.DepartmentId, ct)) return Result<FolderDto>.Failure(Error.Validation("Parent folder không hợp lệ."));
        var folder = DocumentFolder.Create(org.Value, r.DepartmentId, userId, r.Name, clock.UtcNow, r.ParentFolderId); db.DocumentFolders.Add(folder); await db.SaveChangesAsync(ct);
        return Result<FolderDto>.Success(new FolderDto(folder.Id, folder.DepartmentId, folder.ParentFolderId, folder.Name, folder.OwnerUserId));
    }
    public async Task<Result<FolderDto>> UpdateAsync(Guid userId, Guid id, UpdateFolderRequest r, CancellationToken ct) { var f = await db.DocumentFolders.SingleOrDefaultAsync(x => x.Id == id, ct); if (f is null) return Result<FolderDto>.Failure(Error.NotFound("Folder không tồn tại.")); if (f.OwnerUserId != userId) return Result<FolderDto>.Failure(Error.Forbidden("Không có quyền quản lý folder.")); f.Name = r.Name; await db.SaveChangesAsync(ct); return Result<FolderDto>.Success(new FolderDto(f.Id, f.DepartmentId, f.ParentFolderId, f.Name, f.OwnerUserId)); }
    public async Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct) { var f = await db.DocumentFolders.SingleOrDefaultAsync(x => x.Id == id, ct); if (f is null) return Result.Failure(Error.NotFound("Folder không tồn tại.")); if (f.OwnerUserId != userId) return Result.Failure(Error.Forbidden("Không có quyền xóa folder.")); if (await db.Documents.AnyAsync(x => x.FolderId == id && x.DeletedAt == null, ct)) return Result.Failure(Error.Conflict("Folder còn tài liệu.")); f.MarkDeleted(userId, clock.UtcNow); await db.SaveChangesAsync(ct); return Result.Success(); }
    public async Task<IReadOnlyList<FolderGrantDto>> GrantsAsync(Guid id, CancellationToken ct) => await db.DocumentFolderAccessGrants.AsNoTracking().Where(x => x.FolderId == id && x.RevokedAt == null).Select(x => new FolderGrantDto(x.Id, x.FolderId, x.DepartmentId, x.UserId, x.AccessLevel, x.ValidFrom, x.ExpiresAt)).ToListAsync(ct);
    public async Task<Result<FolderGrantDto>> GrantAsync(Guid userId, Guid id, CreateFolderGrantRequest r, CancellationToken ct) { var f = await db.DocumentFolders.SingleOrDefaultAsync(x => x.Id == id, ct); if (f is null) return Result<FolderGrantDto>.Failure(Error.NotFound("Folder không tồn tại.")); if (f.OwnerUserId != userId) return Result<FolderGrantDto>.Failure(Error.Forbidden("Không có quyền cấp grant.")); try { var g = DocumentFolderAccessGrant.Create(f.OrganizationId, id, r.DepartmentId, r.UserId, r.AccessLevel, userId, clock.UtcNow, r.ExpiresAt); db.DocumentFolderAccessGrants.Add(g); await db.SaveChangesAsync(ct); return Result<FolderGrantDto>.Success(new FolderGrantDto(g.Id, g.FolderId, g.DepartmentId, g.UserId, g.AccessLevel, g.ValidFrom, g.ExpiresAt)); } catch (ArgumentException e) { return Result<FolderGrantDto>.Failure(Error.Validation(e.Message)); } }
    public async Task<Result> RevokeAsync(Guid userId, Guid id, Guid grantId, CancellationToken ct) { var f = await db.DocumentFolders.SingleOrDefaultAsync(x => x.Id == id, ct); var g = await db.DocumentFolderAccessGrants.SingleOrDefaultAsync(x => x.Id == grantId && x.FolderId == id, ct); if (f is null || g is null) return Result.Failure(Error.NotFound("Folder hoặc grant không tồn tại.")); if (f.OwnerUserId != userId) return Result.Failure(Error.Forbidden("Không có quyền thu hồi grant.")); g.Revoke(userId, clock.UtcNow); await db.SaveChangesAsync(ct); return Result.Success(); }
}
