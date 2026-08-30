using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common;

namespace Nexora.Domain.Documents;

public class DocumentChunk : CreatedEntityBase
{
    protected DocumentChunk()
    {
    }

    private DocumentChunk(Guid id, DateTimeOffset now)
        : base(id, now)
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid DocumentVersionId { get; private set; }

    public int Ordinal { get; set; }

    public string ContentHash { get; private set; } = string.Empty;

    public string? LanguageCode { get; set; }

    public int? TokenCount { get; set; }

    public int? PageNo { get; set; }

    public int? CharStart { get; set; }

    public int? CharEnd { get; set; }

    public string Status { get; set; } = string.Empty;

    public static DocumentChunk Create(Guid organizationId, Guid documentVersionId, int ordinal, string contentHash, DateTimeOffset now, string? languageCode = null, int? tokenCount = null, int? pageNo = null, int? charStart = null, int? charEnd = null)
    {
        return new DocumentChunk(Guid.NewGuid(), now)
        {
            OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId)),
            DocumentVersionId = DomainGuard.RequiredId(documentVersionId, nameof(documentVersionId)),
            Ordinal = ordinal,
            ContentHash = DomainGuard.RequiredText(contentHash, nameof(contentHash), 64),
            LanguageCode = languageCode,
            TokenCount = tokenCount,
            PageNo = pageNo,
            CharStart = charStart,
            CharEnd = charEnd,
            Status = "ACTIVE"
        };
    }

    public void UpdateContent(string contentHash, int? tokenCount, int? pageNo, int? charStart, int? charEnd)
    {
        ContentHash = DomainGuard.RequiredText(contentHash, nameof(contentHash), 64);
        TokenCount = tokenCount;
        PageNo = pageNo;
        CharStart = charStart;
        CharEnd = charEnd;
        Status = "ACTIVE";
    }

}
