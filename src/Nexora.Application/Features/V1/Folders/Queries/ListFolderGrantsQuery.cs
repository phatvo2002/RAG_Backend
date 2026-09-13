using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Features.V1.Folders.Mappings;
using Nexora.Application.Features.V1.Folders.Model;

namespace Nexora.Application.Features.V1.Folders.Queries;

public sealed record ListFolderGrantsQuery(Guid FolderId) : IQuery<IReadOnlyList<FolderGrantDto>>;

public sealed class ListFolderGrantsQueryHandler(
    IUnitOfWork unitOfWork,
    IDefaultOrganizationProvider organizationProvider) : IRequestHandler<ListFolderGrantsQuery, IReadOnlyList<FolderGrantDto>>
{
    public async Task<IReadOnlyList<FolderGrantDto>> Handle(ListFolderGrantsQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Array.Empty<FolderGrantDto>();
        }

        var grants = await unitOfWork.DocumentFolderAccessGrantRepository.GetByFolderAsync(
            organizationId.Value,
            request.FolderId,
            cancellationToken);
        return grants.Select(grant => grant.ToDto()).ToArray();
    }
}
