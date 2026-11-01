using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemPrompts.Model;

namespace Nexora.Application.Features.V1.SystemPrompts.Command;

public sealed record RestoreSystemPromptVersionCommand(string PromptType, Guid VersionId) : IRequest<Result<SystemPromptVersionResponse>>;

public sealed class RestoreSystemPromptVersionCommandHandler(ISystemPromptStore store, IDefaultOrganizationProvider organizations,
    IUserContext userContext) : IRequestHandler<RestoreSystemPromptVersionCommand, Result<SystemPromptVersionResponse>>
{
    public async Task<Result<SystemPromptVersionResponse>> Handle(RestoreSystemPromptVersionCommand request, CancellationToken cancellationToken)
    {
        if (!SystemPromptTypes.All.Contains(request.PromptType, StringComparer.Ordinal))
            return Result<SystemPromptVersionResponse>.Failure(Error.Validation("Loại system prompt không hợp lệ."));
        var organizationId = await organizations.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue || !userContext.UserId.HasValue)
            return Result<SystemPromptVersionResponse>.Failure(Error.Unauthorized("Cần đăng nhập và chọn organization."));
        var version = await store.RestoreAsync(organizationId.Value, request.PromptType, request.VersionId,
            userContext.UserId.Value, cancellationToken);
        return version is null
            ? Result<SystemPromptVersionResponse>.Failure(Error.NotFound("Không tìm thấy phiên bản đã xuất bản trong organization này."))
            : Result<SystemPromptVersionResponse>.Success(SystemPromptResponseMapper.Map(version));
    }
}
