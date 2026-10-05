using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Application.Features.V1.Rag.Retrieval.Model;

public static class DepartmentScopeGate
{
    public static bool IsDenied(
        VectorSearchResult? topCompanyMatch,
        float minimumScore,
        IReadOnlySet<Guid> accessibleDocumentIds,
        IReadOnlySet<Guid> departmentDocumentIds) =>
        topCompanyMatch is { Score: var score } match &&
        float.IsFinite(score) &&
        score >= minimumScore &&
        departmentDocumentIds.Contains(match.DocumentId) &&
        !accessibleDocumentIds.Contains(match.DocumentId);
}
