using Nexora.Application.Features.V1.Rag.Retrieval.Model;

namespace Nexora.Tests.Rag;

public sealed class RagContextComposerTests
{
    [Fact]
    public void Compose_IncludesQuestionFullPassagesAndSourceLocationsInRankOrder()
    {
        var firstText = new string('A', 800);
        var passages = new[]
        {
            new RagContextPassage("first.pdf", "Human Resources", 3, 100, 899, firstText),
            new RagContextPassage("second.pdf", null, null, 900, 1100, "Second ranked passage.")
        };

        var context = RagContextComposer.Compose("Nexora", "  What does the report say?  ", passages);

        Assert.Contains(RagContextComposer.PromptVersion, context);
        Assert.Contains("Company: Nexora", context);
        Assert.Contains("Department: Human Resources", context);
        Assert.Contains("What does the report say?", context);
        Assert.Contains("Page: 3", context);
        Assert.Contains("Character range: 100–899", context);
        Assert.Contains(firstText, context);
        Assert.True(context.IndexOf("first.pdf", StringComparison.Ordinal) < context.IndexOf("second.pdf", StringComparison.Ordinal));
        Assert.Contains("Untrusted passage text:", context);
    }

    [Fact]
    public void Compose_ReturnsEmptyContextWhenNoPassagesAreRetrieved()
    {
        var context = RagContextComposer.Compose("Nexora", "Question without matches", Array.Empty<RagContextPassage>());

        Assert.Empty(context);
    }

    [Fact]
    public void ComposeWithoutSources_KeepsTheQuestionWithoutInventingEvidence()
    {
        var context = RagContextComposer.ComposeWithoutSources("  Nhiệm vụ của bạn là gì?  ");

        Assert.Contains("Nhiệm vụ của bạn là gì?", context);
        Assert.Contains("No authorized source passages were retrieved", context);
        Assert.DoesNotContain("Company:", context);
    }
}
