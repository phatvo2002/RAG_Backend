namespace Nexora.Application.Common.Abstractions;

using Nexora.Application.Common.Abstractions.Repositories;

public interface IIdentityUnitOfWork
{
    IUserRepository UserRepository { get; }
    IRoleRepository RoleRepository { get; }
}

public interface IOrganizationUnitOfWork
{
    IOrganizationRepository OrganizationRepository { get; }
    IUserProfileRepository UserProfileRepository { get; }
    IOrganizationMembershipRepository OrganizationMembershipRepository { get; }
    IOrganizationRoleAssignmentRepository OrganizationRoleAssignmentRepository { get; }
    IDepartmentRepository DepartmentRepository { get; }
    IDepartmentMembershipRepository DepartmentMembershipRepository { get; }
}

public interface IAuthorizationUnitOfWork
{
    IApiFunctionRepository ApiFunctionRepository { get; }
    IRoleApiFunctionRepository RoleApiFunctionRepository { get; }
}

public interface ISecurityUnitOfWork
{
    IAccessTokenRepository AccessTokenRepository { get; }
    IActivityLogRepository ActivityLogRepository { get; }
}

public interface IUnitOfWork : IIdentityUnitOfWork, IOrganizationUnitOfWork, IAuthorizationUnitOfWork, ISecurityUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<TResponse> ExecuteInTransactionAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken cancellationToken);
}
