using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Domain.Authorization;
using Nexora.Domain.Calendar;
using Nexora.Domain.Chat;
using Nexora.Domain.Documents;
using Nexora.Domain.Meetings;
using Nexora.Domain.Organizations;
using Nexora.Domain.Projects;
using Nexora.Domain.Rag;
using Nexora.Domain.Security;
using TaskEntity = Nexora.Domain.Tasks.Task;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Persistence;

public sealed class NexoraDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly IUserContext? _userContext;
    private readonly ITenantContext? _tenantContext;
    private readonly IClock? _clock;

    public NexoraDbContext(DbContextOptions<NexoraDbContext> options, IUserContext? userContext = null, ITenantContext? tenantContext = null, IClock? clock = null)
        : base(options)
    {
        _userContext = userContext;
        _tenantContext = tenantContext;
        _clock = clock;
    }

    private Guid? TenantOrganizationId => _tenantContext?.OrganizationId;
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    public DbSet<OrganizationRoleAssignment> OrganizationRoleAssignments => Set<OrganizationRoleAssignment>();

    public DbSet<OrganizationSetting> OrganizationSettings => Set<OrganizationSetting>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<DepartmentMembership> DepartmentMemberships => Set<DepartmentMembership>();

    public DbSet<ApiFunction> ApiFunctions => Set<ApiFunction>();

    public DbSet<RoleApiFunction> RoleApiFunctions => Set<RoleApiFunction>();

    public DbSet<AccessToken> AccessTokens => Set<AccessToken>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<ProjectDocument> ProjectDocuments => Set<ProjectDocument>();

    public DbSet<DocumentFolder> DocumentFolders => Set<DocumentFolder>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    public DbSet<DocumentAccessGrant> DocumentAccessGrants => Set<DocumentAccessGrant>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<DocumentChunkContent> DocumentChunkContents => Set<DocumentChunkContent>();

    public DbSet<RagIndex> RagIndexes => Set<RagIndex>();

    public DbSet<RagIndexRecord> RagIndexRecords => Set<RagIndexRecord>();

    public DbSet<RetrievalRun> RetrievalRuns => Set<RetrievalRun>();

    public DbSet<RetrievalHit> RetrievalHits => Set<RetrievalHit>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationContext> ConversationContexts => Set<ConversationContext>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<AiTurn> AiTurns => Set<AiTurn>();

    public DbSet<Citation> Citations => Set<Citation>();

    public DbSet<Meeting> Meetings => Set<Meeting>();

    public DbSet<MeetingParticipant> MeetingParticipants => Set<MeetingParticipant>();

    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().ToTable("AspNetUsers", SchemaNames.Identity);
        builder.Entity<ApplicationRole>().ToTable("AspNetRoles", SchemaNames.Identity);
        builder.Entity<IdentityUserRole<Guid>>().ToTable("AspNetUserRoles", SchemaNames.Identity);
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("AspNetUserClaims", SchemaNames.Identity);
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("AspNetRoleClaims", SchemaNames.Identity);
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("AspNetUserLogins", SchemaNames.Identity);
        builder.Entity<IdentityUserToken<Guid>>().ToTable("AspNetUserTokens", SchemaNames.Identity);

        builder.ApplyConfigurationsFromAssembly(typeof(NexoraDbContext).Assembly);

        builder.Entity<Organization>().HasQueryFilter(x => x.Id == TenantOrganizationId && x.DeletedAt == null);
        builder.Entity<UserProfile>().HasQueryFilter(x => x.DeletedAt == null);
        builder.Entity<OrganizationMembership>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<OrganizationRoleAssignment>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<OrganizationSetting>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<Department>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId && x.DeletedAt == null);
        builder.Entity<DepartmentMembership>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<RoleApiFunction>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<AccessToken>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<ActivityLog>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<Project>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId && x.DeletedAt == null);
        builder.Entity<ProjectMember>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<ProjectDocument>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId && x.DeletedAt == null);
        builder.Entity<DocumentFolder>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId && x.DeletedAt == null);
        builder.Entity<Document>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId && x.DeletedAt == null);
        builder.Entity<DocumentVersion>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<DocumentAccessGrant>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<DocumentChunk>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<RagIndex>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<RagIndexRecord>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<RetrievalRun>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<RetrievalHit>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<Conversation>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId && x.DeletedAt == null);
        builder.Entity<ConversationContext>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<Message>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<AiTurn>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<Citation>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<Meeting>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId && x.DeletedAt == null);
        builder.Entity<MeetingParticipant>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId);
        builder.Entity<TaskEntity>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId && x.DeletedAt == null);
        builder.Entity<CalendarEvent>().HasQueryFilter(x => x.OrganizationId == TenantOrganizationId && x.DeletedAt == null);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditValues();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditValues()
    {
        var now = _clock?.UtcNow ?? DateTimeOffset.UtcNow;
        var userId = _userContext?.UserId;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            SetDateIfPropertyExists(entry, "UpdatedAt", now);
            SetUserIfPropertyExists(entry, "UpdatedByUserId", userId);

            if (entry.State == EntityState.Added)
            {
                SetDateIfDefault(entry, "CreatedAt", now);
                SetLongIfDefault(entry, "RowVersion", 1);
            }
            else if (entry.State == EntityState.Modified)
            {
                IncrementLongIfPropertyExists(entry, "RowVersion");
            }

            if (entry.State == EntityState.Modified &&
                entry.Metadata.FindProperty("DeletedAt") is not null &&
                entry.Property("DeletedAt").IsModified &&
                entry.Property("DeletedAt").OriginalValue is null &&
                entry.Property("DeletedAt").CurrentValue is DateTimeOffset)
            {
                SetUserIfPropertyExists(entry, "DeleteBy", userId);
            }
        }
    }

    private static void SetDateIfPropertyExists(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName, DateTimeOffset value)
    {
        if (entry.Metadata.FindProperty(propertyName) is not null)
        {
            entry.Property(propertyName).CurrentValue = value;
        }
    }

    private static void SetDateIfDefault(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName, DateTimeOffset value)
    {
        if (entry.Metadata.FindProperty(propertyName) is not null &&
            entry.Property(propertyName).CurrentValue is DateTimeOffset current &&
            current == default)
        {
            entry.Property(propertyName).CurrentValue = value;
        }
    }

    private static void SetLongIfDefault(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName, long value)
    {
        if (entry.Metadata.FindProperty(propertyName) is not null &&
            entry.Property(propertyName).CurrentValue is long current &&
            current == 0)
        {
            entry.Property(propertyName).CurrentValue = value;
        }
    }

    private static void IncrementLongIfPropertyExists(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName)
    {
        if (entry.Metadata.FindProperty(propertyName) is not null &&
            entry.Property(propertyName).CurrentValue is long current)
        {
            entry.Property(propertyName).CurrentValue = checked(current + 1);
        }
    }

    private static void SetUserIfPropertyExists(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName, Guid? userId)
    {
        if (userId.HasValue && entry.Metadata.FindProperty(propertyName) is not null)
        {
            entry.Property(propertyName).CurrentValue = userId.Value;
        }
    }
}
