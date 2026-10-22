using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Documents;
using Nexora.Domain.Organizations;
using Nexora.Domain.Rag;

namespace Nexora.Infrastructure.Persistence.Configurations;

internal sealed class RagIndexConfiguration : IEntityTypeConfiguration<RagIndex>
{
    public void Configure(EntityTypeBuilder<RagIndex> builder)
    {
        builder.ToTable("RagIndexes", SchemaNames.Rag);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.IndexName).HasColumnName("indexName").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(80).HasDefaultValue("qdrant").IsRequired();
        builder.Property(x => x.CollectionName).HasColumnName("collectionName").HasMaxLength(200).IsRequired();
        builder.Property(x => x.EmbeddingModel).HasColumnName("embeddingModel").HasMaxLength(200).IsRequired();
        builder.Property(x => x.EmbeddingDimension).HasColumnName("embeddingDimension").IsRequired();
        builder.Property(x => x.DistanceMetric).HasColumnName("distanceMetric").HasMaxLength(20).IsRequired();
        builder.Property(x => x.ConfigurationHash).HasColumnName("configurationHash").HasColumnType("char(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.IndexName }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Id }).IsUnique().HasDatabaseName("ux_rag_index_tenant_id");
        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RagIndexRecordConfiguration : IEntityTypeConfiguration<RagIndexRecord>
{
    public void Configure(EntityTypeBuilder<RagIndexRecord> builder)
    {
        builder.ToTable("RagIndexRecords", SchemaNames.Rag);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.RagIndexId).HasColumnName("ragIndexId").IsRequired();
        builder.Property(x => x.ChunkId).HasColumnName("chunkId").IsRequired();
        builder.Property(x => x.VectorPointId).HasColumnName("vectorPointId").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContentHash).HasColumnName("contentHash").HasColumnType("char(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.IndexedAt).HasColumnName("indexedAt").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.RagIndexId, x.ChunkId }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.RagIndexId, x.VectorPointId }).IsUnique();

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RagIndex>().WithMany().HasForeignKey(x => x.RagIndexId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentChunk>().WithMany().HasForeignKey(x => x.ChunkId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RetrievalRunConfiguration : IEntityTypeConfiguration<RetrievalRun>
{
    public void Configure(EntityTypeBuilder<RetrievalRun> builder)
    {
        builder.ToTable("RetrievalRuns", SchemaNames.Rag);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.AiTurnId).HasColumnName("aiTurnId").IsRequired();
        builder.Property(x => x.RagIndexId).HasColumnName("ragIndexId").IsRequired();
        builder.Property(x => x.Strategy).HasColumnName("strategy").HasMaxLength(30).IsRequired();
        builder.Property(x => x.TopK).HasColumnName("topK").IsRequired();
        builder.Property(x => x.FilterHash).HasColumnName("filterHash").HasColumnType("char(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ConfigurationHash).HasColumnName("configurationHash").HasColumnType("char(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.LatencyMs).HasColumnName("latencyMs");
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.Id }).IsUnique().HasDatabaseName("ux_retrieval_run_tenant_id");
        builder.HasIndex(x => new { x.OrganizationId, x.AiTurnId, x.CreatedAt });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Nexora.Domain.Chat.AiTurn>().WithMany().HasForeignKey(x => x.AiTurnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RagIndex>().WithMany().HasForeignKey(x => x.RagIndexId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RetrievalHitConfiguration : IEntityTypeConfiguration<RetrievalHit>
{
    public void Configure(EntityTypeBuilder<RetrievalHit> builder)
    {
        builder.ToTable("RetrievalHits", SchemaNames.Rag);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.RetrievalRunId).HasColumnName("retrievalRunId").IsRequired();
        builder.Property(x => x.ChunkId).HasColumnName("chunkId");
        builder.Property(x => x.RankNo).HasColumnName("rankNo").IsRequired();
        builder.Property(x => x.SimilarityScore).HasColumnName("similarityScore").HasColumnType("numeric(12,8)");
        builder.Property(x => x.RerankScore).HasColumnName("rerankScore").HasColumnType("numeric(12,8)");
        builder.Property(x => x.SelectedForContext).HasColumnName("selectedForContext").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.RetrievalRunId, x.RankNo }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.ChunkId });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RetrievalRun>().WithMany().HasForeignKey(x => x.RetrievalRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentChunk>().WithMany().HasForeignKey(x => x.ChunkId).OnDelete(DeleteBehavior.SetNull);
    }
}
