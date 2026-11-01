using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemPrompts.Model;

namespace Nexora.Application.Features.V1.SystemPrompts.Command;

public sealed record PreviewSystemPromptCommand(string PromptType, string Question, string Instructions)
    : IRequest<Result<SystemPromptPreviewResponse>>;

public sealed class PreviewSystemPromptCommandHandler(
    Nexora.Application.Common.Abstractions.Chat.IChatProviderService chat, MediatR.ISender sender,
    Nexora.Application.Common.Abstractions.IDefaultOrganizationProvider organizations)
    : IRequestHandler<PreviewSystemPromptCommand, Result<SystemPromptPreviewResponse>>
{
    public async Task<Result<SystemPromptPreviewResponse>> Handle(PreviewSystemPromptCommand request, CancellationToken cancellationToken)
    {
        if (!SystemPromptTypes.All.Contains(request.PromptType, StringComparer.Ordinal))
            return Result<SystemPromptPreviewResponse>.Failure(Error.Validation("Loại system prompt không hợp lệ."));
        if (string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 4000 || request.Instructions is null || request.Instructions.Length > 8000)
            return Result<SystemPromptPreviewResponse>.Failure(Error.Validation("Nội dung thử cần có câu hỏi và prompt tối đa 8.000 ký tự."));
        var organizationId = await organizations.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
            return Result<SystemPromptPreviewResponse>.Failure(Error.Conflict("Chưa chọn organization hiện tại."));

        string answer;
        if (request.PromptType == SystemPromptTypes.MeetingTranslation)
        {
            answer = await chat.PreviewTranslationAsync(request.Question.Trim(), request.Instructions.Trim(), cancellationToken);
        }
        else
        {
            var retrieval = await sender.Send(new Nexora.Application.Features.V1.Rag.Retrieval.Query.RetrieveDocumentsQuery(request.Question.Trim()), cancellationToken);
            if (retrieval.DepartmentAccessDenied)
                return Result<SystemPromptPreviewResponse>.Failure(Error.Forbidden("Bạn không có quyền truy cập nguồn dữ liệu được yêu cầu."));
            var decision = await sender.Send(new Nexora.Application.Features.V1.Chat.Query.AssessChatEvidenceQuery(
                request.Question.Trim(), retrieval), cancellationToken);
            if (decision.Mode is ChatEvidenceMode.Missing or ChatEvidenceMode.Clarify)
                return Result<SystemPromptPreviewResponse>.Success(new SystemPromptPreviewResponse(request.PromptType,
                    Nexora.Application.Features.V1.Chat.Query.ChatEvidenceResponses.ForQuestion(request.Question, decision.Mode)));
            var context = decision.Mode == ChatEvidenceMode.General
                ? Nexora.Application.Features.V1.Rag.Retrieval.Model.RagContextComposer.ComposeGeneralQuestion(request.Question.Trim())
                : decision.Context;
            var model = chat.AvailableModels().FirstOrDefault();
            if (model is null)
                return Result<SystemPromptPreviewResponse>.Failure(Error.Conflict("Chưa cấu hình mô hình chat."));
            var estimate = chat.EstimatePromptTokens([], context, request.Instructions);
            var available = chat.ContextWindowTokens - chat.MaxOutputTokens;
            if (estimate > available)
                return Result<SystemPromptPreviewResponse>.Failure(Error.Validation($"Prompt thử vượt giới hạn context ước tính ({estimate:N0}/{available:N0} token)."));
            var output = new System.Text.StringBuilder();
            await foreach (var token in chat.StreamAsync(model, [], context, request.Instructions.Trim(), cancellationToken))
                output.Append(token);
            answer = output.ToString();
        }
        return Result<SystemPromptPreviewResponse>.Success(new SystemPromptPreviewResponse(request.PromptType, answer));
    }
}
