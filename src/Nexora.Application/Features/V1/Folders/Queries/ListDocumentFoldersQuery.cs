using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Features.V1.Folders.Mappings;
using Nexora.Application.Features.V1.Folders.Model;

namespace Nexora.Application.Features.V1.Folders.Queries;

public sealed record ListDocumentFoldersQuery(Guid? DepartmentId) : IQuery<IReadOnlyList<FolderOverviewDto>>;

public sealed class ListDocumentFoldersQueryHandler(
    IUnitOfWork unitOfWork,
    IFolderOverviewRepository folderOverviewRepository,
    IDefaultOrganizationProvider organizationProvider) : IRequestHandler<ListDocumentFoldersQuery, IReadOnlyList<FolderOverviewDto>>
{
    public async Task<IReadOnlyList<FolderOverviewDto>> Handle(ListDocumentFoldersQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Array.Empty<FolderOverviewDto>();
        }

        var folders = await unitOfWork.DocumentFolderRepository.GetForOrganizationAsync(
            organizationId.Value,
            request.DepartmentId,
            cancellationToken);
        var summaries = await folderOverviewRepository.GetFolderSummariesAsync(
            organizationId.Value,
            folders.Select(folder => folder.Id).ToArray(),
            cancellationToken);
        var summariesByFolderId = summaries.ToDictionary(summary => summary.FolderId);

        return folders
            .Select(folder => folder.ToOverviewDto(summariesByFolderId.GetValueOrDefault(folder.Id)))
            .ToArray();
    }
}
