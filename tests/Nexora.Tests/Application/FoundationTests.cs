using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Results;
using Nexora.Application.Common.Services;

namespace Nexora.Tests.Application;

public sealed class FoundationTests
{
    [Fact]
    public void Result_Success_ShouldExposeValueAndNoError()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Result_Failure_ShouldExposeErrorAndNoValue()
    {
        var result = Result<int>.Failure(new Error("test.failure", "Failure"));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Equal("test.failure", result.Error?.Code);
    }

    [Fact]
    public void BaseService_ShouldExposeTrustedContext()
    {
        var service = new TestService(
            new TestUserContext(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            new TestTenantContext(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
            new TestClock(new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero)));

        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), service.UserId);
        Assert.Equal(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), service.OrganizationId);
        Assert.Equal(new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero), service.Now);
    }

    private sealed class TestService(IUserContext userContext, ITenantContext tenantContext, IClock clock) : BaseService(userContext, tenantContext, clock)
    {
        public Guid UserId => CurrentUserId;

        public Guid OrganizationId => CurrentOrganizationId;

        public DateTimeOffset Now => UtcNow;
    }

    private sealed class TestUserContext(Guid userId) : IUserContext
    {
        public Guid? UserId { get; } = userId;

        public bool IsAuthenticated => true;
    }

    private sealed class TestTenantContext(Guid organizationId) : ITenantContext
    {
        public Guid? OrganizationId { get; } = organizationId;
    }

    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
