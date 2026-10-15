using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Chat;
using Nexora.Domain.Documents;
using Nexora.Domain.Organizations;
using Nexora.Domain.Projects;
using Nexora.Infrastructure.Services.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations", SchemaNames.Chat);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.OwnerUserId).HasColumnName("ownerUserId").IsRequired();
        builder.Property(x => x.TitleCiphertext).HasColumnName("titleCiphertext").HasColumnType("bytea");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deletedAt").HasColumnType("timestamptz");
        builder.Property(x => x.DeleteBy).HasColumnName("deleteBy");
        builder.Property(x => x.RowVersion).HasColumnName("rowVersion").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(x => new { x.OrganizationId, x.Id }).IsUnique().HasDatabaseName("ux_conversation_tenant_id");
        builder.HasIndex(x => new { x.OrganizationId, x.OwnerUserId, x.UpdatedAt });
        builder.HasIndex(x => new { x.OrganizationId, x.Status });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DeleteBy).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ConversationContextConfiguration : IEntityTypeConfiguration<ConversationContext>
{
    public void Configure(EntityTypeBuilder<ConversationContext> builder)
    {
        builder.ToTable("ConversationContexts", SchemaNames.Chat);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.ConversationId).HasColumnName("conversationId").IsRequired();
        builder.Property(x => x.ScopeType).HasColumnName("scopeType").HasMaxLength(20).IsRequired();
        builder.Property(x => x.DepartmentId).HasColumnName("departmentId");
        builder.Property(x => x.ProjectId).HasColumnName("projectId");
        builder.Property(x => x.DocumentId).HasColumnName("documentId");
        builder.Property(x => x.SelectedByUserId).HasColumnName("selectedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Document>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SelectedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages", SchemaNames.Chat);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.ConversationId).HasColumnName("conversationId").IsRequired();
        builder.Property(x => x.SenderType).HasColumnName("senderType").HasMaxLength(20).IsRequired();
        builder.Property(x => x.SenderUserId).HasColumnName("senderUserId");
        builder.Property(x => x.ContentCiphertext).HasColumnName("contentCiphertext").HasColumnType("bytea").IsRequired();
        builder.Property(x => x.ContentHash).HasColumnName("contentHash").HasColumnType("char(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.LanguageCode).HasColumnName("languageCode").HasMaxLength(10);
        builder.Property(x => x.SequenceNo).HasColumnName("sequenceNo").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deletedAt").HasColumnType("timestamptz");
        builder.Property(x => x.DeletedByUserId).HasColumnName("deletedByUserId");

        builder.HasIndex(x => new { x.OrganizationId, x.Id }).IsUnique().HasDatabaseName("ux_message_tenant_id");
        builder.HasIndex(x => new { x.OrganizationId, x.ConversationId, x.SequenceNo }).IsUnique();

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DeletedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AiTurnConfiguration : IEntityTypeConfiguration<AiTurn>
{
    public void Configure(EntityTypeBuilder<AiTurn> builder)
    {
        builder.ToTable("AiTurns", SchemaNames.Chat);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.ConversationId).HasColumnName("conversationId").IsRequired();
        builder.Property(x => x.RequestMessageId).HasColumnName("requestMessageId").IsRequired();
        builder.Property(x => x.ResponseMessageId).HasColumnName("responseMessageId");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ModelProvider).HasColumnName("modelProvider").HasMaxLength(100);
        builder.Property(x => x.ModelName).HasColumnName("modelName").HasMaxLength(200);
        builder.Property(x => x.PromptVersion).HasColumnName("promptVersion").HasMaxLength(100);
        builder.Property(x => x.InputTokens).HasColumnName("inputTokens");
        builder.Property(x => x.OutputTokens).HasColumnName("outputTokens");
        builder.Property(x => x.LatencyMs).HasColumnName("latencyMs");
        builder.Property(x => x.NoSourceReason).HasColumnName("noSourceReason").HasMaxLength(500);
        builder.Property(x => x.CorrelationId).HasColumnName("correlationId").HasMaxLength(100).IsRequired();
        builder.Property(x => x.StartedAt).HasColumnName("startedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnName("completedAt").HasColumnType("timestamptz");
        builder.Property(x => x.ErrorCode).HasColumnName("errorCode").HasMaxLength(80);

        builder.HasIndex(x => new { x.OrganizationId, x.Id }).IsUnique().HasDatabaseName("ux_ai_turn_tenant_id");
        builder.HasIndex(x => new { x.OrganizationId, x.ConversationId, x.StartedAt });
        builder.HasIndex(x => new { x.OrganizationId, x.Status });
        builder.HasIndex(x => new { x.OrganizationId, x.CorrelationId }).IsUnique();

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Message>().WithMany().HasForeignKey(x => x.RequestMessageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Message>().WithMany().HasForeignKey(x => x.ResponseMessageId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CitationConfiguration : IEntityTypeConfiguration<Citation>
{
    public void Configure(EntityTypeBuilder<Citation> builder)
    {
        builder.ToTable("Citations", SchemaNames.Chat);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organizationId").IsRequired();
        builder.Property(x => x.AssistantMessageId).HasColumnName("assistantMessageId").IsRequired();
        builder.Property(x => x.ChunkId).HasColumnName("chunkId").IsRequired();
        builder.Property(x => x.CitationNo).HasColumnName("citationNo").IsRequired();
        builder.Property(x => x.PageNo).HasColumnName("pageNo");
        builder.Property(x => x.CharStart).HasColumnName("charStart");
        builder.Property(x => x.CharEnd).HasColumnName("charEnd");
        builder.Property(x => x.Score).HasColumnName("score");
        builder.Property(x => x.QuoteHash).HasColumnName("quoteHash").HasColumnType("char(64)").HasMaxLength(64);
        builder.Property(x => x.CreatedAt).HasColumnName("createdAt").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.AssistantMessageId, x.CitationNo }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.ChunkId });

        builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Message>().WithMany().HasForeignKey(x => x.AssistantMessageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentChunk>().WithMany().HasForeignKey(x => x.ChunkId).OnDelete(DeleteBehavior.Restrict);
    }
}
