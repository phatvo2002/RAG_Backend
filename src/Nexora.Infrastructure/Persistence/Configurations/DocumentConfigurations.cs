using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Documents;
using Nexora.Domain.Organizations;
using Nexora.Domain.Projects;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

internal sealed class DocumentFolderConfiguration : IEntityTypeConfiguration<DocumentFolder>
{
    public void Configure(EntityTypeBuilder<DocumentFolder> builder)
    {
        builder.ToTable("DocumentFolders", SchemaNames.Document);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.DepartmentId).HasColumnName("departmentId");
        builder.Property(x => x.ParentFolderId).HasColumnName("parentFolderId");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.OwnerUserId).HasColumnName("ownerUserId").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deletedAt").HasColumnType("timestamptz");
        builder.Property(x => x.DeleteBy).HasColumnName("deleteBy");
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.ParentFolderId, x.Name }).IsUnique().HasDatabaseName("ux_folder_parent_name");
        builder.HasIndex(x => new { x.OrganizationId, x.DepartmentId });
        builder.HasIndex(x => new { x.OrganizationId, x.Id }).IsUnique().HasDatabaseName("ux_folder_tenant_id");

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentFolder>().WithMany().HasForeignKey(x => x.ParentFolderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DeleteBy).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents", SchemaNames.Document);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.DepartmentId).HasColumnName("departmentId");
        builder.Property(x => x.OwnerUserId).HasColumnName("ownerUserId").IsRequired();
        builder.Property(x => x.FolderId).HasColumnName("folderId");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.CurrentVersionId).HasColumnName("currentVersionId");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deletedAt").HasColumnType("timestamptz");
        builder.Property(x => x.DeleteBy).HasColumnName("deleteBy");
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.Status });
        builder.HasIndex(x => new { x.OrganizationId, x.OwnerUserId });
        builder.HasIndex(x => new { x.OrganizationId, x.FolderId });
        builder.HasIndex(x => new { x.OrganizationId, x.DepartmentId });
        builder.HasIndex(x => new { x.OrganizationId, x.Id }).IsUnique().HasDatabaseName("ux_document_tenant_id");

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentFolder>().WithMany().HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentVersion>().WithMany().HasForeignKey(x => x.CurrentVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DeleteBy).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("DocumentVersions", SchemaNames.Document, table =>
        {
            table.HasCheckConstraint("ck_document_version_file_sha256_length", "octet_length(\"fileSha256\") = 32");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.DocumentId).HasColumnName("documentId").IsRequired();
        builder.Property(x => x.VersionNo).HasColumnName("versionNo").IsRequired();
        builder.Property(x => x.OriginalFileName).HasColumnName("originalFileName").HasMaxLength(500).IsRequired();
        builder.Property(x => x.MimeType).HasColumnName("mimeType").HasMaxLength(150).IsRequired();
        builder.Property(x => x.SizeBytes).HasColumnName("sizeBytes").IsRequired();
        builder.Property(x => x.StorageProvider).HasColumnName("storageProvider").HasMaxLength(50).HasDefaultValue("minio").IsRequired();
        builder.Property(x => x.StorageBucket).HasColumnName("storageBucket").HasMaxLength(100).IsRequired();
        builder.Property(x => x.StorageObjectKey).HasColumnName("storageObjectKey").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.StorageVersionId).HasColumnName("storageVersionId").HasMaxLength(200);
        builder.Property(x => x.FileSha256).HasColumnName("fileSha256").HasColumnType("bytea").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ExtractedContentHash).HasColumnName("extractedContentHash").HasColumnType("char(64)").HasMaxLength(64);
        builder.Property(x => x.ParserName).HasColumnName("parserName").HasMaxLength(100);
        builder.Property(x => x.ParserVersion).HasColumnName("parserVersion").HasMaxLength(50);
        builder.Property(x => x.ProcessingStatus).HasColumnName("processingStatus").HasMaxLength(30).IsRequired();
        builder.Property(x => x.FailureCode).HasColumnName("failureCode").HasMaxLength(80);
        builder.Property(x => x.CreatedByUserId).HasColumnName("createdByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ReadyAt).HasColumnName("readyAt").HasColumnType("timestamptz");
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.DocumentId, x.VersionNo }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.DocumentId, x.Id }).IsUnique().HasDatabaseName("ux_document_version_tenant_id");
        builder.HasIndex(x => new { x.OrganizationId, x.ProcessingStatus });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Document>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DocumentAccessGrantConfiguration : IEntityTypeConfiguration<DocumentAccessGrant>
{
    public void Configure(EntityTypeBuilder<DocumentAccessGrant> builder)
    {
        builder.ToTable("DocumentAccessGrants", SchemaNames.Document, table =>
        {
            table.HasCheckConstraint("ck_document_grant_valid_range", "\"expiresAt\" IS NULL OR \"validFrom\" < \"expiresAt\"");
            table.HasCheckConstraint("ck_document_grant_revoke_actor", "(\"revokedAt\" IS NULL AND \"revokedByUserId\" IS NULL) OR (\"revokedAt\" IS NOT NULL AND \"revokedByUserId\" IS NOT NULL)");
            table.HasCheckConstraint("ck_document_grant_scope_principal", "(\"scopeType\" = 'ORGANIZATION' AND \"principalDepartmentId\" IS NULL AND \"principalProjectId\" IS NULL AND \"principalUserId\" IS NULL) OR (\"scopeType\" = 'DEPARTMENT' AND \"principalDepartmentId\" IS NOT NULL AND \"principalProjectId\" IS NULL AND \"principalUserId\" IS NULL) OR (\"scopeType\" = 'PROJECT' AND \"principalDepartmentId\" IS NULL AND \"principalProjectId\" IS NOT NULL AND \"principalUserId\" IS NULL) OR (\"scopeType\" = 'USER' AND \"principalDepartmentId\" IS NULL AND \"principalProjectId\" IS NULL AND \"principalUserId\" IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.DocumentId).HasColumnName("documentId").IsRequired();
        builder.Property(x => x.ScopeType).HasColumnName("scopeType").HasMaxLength(20).IsRequired();
        builder.Property(x => x.PrincipalDepartmentId).HasColumnName("principalDepartmentId");
        builder.Property(x => x.PrincipalProjectId).HasColumnName("principalProjectId");
        builder.Property(x => x.PrincipalUserId).HasColumnName("principalUserId");
        builder.Property(x => x.AccessLevel).HasColumnName("accessLevel").HasMaxLength(20).HasDefaultValue("VIEW").IsRequired();
        builder.Property(x => x.GrantedByUserId).HasColumnName("grantedByUserId").IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("validFrom").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expiresAt").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedAt).HasColumnName("revokedAt").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedByUserId).HasColumnName("revokedByUserId");
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.DocumentId, x.RevokedAt, x.ExpiresAt });
        builder.HasIndex(x => new { x.OrganizationId, x.PrincipalDepartmentId });
        builder.HasIndex(x => new { x.OrganizationId, x.PrincipalProjectId });
        builder.HasIndex(x => new { x.OrganizationId, x.PrincipalUserId });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Document>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.PrincipalDepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.PrincipalProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PrincipalUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.GrantedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DocumentFolderAccessGrantConfiguration : IEntityTypeConfiguration<DocumentFolderAccessGrant>
{
    public void Configure(EntityTypeBuilder<DocumentFolderAccessGrant> builder)
    {
        builder.ToTable("DocumentFolderAccessGrants", SchemaNames.Document);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.FolderId).HasColumnName("folderId").IsRequired();
        builder.Property(x => x.DepartmentId).HasColumnName("departmentId");
        builder.Property(x => x.UserId).HasColumnName("userId");
        builder.Property(x => x.AccessLevel).HasColumnName("accessLevel").HasMaxLength(20).IsRequired();
        builder.Property(x => x.GrantedByUserId).HasColumnName("grantedByUserId").IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("validFrom").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expiresAt").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedAt).HasColumnName("revokedAt").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedByUserId).HasColumnName("revokedByUserId");
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();
        builder.HasIndex(x => new { x.OrganizationId, x.FolderId });
        builder.HasIndex(x => new { x.OrganizationId, x.DepartmentId });
        builder.HasIndex(x => new { x.OrganizationId, x.UserId });
        builder.HasOne<DocumentFolder>().WithMany().HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("DocumentChunks", SchemaNames.Document);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.DocumentVersionId).HasColumnName("documentVersionId").IsRequired();
        builder.Property(x => x.Ordinal).HasColumnName("ordinal").IsRequired();
        builder.Property(x => x.ContentHash).HasColumnName("contentHash").HasColumnType("char(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.LanguageCode).HasColumnName("languageCode").HasMaxLength(10);
        builder.Property(x => x.TokenCount).HasColumnName("tokenCount");
        builder.Property(x => x.PageNo).HasColumnName("pageNo");
        builder.Property(x => x.CharStart).HasColumnName("charStart");
        builder.Property(x => x.CharEnd).HasColumnName("charEnd");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("ACTIVE").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.DocumentVersionId, x.Ordinal }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Id }).IsUnique().HasDatabaseName("ux_chunk_tenant_id");

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentVersion>().WithMany().HasForeignKey(x => x.DocumentVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DocumentChunkContentConfiguration : IEntityTypeConfiguration<DocumentChunkContent>
{
    public void Configure(EntityTypeBuilder<DocumentChunkContent> builder)
    {
        builder.ToTable("DocumentChunkContents", SchemaNames.Document);
        builder.HasKey(x => x.ChunkId);
        builder.Property(x => x.ChunkId).HasColumnName("chunkId");
        builder.Property(x => x.StorageProvider).HasColumnName("storageProvider").HasMaxLength(50).HasDefaultValue("minio").IsRequired();
        builder.Property(x => x.StorageBucket).HasColumnName("storageBucket").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ContentObjectKey).HasColumnName("contentObjectKey").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.StorageVersionId).HasColumnName("storageVersionId").HasMaxLength(200);
        builder.Property(x => x.EncryptionKeyVersion).HasColumnName("encryptionKeyVersion").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ContentSha256).HasColumnName("contentSha256").HasColumnType("char(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasOne<DocumentChunk>().WithOne().HasForeignKey<DocumentChunkContent>(x => x.ChunkId).OnDelete(DeleteBehavior.Restrict);
    }
}
