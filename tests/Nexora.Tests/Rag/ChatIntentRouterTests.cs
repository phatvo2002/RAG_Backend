using Nexora.Application.Features.V1.Chat;

namespace Nexora.Tests.Rag;

public sealed class ChatIntentRouterTests
{
    private readonly ChatIntentRouter _router = new();

    [Theory]
    [InlineData("Xin chào")]
    [InlineData("Thank you!")]
    [InlineData("How are you?")]
    public void Standalone_small_talk_skips_retrieval(string question) =>
        Assert.True(_router.IsStandaloneSmallTalk(question));

    [Theory]
    [InlineData("Xin chào, báo cáo năm nay nói gì?")]
    [InlineData("Bạn biết gì về Trường Đại học Luật?")]
    [InlineData("Số lượng sinh viên hiện tại là bao nhiêu?")]
    public void Substantive_questions_require_evidence_assessment(string question) =>
        Assert.False(_router.IsStandaloneSmallTalk(question));

    [Fact]
    public void Short_follow_up_uses_previous_question_for_retrieval()
    {
        Assert.Equal("How many students attended? What about last year?",
            _router.RetrievalQuestion("What about last year?", "How many students attended?"));
        Assert.Equal("What is photosynthesis?", _router.RetrievalQuestion("What is photosynthesis?", "Old topic"));
    }
}
