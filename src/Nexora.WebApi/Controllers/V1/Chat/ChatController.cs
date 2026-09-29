using System.Text.Json;
using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Features.V1.Rag;
using Nexora.Infrastructure.Rag;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Controllers.V1.Chat;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(ChatService chat, ChatCipher cipher, ProviderKeyCipher providerCipher, ChatModelStreamer models, AccountSettingsService settings, ISender sender, ILogger<ChatController> logger) : BaseController
{
    [HttpGet("models")]
    public async Task<IActionResult> Models(CancellationToken cancellationToken)
    {
        var keys = await settings.ListKeysAsync(cancellationToken);
        var choices = cipher.IsConfigured && providerCipher.IsConfigured ? models.AvailableModels(keys.Keys.Any(x => x.Provider == "gemini"),
            keys.Keys.Any(x => x.Provider == "claude" && keys.ClaudeEndpoints.Any(e => e.Id == x.EndpointId))) : [];
        return Ok(new
        {
            defaultModel = choices.FirstOrDefault()?.Id ?? "",
            models = choices,
            configurationError = !cipher.IsConfigured ? "Chưa cấu hình khóa mã hóa lịch sử chat trên máy chủ." :
                !providerCipher.IsConfigured ? "Chưa cấu hình khóa mã hóa API của người dùng trên máy chủ." :
                choices.Count == 0 ? "Hãy thêm khóa Gemini hoặc Claude trong Thông tin cá nhân để bắt đầu chat." : null
        });
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations([FromQuery] int skip = 0, CancellationToken cancellationToken = default) =>
        Ok(await chat.ListConversationsAsync(skip, cancellationToken));

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> Messages(Guid conversationId, [FromQuery] int? beforeSequenceNo, CancellationToken cancellationToken)
    {
        try { return Ok(await chat.ListMessagesAsync(conversationId, beforeSequenceNo, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("conversations/{conversationId:guid}/messages/{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        try { return await chat.DeleteMessageAsync(conversationId, messageId, cancellationToken) ? NoContent() : NotFound(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("turns/stream")]
    public async Task Stream([FromBody] ChatTurnRequest request, CancellationToken cancellationToken)
    {
        if (!cipher.IsConfigured || !providerCipher.IsConfigured)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }
        if (string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 2000 || request.ClientRequestId == Guid.Empty)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        var keys = await settings.ListKeysAsync(cancellationToken);
        var available = models.AvailableModels(keys.Keys.Any(x => x.Provider == "gemini"),
            keys.Keys.Any(x => x.Provider == "claude" && keys.ClaudeEndpoints.Any(e => e.Id == x.EndpointId)));
        var chosen = available.FirstOrDefault(x => x.Id == request.ModelId);
        if (chosen is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        var credential = await settings.GetCredentialAsync(chosen.Provider, cancellationToken);
        if (credential is null) { Response.StatusCode = StatusCodes.Status400BadRequest; return; }

        ChatTurnStart start;
        try
        {
            start = await chat.StartTurnAsync(request.ConversationId, request.Question, request.ModelId, request.ClientRequestId, cancellationToken);
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
            var retrieval = await sender.Send(new RetrieveDocumentsQuery(request.Question), cancellationToken);
            await EmitAsync("sources", retrieval.Results, cancellationToken);
            string answer;
            if (retrieval.Results.Count == 0)
            {
                answer = "Không tìm thấy tài liệu được phép truy cập hỗ trợ câu hỏi này.";
                await EmitAsync("delta", new { text = answer }, cancellationToken);
            }
            else
            {
                var history = await chat.PromptHistoryAsync(start.ConversationId, start.RequestMessageId, cancellationToken);
                var output = new System.Text.StringBuilder();
                await foreach (var delta in models.StreamAsync(start.Model, history, retrieval.AugmentedContext, credential, cancellationToken))
                {
                    output.Append(delta);
                    await EmitAsync("delta", new { text = delta }, cancellationToken);
                }
                answer = output.ToString().Trim();
                if (answer.Length == 0) throw new InvalidOperationException("The generation provider returned an empty answer.");
            }

            var message = await chat.CompleteTurnAsync(start, answer, retrieval.Results, retrieval.Results.Count == 0, cancellationToken);
            await EmitAsync("completed", message, cancellationToken);
        }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    await chat.FailTurnAsync(start, "CANCELLED");
                }
        catch (Exception exception)
        {
            logger.LogError(exception, "Chat turn {TurnId} failed.", start.TurnId);
            await chat.FailTurnAsync(start, "GENERATION_FAILED");
            if (!cancellationToken.IsCancellationRequested)
            {
                var message = exception is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden }
                    ? "Khóa API của mô hình đã chọn không hợp lệ hoặc không có quyền truy cập. Hãy cập nhật khóa trong Thông tin cá nhân."
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

public sealed record ChatTurnRequest(Guid? ConversationId, string Question, string ModelId, Guid ClientRequestId);
