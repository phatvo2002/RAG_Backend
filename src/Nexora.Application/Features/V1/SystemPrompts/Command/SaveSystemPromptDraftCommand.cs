using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemPrompts.Model;

namespace Nexora.Application.Features.V1.SystemPrompts.Command;

public sealed record SaveSystemPromptDraftCommand(string PromptType, string Instructions) : IRequest<Result<SystemPromptVersionResponse>>;

public sealed class SaveSystemPromptDraftCommandHandler(ISystemPromptStore store, IDefaultOrganizationProvider organizations,
    IUserContext userContext) : IRequestHandler<SaveSystemPromptDraftCommand, Result<SystemPromptVersionResponse>>
{
    public async Task<Result<SystemPromptVersionResponse>> Handle(SaveSystemPromptDraftCommand request, CancellationToken cancellationToken)
    {
        if (!SystemPromptTypes.All.Contains(request.PromptType, StringComparer.Ordinal))
            return Result<SystemPromptVersionResponse>.Failure(Error.Validation("Loại system prompt không hợp lệ."));
        if (string.IsNullOrWhiteSpace(request.Instructions) || request.Instructions.Length > 8000)
            return Result<SystemPromptVersionResponse>.Failure(Error.Validation("Prompt không được để trống và tối đa 8.000 ký tự."));
        var organizationId = await organizations.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue || !userContext.UserId.HasValue)
            return Result<SystemPromptVersionResponse>.Failure(Error.Unauthorized("Cần đăng nhập và chọn organization."));
        var version = await store.SaveDraftAsync(organizationId.Value, request.PromptType,
            request.Instructions.Trim(), userContext.UserId.Value, cancellationToken);
        return Result<SystemPromptVersionResponse>.Success(SystemPromptResponseMapper.Map(version));
    }
}
