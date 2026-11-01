using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Domain.Chat;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Persistence.Repositories.Chat;

internal sealed class SystemPromptStore(NexoraDbContext db, IClock clock) : ISystemPromptStore
{
    public async Task<IReadOnlyList<SystemPromptData>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        foreach (var type in SystemPromptTypes.All)
            await EnsureProfileAsync(organizationId, type, cancellationToken);

        var profiles = await db.OrganizationSystemPrompts.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .Include(x => x.ActiveVersion)
            .ToListAsync(cancellationToken);
        var ids = profiles.Select(x => x.Id).ToArray();
        var drafts = await db.SystemPromptVersions.IgnoreQueryFilters().AsNoTracking()
            .Where(x => ids.Contains(x.SystemPromptId) && x.Status == SystemPromptVersion.DraftStatus)
            .ToDictionaryAsync(x => x.SystemPromptId, cancellationToken);
        return profiles.Select(profile => ToData(profile,
            drafts.TryGetValue(profile.Id, out var draft) ? draft : null)).ToArray();
    }

    public async Task<SystemPromptData?> GetAsync(Guid organizationId, string promptType, CancellationToken cancellationToken)
    {
        var profile = await EnsureProfileAsync(organizationId, promptType, cancellationToken);
        var loaded = await LoadProfileAsync(organizationId, profile.Id, cancellationToken);
        if (loaded?.ActiveVersion is null) return null;
        var draft = await db.SystemPromptVersions.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(x => x.SystemPromptId == loaded.Id && x.Status == SystemPromptVersion.DraftStatus, cancellationToken);
        return ToData(loaded, draft);
    }

    public async Task<IReadOnlyList<SystemPromptVersionData>> GetVersionsAsync(Guid organizationId, string promptType,
        CancellationToken cancellationToken)
    {
        var profile = await EnsureProfileAsync(organizationId, promptType, cancellationToken);
        var versions = await db.SystemPromptVersions.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.SystemPromptId == profile.Id).OrderByDescending(x => x.VersionNo)
            .ToListAsync(cancellationToken);
        return versions.Select(ToData).ToArray();
    }

    public async Task<SystemPromptVersionData> GetActiveVersionAsync(Guid organizationId, string promptType,
        CancellationToken cancellationToken)
    {
        var profile = await EnsureProfileAsync(organizationId, promptType, cancellationToken);
        var version = await db.SystemPromptVersions.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(x => x.Id == profile.ActiveVersionId && x.SystemPromptId == profile.Id, cancellationToken);
        return ToData(version);
    }

    public async Task<SystemPromptVersionData> SaveDraftAsync(Guid organizationId, string promptType,
        string instructions, Guid userId, CancellationToken cancellationToken)
    {
        var profile = await EnsureProfileAsync(organizationId, promptType, cancellationToken);
        var draft = await db.SystemPromptVersions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.SystemPromptId == profile.Id && x.Status == SystemPromptVersion.DraftStatus, cancellationToken);
        if (draft is null)
        {
            var nextVersion = (await db.SystemPromptVersions.IgnoreQueryFilters()
                .Where(x => x.SystemPromptId == profile.Id).MaxAsync(x => (int?)x.VersionNo, cancellationToken) ?? 0) + 1;
            draft = SystemPromptVersion.Create(profile.Id, nextVersion, instructions, userId, clock.UtcNow);
            db.SystemPromptVersions.Add(draft);
        }
        else
        {
            draft.UpdateDraft(instructions);
        }
        await db.SaveChangesAsync(cancellationToken);
        return ToData(draft);
    }

    public async Task<SystemPromptVersionData?> PublishDraftAsync(Guid organizationId, string promptType,
        Guid userId, CancellationToken cancellationToken)
    {
        var profile = await EnsureProfileAsync(organizationId, promptType, cancellationToken);
        var draft = await db.SystemPromptVersions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.SystemPromptId == profile.Id && x.Status == SystemPromptVersion.DraftStatus, cancellationToken);
        if (draft is null) return null;
        draft.Publish(userId, clock.UtcNow);
        profile.Activate(draft);
        await db.SaveChangesAsync(cancellationToken);
        return ToData(draft);
    }

    public async Task<SystemPromptVersionData?> RestoreAsync(Guid organizationId, string promptType,
        Guid versionId, Guid userId, CancellationToken cancellationToken)
    {
        var profile = await EnsureProfileAsync(organizationId, promptType, cancellationToken);
        var source = await db.SystemPromptVersions.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == versionId && x.SystemPromptId == profile.Id &&
                x.Status == SystemPromptVersion.PublishedStatus, cancellationToken);
        if (source is null) return null;
        var nextVersion = (await db.SystemPromptVersions.IgnoreQueryFilters()
            .Where(x => x.SystemPromptId == profile.Id).MaxAsync(x => (int?)x.VersionNo, cancellationToken) ?? 0) + 1;
        var restored = SystemPromptVersion.Create(profile.Id, nextVersion, source.Instructions, userId, clock.UtcNow, published: true);
        db.SystemPromptVersions.Add(restored);
        profile.Activate(restored);
        await db.SaveChangesAsync(cancellationToken);
        return ToData(restored);
    }

    private async Task<OrganizationSystemPrompt> EnsureProfileAsync(Guid organizationId, string promptType,
        CancellationToken cancellationToken)
    {
        if (!SystemPromptTypes.All.Contains(promptType, StringComparer.Ordinal))
            throw new ArgumentOutOfRangeException(nameof(promptType));

        var existing = await db.OrganizationSystemPrompts.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.PromptType == promptType, cancellationToken);
        if (existing is not null) return existing;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            existing = await db.OrganizationSystemPrompts.IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.PromptType == promptType, cancellationToken);
            if (existing is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return existing;
            }

            var profile = OrganizationSystemPrompt.Create(organizationId, promptType, clock.UtcNow);
            db.OrganizationSystemPrompts.Add(profile);
            await db.SaveChangesAsync(cancellationToken);
            var defaultInstructions = promptType == SystemPromptTypes.Chat
                ? SystemPromptDefaults.Chat
                : SystemPromptDefaults.MeetingTranslation;
            var initialVersion = SystemPromptVersion.Create(profile.Id, 1, defaultInstructions, null, clock.UtcNow, published: true);
            db.SystemPromptVersions.Add(initialVersion);
            await db.SaveChangesAsync(cancellationToken);
            profile.Activate(initialVersion);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return profile;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            var concurrent = await db.OrganizationSystemPrompts.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.PromptType == promptType, cancellationToken);
            if (concurrent is not null) return concurrent;
            throw;
        }
    }

    private Task<OrganizationSystemPrompt?> LoadProfileAsync(Guid organizationId, Guid profileId, CancellationToken cancellationToken) =>
        db.OrganizationSystemPrompts.IgnoreQueryFilters().Include(x => x.ActiveVersion)
            .SingleOrDefaultAsync(x => x.Id == profileId && x.OrganizationId == organizationId, cancellationToken);

    private static SystemPromptData ToData(OrganizationSystemPrompt profile, SystemPromptVersion? draft) =>
        new(profile.Id, profile.PromptType, profile.RowVersion,
            ToData(profile.ActiveVersion ?? throw new InvalidOperationException("Prompt profile has no active version.")),
            draft is null ? null : ToData(draft));

    private static SystemPromptVersionData ToData(SystemPromptVersion version) =>
        new(version.Id, version.VersionNo, version.Instructions, version.Status, version.CreatedByUserId,
            version.CreatedAt, version.PublishedByUserId, version.PublishedAt);
}
