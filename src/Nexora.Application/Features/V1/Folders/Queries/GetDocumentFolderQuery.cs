using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Features.V1.Folders.Mappings;
using Nexora.Application.Features.V1.Folders.Model;

namespace Nexora.Application.Features.V1.Folders.Queries;

public sealed record GetDocumentFolderQuery(Guid FolderId) : IQuery<FolderDto?>;

public sealed class GetDocumentFolderQueryHandler(
    IUnitOfWork unitOfWork,
    IDefaultOrganizationProvider organizationProvider) : IRequestHandler<GetDocumentFolderQuery, FolderDto?>
{
    public async Task<FolderDto?> Handle(GetDocumentFolderQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return null;
        }

        var folder = await unitOfWork.DocumentFolderRepository.GetByIdAsync(
            request.FolderId,
            asTracking: false,
            cancellationToken);
        return folder is not null && folder.OrganizationId == organizationId.Value ? folder.ToDto() : null;
    }
}
