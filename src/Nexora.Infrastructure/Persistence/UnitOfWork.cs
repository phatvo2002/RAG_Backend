using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Results;
 
namespace Nexora.Infrastructure.Persistence;

public class UnitOfWork(
    NexoraDbContext dbContext,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IOrganizationRepository organizationRepository,
    IUserProfileRepository userProfileRepository,
    IOrganizationMembershipRepository organizationMembershipRepository,
    IOrganizationRoleAssignmentRepository organizationRoleAssignmentRepository,
    IDepartmentRepository departmentRepository,
    IDepartmentMembershipRepository departmentMembershipRepository,
    IApiFunctionRepository apiFunctionRepository,
    IRoleApiFunctionRepository roleApiFunctionRepository,
    IAccessTokenRepository accessTokenRepository,
    IActivityLogRepository activityLogRepository,
    IDocumentFolderRepository documentFolderRepository,
    IDocumentFolderAccessGrantRepository documentFolderAccessGrantRepository) : IUnitOfWork
{
    public IUserRepository UserRepository { get; } = userRepository;
    public IRoleRepository RoleRepository { get; } = roleRepository;
    public IOrganizationRepository OrganizationRepository { get; } = organizationRepository;
    public IUserProfileRepository UserProfileRepository { get; } = userProfileRepository;
    public IOrganizationMembershipRepository OrganizationMembershipRepository { get; } = organizationMembershipRepository;
    public IOrganizationRoleAssignmentRepository OrganizationRoleAssignmentRepository { get; } = organizationRoleAssignmentRepository;
    public IDepartmentRepository DepartmentRepository { get; } = departmentRepository;
    public IDepartmentMembershipRepository DepartmentMembershipRepository { get; } = departmentMembershipRepository;
    public IApiFunctionRepository ApiFunctionRepository { get; } = apiFunctionRepository;
    public IRoleApiFunctionRepository RoleApiFunctionRepository { get; } = roleApiFunctionRepository;
    public IAccessTokenRepository AccessTokenRepository { get; } = accessTokenRepository;
    public IActivityLogRepository ActivityLogRepository { get; } = activityLogRepository;
    public IDocumentFolderRepository DocumentFolderRepository { get; } = documentFolderRepository;
    public IDocumentFolderAccessGrantRepository DocumentFolderAccessGrantRepository { get; } = documentFolderAccessGrantRepository;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<TResponse> ExecuteInTransactionAsync<TResponse>(Func<CancellationToken, Task<TResponse>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await operation(cancellationToken);

            if (response is IApplicationResult { IsSuccess: false })
            {
                await transaction.RollbackAsync(cancellationToken);
                return response;
            }

            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
