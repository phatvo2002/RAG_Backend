using System.Reflection;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Features.V1.SystemManagements.User.Command;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;
using Nexora.Domain.Organizations.Enums;
using Nexora.Infrastructure.Identity;

namespace Nexora.Tests.Application;

public sealed class UpdateUserOrganizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task WithoutOrganizationId_LeavesMembershipUnchanged()
    {
        var fixture = new Fixture();

        var result = await fixture.UpdateAsync(null);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, fixture.MembershipWriteCount);
        Assert.Null(result.Value?.OrganizationId);
    }

    [Fact]
    public async Task SelectingCurrentOrganization_CreatesActiveMembership()
    {
        var fixture = new Fixture();

        var result = await fixture.UpdateAsync(fixture.Organization.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.Organization.Id, result.Value?.OrganizationId);
        Assert.Equal(1, fixture.MembershipWriteCount);
        Assert.Equal(OrganizationMembershipStatus.Active, fixture.InsertedMembership?.Status);
    }

    [Fact]
    public async Task SelectingExistingMembership_DoesNotCreateDuplicate()
    {
        var fixture = new Fixture();
        fixture.ExistingMembership = OrganizationMembership.Create(fixture.Organization.Id, fixture.User.Id, fixture.ActorUserId, Now);
        fixture.ExistingMembership.Activate(Now);

        var result = await fixture.UpdateAsync(fixture.Organization.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, fixture.MembershipWriteCount);
    }

    [Fact]
    public async Task SelectingRemovedMembership_RejoinsWithoutNewRow()
    {
        var fixture = new Fixture();
        fixture.ExistingMembership = OrganizationMembership.Create(fixture.Organization.Id, fixture.User.Id, fixture.ActorUserId, Now);
        fixture.ExistingMembership.Activate(Now);
        fixture.ExistingMembership.Remove(fixture.ActorUserId, Now.AddMinutes(1));

        var result = await fixture.UpdateAsync(fixture.Organization.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, fixture.MembershipWriteCount);
        Assert.Equal(OrganizationMembershipStatus.Active, fixture.ExistingMembership.Status);
    }

    [Fact]
    public async Task SelectingAnotherOrganization_ReturnsValidationError()
    {
        var fixture = new Fixture();

        var result = await fixture.UpdateAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(0, fixture.MembershipWriteCount);
    }

    private sealed class Fixture
    {
        private int _userGetCalls;

        public Guid ActorUserId { get; } = Guid.NewGuid();
        public ApplicationUser User { get; } = new()
        {
            Id = Guid.NewGuid(),
            Email = "employee@example.com",
            UserName = "employee",
            NormalizedEmail = "EMPLOYEE@EXAMPLE.COM",
            NormalizedUserName = "EMPLOYEE",
        };
        public Organization Organization { get; } = Organization.Create("acme", "Acme", Now);
        public OrganizationMembership? ExistingMembership { get; set; }
        public OrganizationMembership? InsertedMembership { get; private set; }
        public int MembershipWriteCount { get; private set; }

        public async Task<Nexora.Application.Common.Results.Result<Nexora.Application.Features.V1.SystemManagements.User.Model.UserResponse>> UpdateAsync(Guid? organizationId)
        {
            var profile = UserProfile.Create(User.Id, "Employee", LanguageCode.Create("vi"), null, Now);
            var userRepository = StubProxy<IUserRepository>.Create((method, _) => method.Name switch
            {
                "GetAsync" => Task.FromResult<ApplicationUser?>(++_userGetCalls == 1 ? User : null),
                _ => throw new NotSupportedException(method.Name)
            });
            var profileRepository = StubProxy<IUserProfileRepository>.Create((method, _) => method.Name switch
            {
                "GetByUserIdAsync" => Task.FromResult<UserProfile?>(profile),
                _ => throw new NotSupportedException(method.Name)
            });
            var organizationRepository = StubProxy<IOrganizationRepository>.Create((method, _) => method.Name switch
            {
                "GetByIdAsync" => Task.FromResult<Organization?>(Organization),
                _ => throw new NotSupportedException(method.Name)
            });
            var membershipRepository = StubProxy<IOrganizationMembershipRepository>.Create((method, args) => method.Name switch
            {
                "GetForUpdateAsync" => Task.FromResult(ExistingMembership),
                "GetActiveByUsersAsync" => Task.FromResult(ExistingMembership?.Status == OrganizationMembershipStatus.Active
                    ? new List<OrganizationMembership> { ExistingMembership }
                    : new List<OrganizationMembership>()),
                "Insert" => RecordInsert(args),
                _ => throw new NotSupportedException(method.Name)
            });
            var unitOfWork = StubProxy<IUnitOfWork>.Create((method, _) => method.Name switch
            {
                "get_UserRepository" => userRepository,
                "get_UserProfileRepository" => profileRepository,
                "get_OrganizationRepository" => organizationRepository,
                "get_OrganizationMembershipRepository" => membershipRepository,
                _ => throw new NotSupportedException(method.Name)
            });
            var handler = new UpdateUserCommandHandler(unitOfWork, new TestClock(),
                new TestOrganizationProvider(Organization.Id), new TestUserContext(ActorUserId));
            return await handler.Handle(new UpdateUserCommand(User.Id, User.Email!, User.UserName!, null,
                "Employee", "vi", null, organizationId), CancellationToken.None);

            object? RecordInsert(object?[]? args)
            {
                InsertedMembership = (OrganizationMembership?)args?[0];
                MembershipWriteCount++;
                return null;
            }
        }
    }

    private sealed class TestOrganizationProvider(Guid organizationId) : IDefaultOrganizationProvider
    {
        public Task<Guid?> GetOrganizationIdAsync(CancellationToken cancellationToken) => Task.FromResult<Guid?>(organizationId);
    }

    private sealed class TestUserContext(Guid userId) : IUserContext
    {
        public Guid? UserId => userId;
        public bool IsAuthenticated => true;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    public class StubProxy<T> : DispatchProxy where T : class
    {
        private Func<MethodInfo, object?[]?, object?> _handler = null!;

        public static T Create(Func<MethodInfo, object?[]?, object?> handler)
        {
            var proxy = DispatchProxy.Create<T, StubProxy<T>>();
            ((StubProxy<T>)(object)proxy)._handler = handler;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            _handler(targetMethod ?? throw new InvalidOperationException(), args);
    }
}
