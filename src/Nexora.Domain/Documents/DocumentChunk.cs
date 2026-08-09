using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Documents;

public class DocumentChunk : CreatedEntityBase
{
    protected DocumentChunk()
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

}
