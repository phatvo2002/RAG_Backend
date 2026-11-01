using System.Text.RegularExpressions;
using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record AssessChatEvidenceQuery(string Question, RetrievalResponse Retrieval, string? PreviousUserQuestion = null)
    : IRequest<ChatEvidenceDecision>;

public sealed record ChatEvidenceDecision(ChatEvidenceMode Mode, IReadOnlyList<RetrievedChunkResponse> Sources,
    string Context);

public static class ChatEvidenceResponses
{
    public static string ForQuestion(string question, ChatEvidenceMode mode)
    {
        const string vietnamese = "ăâđêôơưáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ";
        var isVietnamese = question.Any(character => vietnamese.Contains(char.ToLowerInvariant(character)));
        return (mode, isVietnamese) switch
        {
            (ChatEvidenceMode.Clarify, true) => "Bạn có thể nói rõ hơn bạn muốn hỏi về thông tin nào không?",
            (ChatEvidenceMode.Clarify, false) => "Could you clarify which information you need?",
            (_, true) => "Tôi chưa tìm thấy căn cứ đủ rõ trong tài liệu được phép truy cập để trả lời chính xác câu hỏi này.",
            _ => "I couldn't find enough evidence in documents you can access to answer this accurately."
        };
    }
}

public sealed class AssessChatEvidenceQueryHandler(IChatEvidenceAssessor assessor)
    : IRequestHandler<AssessChatEvidenceQuery, ChatEvidenceDecision>
{
    public async Task<ChatEvidenceDecision> Handle(AssessChatEvidenceQuery request, CancellationToken cancellationToken)
    {
        var effectiveQuestion = string.IsNullOrWhiteSpace(request.PreviousUserQuestion)
            ? request.Question
            : $"Previous user question: {request.PreviousUserQuestion}\nCurrent question: {request.Question}";
        var assessment = await assessor.AssessAsync(effectiveQuestion, request.Retrieval.Candidates, cancellationToken);
        if (assessment.Mode == ChatEvidenceMode.General && Regex.IsMatch(effectiveQuestion,
                @"\b(?:current|currently|latest|how many|number of|our company|our organization|internal|according to|uploaded document)\b|(?:hiện tại|mới nhất|bao nhiêu|số lượng|tổng số|tài liệu|báo cáo|nội bộ|công ty chúng ta)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return new ChatEvidenceDecision(ChatEvidenceMode.Missing, [], string.Empty);
        if (assessment.Mode != ChatEvidenceMode.Supported)
            return new ChatEvidenceDecision(assessment.Mode, [], string.Empty);

        var candidates = request.Retrieval.Candidates.ToDictionary(candidate => candidate.ChunkId);
        var sources = request.Retrieval.Results.ToDictionary(source => source.ChunkId);
        var accepted = new List<RetrievedChunkResponse>();
        var passages = new List<RagContextPassage>();
        var asksForNumber = Regex.IsMatch(effectiveQuestion,
            @"\b(?:how many|number of|count of|current total)\b|(?:bao nhiêu|số lượng|tổng số)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (var selection in assessment.Selections.Take(3))
        {
            if (!candidates.TryGetValue(selection.ChunkId, out var candidate) ||
                !sources.TryGetValue(selection.ChunkId, out var source) ||
                string.IsNullOrWhiteSpace(selection.Quote) ||
                !candidate.Text.Contains(selection.Quote, StringComparison.Ordinal))
                continue;

            if (asksForNumber && !HasMatchingQuantity(selection, effectiveQuestion))
                continue;

            if (accepted.Any(item => item.ChunkId == source.ChunkId)) continue;
            accepted.Add(source with { Excerpt = selection.Quote[..Math.Min(selection.Quote.Length, 600)] });
            passages.Add(new RagContextPassage(source.FileName, null, source.PageNo, source.CharStart,
                source.CharEnd, selection.Quote));
        }

        if (accepted.Count == 0)
            return new ChatEvidenceDecision(ChatEvidenceMode.Missing, [], string.Empty);

        return new ChatEvidenceDecision(ChatEvidenceMode.Supported, accepted,
            RagContextComposer.Compose(request.Retrieval.OrganizationName, request.Question, passages));
    }

    private static bool HasMatchingQuantity(ChatEvidenceSelection selection, string question)
    {
        if (string.IsNullOrWhiteSpace(selection.Subject) || string.IsNullOrWhiteSpace(selection.Value) ||
            !selection.Value.Any(char.IsDigit) ||
            !question.Contains(selection.Subject, StringComparison.OrdinalIgnoreCase)) return false;

        return Regex.Split(selection.Quote, @"(?<!\d)\.(?!\d)|;|\r?\n")
            .Any(clause =>
            {
                var subjectIndex = clause.IndexOf(selection.Subject, StringComparison.OrdinalIgnoreCase);
                if (subjectIndex < 0) return false;
                var nearestNumber = Regex.Matches(clause, @"\d[\d.,]*")
                    .OrderBy(match => Math.Abs(match.Index - subjectIndex))
                    .FirstOrDefault();
                return nearestNumber?.Value == selection.Value;
            });
    }
}
