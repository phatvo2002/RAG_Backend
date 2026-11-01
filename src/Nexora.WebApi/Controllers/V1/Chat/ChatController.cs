using System.Text.Json;
using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Features.V1.Chat.Command;
using Nexora.Application.Features.V1.Chat;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;
using Nexora.Application.Features.V1.Chat.Query;
using Nexora.Application.Features.V1.Rag.Retrieval.Query;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Controllers.V1.Chat;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(ISender sender, ILogger<ChatController> logger) : BaseController
{
    private static readonly ChatIntentRouter IntentRouter = new();
    [HttpGet("models")]
    public async Task<IActionResult> Models(CancellationToken cancellationToken)
    {
        var configuration = await sender.Send(new GetChatConfigurationQuery(), cancellationToken);
        return Ok(new
        {
            defaultModel = configuration.Models.FirstOrDefault()?.Id ?? "",
            models = configuration.Models,
            configurationError = configuration.ConfigurationError,
            contextWindowTokens = configuration.ContextWindowTokens,
            maxOutputTokens = configuration.MaxOutputTokens,
            estimatedRetrievedContextTokens = configuration.EstimatedRetrievedContextTokens,
            estimatedSystemPromptTokens = configuration.EstimatedSystemPromptTokens,
            maxQuestionCharacters = configuration.MaxQuestionCharacters
        });
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations([FromQuery] int skip = 0, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new ListChatConversationsQuery(skip), cancellationToken));

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> Messages(Guid conversationId, [FromQuery] int? beforeSequenceNo, CancellationToken cancellationToken)
    {
        try { return Ok(await sender.Send(new ListChatMessagesQuery(conversationId, beforeSequenceNo), cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("conversations/{conversationId:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid conversationId, CancellationToken cancellationToken)
    {
        try { return await sender.Send(new DeleteChatConversationCommand(conversationId), cancellationToken) ? NoContent() : NotFound(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("conversations/{conversationId:guid}/messages/{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        try { return await sender.Send(new DeleteChatMessageCommand(conversationId, messageId), cancellationToken) ? NoContent() : NotFound(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("turns/stream")]
    public async Task Stream([FromBody] ChatTurnRequest request, CancellationToken cancellationToken)
    {
        var configuration = await sender.Send(new GetChatConfigurationQuery(), cancellationToken);
        if (!configuration.ChatEncryptionConfigured)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }
        if (string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 2000 || request.ClientRequestId == Guid.Empty)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        var chosen = configuration.Models.FirstOrDefault();
        if (chosen is null)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }
        ChatTurnStart start;
        try
        {
            start = await sender.Send(new StartChatTurnCommand(request.ConversationId, request.Question, chosen.Id, request.ClientRequestId), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }
        catch (KeyNotFoundException)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
        await EmitAsync("started", new { start.ConversationId, start.RequestMessageId, start.TurnId, start.Model }, cancellationToken);
        try
        {
            var history = await sender.Send(new GetChatPromptHistoryQuery(start.ConversationId, start.RequestMessageId), cancellationToken);
            var isGreeting = IntentRouter.IsStandaloneSmallTalk(request.Question);
            var previousUserQuestion = history.LastOrDefault(message => message.Role == "user")?.Content;
            var retrievalQuestion = IntentRouter.RetrievalQuestion(request.Question, previousUserQuestion);
            var retrieval = isGreeting
                ? null
                : await sender.Send(new RetrieveDocumentsQuery(retrievalQuestion), cancellationToken);
            var assessmentTimer = Stopwatch.StartNew();
            var decision = retrieval is null || retrieval.DepartmentAccessDenied
                ? null
                : await sender.Send(new AssessChatEvidenceQuery(request.Question, retrieval,
                    retrievalQuestion == request.Question ? null : previousUserQuestion), cancellationToken);
            assessmentTimer.Stop();
            var sources = decision?.Sources ?? Array.Empty<RetrievedChunkResponse>();
            await EmitAsync("sources", sources, cancellationToken);
            logger.LogInformation("Chat turn {TurnId} evidence mode {Mode}; top retrieval score {TopScore}; candidates {CandidateCount}; accepted sources {SourceCount}; assessment {AssessmentMs} ms.",
                start.TurnId, decision?.Mode.ToString() ?? (isGreeting ? "Greeting" : "Denied"),
                retrieval?.TopScore, retrieval?.Results.Count ?? 0, sources.Count, assessmentTimer.ElapsedMilliseconds);
            string answer;
            var completedTurn = start;
            if (retrieval?.DepartmentAccessDenied == true)
            {
                answer = "You do not have access to the relevant department documents.";
                await EmitAsync("delta", new { text = answer }, cancellationToken);
            }
            else if (decision?.Mode is ChatEvidenceMode.Missing or ChatEvidenceMode.Clarify)
            {
                answer = ChatEvidenceResponses.ForQuestion(request.Question, decision.Mode);
                await EmitAsync("delta", new { text = answer }, cancellationToken);
            }
            else
            {
                var candidates = configuration.Models
                    .Where(model => string.Equals(model.Provider, chosen.Provider, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var result = await ChatModelFallbackRunner.RunAsync(
                    candidates,
                    (model, token) => sender.Send(new StreamChatGenerationQuery(model, history,
                        decision is null || decision.Mode == ChatEvidenceMode.General
                            ? RagContextComposer.ComposeGeneralQuestion(request.Question)
                            : decision.Context,
                        start.SystemPromptInstructions), token),
                    (delta, token) => EmitAsync("delta", new { text = delta }, token),
                    (model, token) => EmitAsync("reset", new { modelName = model.Name }, token),
                    (failedModel, nextModel, exception) => logger.LogWarning(exception,
                        "Chat model {FailedModel} failed; retrying with {NextModel} from the same provider.", failedModel.Model, nextModel.Model),
                    cancellationToken);
                answer = result.Answer;
                completedTurn = start with { Model = result.Model };
            }

            var message = await sender.Send(new CompleteChatTurnCommand(completedTurn, answer, sources, sources.Count == 0), cancellationToken);
            await EmitAsync("completed", message, cancellationToken);
        }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    await sender.Send(new FailChatTurnCommand(start, "CANCELLED"), CancellationToken.None);
                }
        catch (Exception exception)
        {
            logger.LogError(exception, "Chat turn {TurnId} failed.", start.TurnId);
            await sender.Send(new FailChatTurnCommand(start, "GENERATION_FAILED"), CancellationToken.None);
            if (!cancellationToken.IsCancellationRequested)
            {
                var message = exception is ChatContextWindowExceededException contextException
                    ? $"Tin nhắn này vượt context window ước tính ({contextException.EstimatedTokens:N0}/{contextException.MaximumPromptTokens:N0} token khả dụng). Hãy rút ngắn câu hỏi hoặc bắt đầu hội thoại mới."
                    : exception is HttpRequestException
                    ? "Không thể kết nối Ollama hoặc model chưa được tải. Vui lòng liên hệ quản trị viên."
                    : "Không thể tạo câu trả lời lúc này. Vui lòng thử lại.";
                await EmitAsync("error", new { message }, cancellationToken);
            }
        }
    }

    private async Task EmitAsync(string name, object data, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {name}\ndata: {JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web))}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}

public sealed record ChatTurnRequest(Guid? ConversationId, string Question, Guid ClientRequestId, string? ModelId = null);
