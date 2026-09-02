using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Behaviors;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Common.Abstractions.Repositories;

namespace Nexora.Tests.Application;

public sealed class TransactionBehaviorTests
{
    [Fact]
    public async Task SuccessfulTransactionalCommand_ShouldSaveOnce()
    {
        var unitOfWork = new TestUnitOfWork();
        var behavior = new TransactionBehavior<TransactionCommand, Result>(unitOfWork);

        var response = await behavior.Handle(new TransactionCommand(), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task FailedTransactionalCommand_ShouldNotSave()
    {
        var unitOfWork = new TestUnitOfWork();
        var behavior = new TransactionBehavior<TransactionCommand, Result>(unitOfWork);

        var response = await behavior.Handle(
            new TransactionCommand(),
            _ => Task.FromResult(Result.Failure(new Error("test.failure", "Failure"))),
            CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    private sealed record TransactionCommand : ITransactionalCommand<Result>;

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public IUserRepository UserRepository => null!;
        public IRoleRepository RoleRepository => null!;
        public IOrganizationRepository OrganizationRepository => null!;
        public IUserProfileRepository UserProfileRepository => null!;
        public IOrganizationMembershipRepository OrganizationMembershipRepository => null!;
        public IOrganizationRoleAssignmentRepository OrganizationRoleAssignmentRepository => null!;
        public IDepartmentRepository DepartmentRepository => null!;
        public IDepartmentMembershipRepository DepartmentMembershipRepository => null!;
        public IApiFunctionRepository ApiFunctionRepository => null!;
        public IRoleApiFunctionRepository RoleApiFunctionRepository => null!;
        public IAccessTokenRepository AccessTokenRepository => null!;
        public IActivityLogRepository ActivityLogRepository => null!;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }

        public Task<TResponse> ExecuteInTransactionAsync<TResponse>(Func<CancellationToken, Task<TResponse>> operation, CancellationToken cancellationToken) =>
            operation(cancellationToken);
    }
}
