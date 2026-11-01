using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemPrompts.Model;

namespace Nexora.Application.Features.V1.SystemPrompts.Query;

public sealed record GetSystemPromptsQuery : IRequest<Result<IReadOnlyList<SystemPromptResponse>>>;

public sealed class GetSystemPromptsQueryHandler(ISystemPromptStore store, IDefaultOrganizationProvider organizations)
    : IRequestHandler<GetSystemPromptsQuery, Result<IReadOnlyList<SystemPromptResponse>>>
{
    public async Task<Result<IReadOnlyList<SystemPromptResponse>>> Handle(GetSystemPromptsQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizations.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
            return Result<IReadOnlyList<SystemPromptResponse>>.Failure(Error.Conflict("Chưa chọn organization hiện tại."));
        var prompts = await store.GetAllAsync(organizationId.Value, cancellationToken);
        return Result<IReadOnlyList<SystemPromptResponse>>.Success(prompts.Select(SystemPromptResponseMapper.Map).ToArray());
    }
}
