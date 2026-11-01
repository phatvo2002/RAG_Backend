namespace Nexora.Application.Features.V1.Chat;

public sealed class ChatIntentRouter
{
    private static readonly string[] StandaloneSmallTalk =
    [
        "hi", "hello", "hey", "good morning", "good afternoon", "good evening",
        "how are you", "how's it going", "what's up", "thank you", "thanks",
        "xin chào", "chào", "chào bạn", "bạn khỏe không", "bạn thế nào",
        "cảm ơn", "cám ơn", "tạm biệt"
    ];

    public bool IsStandaloneSmallTalk(string question)
    {
        var normalized = string.Join(' ', question.Trim().TrimEnd('?', '!', '.', ',', ';', ':')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return StandaloneSmallTalk.Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    public string RetrievalQuestion(string question, string? previousUserQuestion)
    {
        if (string.IsNullOrWhiteSpace(previousUserQuestion) || question.Length > 80)
            return question;

        var followUp = new[] { "còn ", "vậy ", "thế ", "tiếp theo", "năm trước", "năm sau",
            "what about", "and ", "how about", "last year", "next year" };
        return followUp.Any(prefix => question.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            ? $"{previousUserQuestion} {question}"
            : question;
    }
}
