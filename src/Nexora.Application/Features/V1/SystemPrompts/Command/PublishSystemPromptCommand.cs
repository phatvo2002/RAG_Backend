using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemPrompts.Model;

namespace Nexora.Application.Features.V1.SystemPrompts.Command;

public sealed record PublishSystemPromptCommand(string PromptType) : IRequest<Result<SystemPromptVersionResponse>>;

public sealed class PublishSystemPromptCommandHandler(ISystemPromptStore store, IDefaultOrganizationProvider organizations,
    IUserContext userContext) : IRequestHandler<PublishSystemPromptCommand, Result<SystemPromptVersionResponse>>
{
    public async Task<Result<SystemPromptVersionResponse>> Handle(PublishSystemPromptCommand request, CancellationToken cancellationToken)
    {
        if (!SystemPromptTypes.All.Contains(request.PromptType, StringComparer.Ordinal))
            return Result<SystemPromptVersionResponse>.Failure(Error.Validation("Loại system prompt không hợp lệ."));
        var organizationId = await organizations.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue || !userContext.UserId.HasValue)
            return Result<SystemPromptVersionResponse>.Failure(Error.Unauthorized("Cần đăng nhập và chọn organization."));
        var version = await store.PublishDraftAsync(organizationId.Value, request.PromptType,
            userContext.UserId.Value, cancellationToken);
        return version is null
            ? Result<SystemPromptVersionResponse>.Failure(Error.Conflict("Loại prompt này chưa có bản nháp để xuất bản."))
            : Result<SystemPromptVersionResponse>.Success(SystemPromptResponseMapper.Map(version));
    }
}
