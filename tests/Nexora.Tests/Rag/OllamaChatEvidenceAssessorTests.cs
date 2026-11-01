using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Query;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;
using Nexora.Infrastructure.Services.Rag;

namespace Nexora.Tests.Rag;

public sealed class OllamaChatEvidenceAssessorTests
{
    [Fact]
    public async Task Quantity_about_a_different_subject_is_not_cited()
    {
        var id = Guid.NewGuid();
        var text = "Có 357 giảng viên và nhân sự hành chính.";
        var decision = await DecideAsync("Số lượng sinh viên hiện tại là bao nhiêu?", id, text,
            new ChatEvidenceAssessment(ChatEvidenceMode.Supported,
                [new ChatEvidenceSelection(id, text, "sinh viên", "357")]));

        Assert.Equal(ChatEvidenceMode.Missing, decision.Mode);
        Assert.Empty(decision.Sources);
    }

    [Fact]
    public async Task Exact_authorized_quote_is_the_only_source_in_context()
    {
        var id = Guid.NewGuid();
        var quote = "Tổng số sinh viên là 12.000";
        var decision = await DecideAsync("Số lượng sinh viên là bao nhiêu?", id, quote,
            new ChatEvidenceAssessment(ChatEvidenceMode.Supported,
                [new ChatEvidenceSelection(id, quote, "sinh viên", "12.000")]));

        Assert.Equal(ChatEvidenceMode.Supported, decision.Mode);
        Assert.Equal(id, Assert.Single(decision.Sources).ChunkId);
        Assert.Equal(quote, decision.Sources[0].Excerpt);
        Assert.Contains(quote, decision.Context);
    }

    [Fact]
    public async Task Quantity_must_be_the_number_nearest_to_the_requested_subject()
    {
        var id = Guid.NewGuid();
        var quote = "Có 357 giảng viên và 12 sinh viên";
        var decision = await DecideAsync("Có bao nhiêu sinh viên?", id, quote,
            new ChatEvidenceAssessment(ChatEvidenceMode.Supported,
                [new ChatEvidenceSelection(id, quote, "sinh viên", "357")]));
        Assert.Equal(ChatEvidenceMode.Missing, decision.Mode);
    }

    [Fact]
    public async Task General_mode_cannot_answer_current_number_without_evidence()
    {
        var decision = await DecideAsync("How many students currently attend?", Guid.NewGuid(), "", 
            new ChatEvidenceAssessment(ChatEvidenceMode.General, []));
        Assert.Equal(ChatEvidenceMode.Missing, decision.Mode);
    }

    [Fact]
    public async Task Invented_quote_or_chunk_id_is_not_cited()
    {
        var id = Guid.NewGuid();
        var text = "This passage discusses a different subject.";
        var decision = await DecideAsync("What is the policy?", id, text,
            new ChatEvidenceAssessment(ChatEvidenceMode.Supported,
                [new ChatEvidenceSelection(Guid.NewGuid(), "Invented evidence", null, null)]));
        Assert.Equal(ChatEvidenceMode.Missing, decision.Mode);
        Assert.Empty(decision.Sources);
    }

    [Fact]
    public async Task Ollama_parses_structured_result_and_rejects_invalid_json()
    {
        var id = Guid.NewGuid();
        var valid = new FakeHandler("{\"mode\":\"SUPPORTED\",\"selections\":[{\"chunkId\":\"" + id + "\",\"quote\":\"12 students\",\"subject\":\"students\",\"value\":\"12\"}]}");
        var assessor = CreateAssessor(valid);
        var result = await assessor.AssessAsync("How many students?", [new ChatEvidenceCandidate(id, "12 students")], default);
        Assert.Equal(ChatEvidenceMode.Supported, result.Mode);
        Assert.Equal(id, Assert.Single(result.Selections).ChunkId);
        Assert.Contains("format", valid.RequestBody);

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateAssessor(new FakeHandler("not json"))
            .AssessAsync("question", [], default));
    }

    private static async Task<ChatEvidenceDecision> DecideAsync(string question, Guid id, string text,
        ChatEvidenceAssessment assessment)
    {
        var source = new RetrievedChunkResponse(Guid.NewGuid(), id, "report.txt", text, 0.9f, null, null, null);
        var retrieval = new RetrievalResponse([source], "", RagContextComposer.PromptVersion)
        {
            OrganizationName = "Nexora",
            Candidates = [new ChatEvidenceCandidate(id, text)]
        };
        return await new AssessChatEvidenceQueryHandler(new StubAssessor(assessment))
            .Handle(new AssessChatEvidenceQuery(question, retrieval), default);
    }

    private static OllamaChatEvidenceAssessor CreateAssessor(HttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("http://ollama:11434/") },
        Options.Create(new ChatOptions()), new LocalInferenceScheduler());

    private sealed class StubAssessor(ChatEvidenceAssessment result) : IChatEvidenceAssessor
    {
        public Task<ChatEvidenceAssessment> AssessAsync(string question,
            IReadOnlyList<ChatEvidenceCandidate> candidates, CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class FakeHandler(string content) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            var envelope = System.Text.Json.JsonSerializer.Serialize(new { message = new { content } });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(envelope, Encoding.UTF8, "application/json")
            };
        }
    }
}
