using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Features.V1.Folders.Mappings;
using Nexora.Application.Features.V1.Folders.Model;

namespace Nexora.Application.Features.V1.Folders.Queries;

public sealed record ListDocumentFoldersQuery(Guid? DepartmentId) : IQuery<IReadOnlyList<FolderDto>>;

public sealed class ListDocumentFoldersQueryHandler(
    IUnitOfWork unitOfWork,
    IDefaultOrganizationProvider organizationProvider) : IRequestHandler<ListDocumentFoldersQuery, IReadOnlyList<FolderDto>>
{
    public async Task<IReadOnlyList<FolderDto>> Handle(ListDocumentFoldersQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Array.Empty<FolderDto>();
        }

        var folders = await unitOfWork.DocumentFolderRepository.GetForOrganizationAsync(
            organizationId.Value,
            request.DepartmentId,
            cancellationToken);
        return folders.Select(folder => folder.ToDto()).ToArray();
    }
}
