using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemPrompts.Model;

namespace Nexora.Application.Features.V1.SystemPrompts.Query;

public sealed record GetSystemPromptVersionsQuery(string PromptType) : IRequest<Result<IReadOnlyList<SystemPromptVersionResponse>>>;

public sealed class GetSystemPromptVersionsQueryHandler(ISystemPromptStore store, IDefaultOrganizationProvider organizations)
    : IRequestHandler<GetSystemPromptVersionsQuery, Result<IReadOnlyList<SystemPromptVersionResponse>>>
{
    public async Task<Result<IReadOnlyList<SystemPromptVersionResponse>>> Handle(GetSystemPromptVersionsQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizations.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
            return Result<IReadOnlyList<SystemPromptVersionResponse>>.Failure(Error.Conflict("Chưa chọn organization hiện tại."));
        if (!SystemPromptTypes.All.Contains(request.PromptType, StringComparer.Ordinal))
            return Result<IReadOnlyList<SystemPromptVersionResponse>>.Failure(Error.Validation("Loại system prompt không hợp lệ."));
        var versions = await store.GetVersionsAsync(organizationId.Value, request.PromptType, cancellationToken);
        return Result<IReadOnlyList<SystemPromptVersionResponse>>.Success(versions.Select(SystemPromptResponseMapper.Map).ToArray());
    }
}
