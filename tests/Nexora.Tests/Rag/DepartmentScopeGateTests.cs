using System.Text.Json;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;
using Nexora.Application.Features.V1.Rag.Retrieval.Query;

namespace Nexora.Tests.Rag;

public sealed class DepartmentScopeGateTests
{
    [Fact]
    public void IsDenied_WhenTopMatchIsRelevantAndInaccessibleDepartment()
    {
        var documentId = Guid.NewGuid();

        var denied = DepartmentScopeGate.IsDenied(
            new VectorSearchResult(documentId, Guid.NewGuid(), 0.70f),
            0.70f,
            new HashSet<Guid>(),
            new HashSet<Guid> { documentId });

        Assert.True(denied);
    }

    [Fact]
    public void IsNotDenied_WhenTopMatchIsBelowThreshold()
    {
        var documentId = Guid.NewGuid();

        var denied = DepartmentScopeGate.IsDenied(
            new VectorSearchResult(documentId, Guid.NewGuid(), 0.69f),
            0.70f,
            new HashSet<Guid>(),
            new HashSet<Guid> { documentId });

        Assert.False(denied);
    }

    [Fact]
    public void IsNotDenied_WhenTopMatchIsAccessibleOrNotDepartmentScoped()
    {
        var documentId = Guid.NewGuid();
        var match = new VectorSearchResult(documentId, Guid.NewGuid(), 0.95f);

        Assert.False(DepartmentScopeGate.IsDenied(match, 0.70f,
            new HashSet<Guid> { documentId }, new HashSet<Guid> { documentId }));
        Assert.False(DepartmentScopeGate.IsDenied(match, 0.70f,
            new HashSet<Guid>(), new HashSet<Guid>()));
    }

    [Fact]
    public void DepartmentAccessDenied_IsNotIncludedInRetrievalApiJson()
    {
        var response = new RetrievalResponse([], string.Empty, RagContextComposer.PromptVersion)
        {
            DepartmentAccessDenied = true
        };

        var json = JsonSerializer.Serialize(response);

        Assert.DoesNotContain("DepartmentAccessDenied", json);
        Assert.DoesNotContain("No access", json);
        Assert.Empty(response.Results);
        Assert.Empty(response.AugmentedContext);
    }
}
