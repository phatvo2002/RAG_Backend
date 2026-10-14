using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Meetings.Command;
using Nexora.Domain.Meetings;

namespace Nexora.Tests.Meetings;

public sealed class UpdateMeetingCommandTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = Start.AddHours(1);

    [Fact]
    public async Task HostCanUpdateDetailsAndReplaceUserParticipants()
    {
        var organizationId = Guid.NewGuid();
        var hostId = Guid.NewGuid();
        var removedId = Guid.NewGuid();
        var addedId = Guid.NewGuid();
        var meeting = Meeting.Create(organizationId, hostId, "Cũ", Start, End, Start.AddDays(-1));
        var store = new FakeMeetingStore(meeting, hostId, removedId, addedId);
        store.ParticipantIds.AddRange([hostId, removedId]);
        var handler = new UpdateMeetingCommandHandler(store, new TestUserContext(hostId), new TestTenantContext(organizationId));

        var result = await handler.Handle(new UpdateMeetingCommand(meeting.Id, "  Mới  ", Start.AddHours(2),
            End.AddHours(2), [addedId], meeting.RowVersion), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Mới", meeting.Title);
        Assert.Equal(Start.AddHours(2), meeting.ScheduledStart);
        Assert.Equal(new[] { hostId, addedId }.OrderBy(id => id), store.ParticipantIds.OrderBy(id => id));
        Assert.Equal(1, store.SaveCount);
        Assert.Contains(result.Value!.Participants, participant => participant.Id == hostId);
        Assert.DoesNotContain(result.Value.Participants, participant => participant.Id == removedId);
    }

    [Fact]
    public async Task ParticipantCannotUpdateMeeting()
    {
        var organizationId = Guid.NewGuid();
        var hostId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var meeting = Meeting.Create(organizationId, hostId, "Cũ", Start, End, Start.AddDays(-1));
        var store = new FakeMeetingStore(meeting, hostId, participantId);
        store.ParticipantIds.AddRange([hostId, participantId]);
        var handler = new UpdateMeetingCommandHandler(store, new TestUserContext(participantId), new TestTenantContext(organizationId));

        var result = await handler.Handle(new UpdateMeetingCommand(meeting.Id, "Mới", Start, End,
            [participantId], meeting.RowVersion), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Error.Forbidden("Không có quyền sửa cuộc họp.").Code, result.Error!.Code);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task UserFromAnotherOrganizationCannotUpdateMeeting()
    {
        var organizationId = Guid.NewGuid();
        var hostId = Guid.NewGuid();
        var meeting = Meeting.Create(organizationId, hostId, "Cũ", Start, End, Start.AddDays(-1));
        var store = new FakeMeetingStore(meeting, hostId);
        store.ParticipantIds.Add(hostId);
        var handler = new UpdateMeetingCommandHandler(store, new TestUserContext(hostId), new TestTenantContext(Guid.NewGuid()));

        var result = await handler.Handle(new UpdateMeetingCommand(meeting.Id, "Mới", Start, End, [],
            meeting.RowVersion), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Error.Forbidden("Không có quyền sửa cuộc họp.").Code, result.Error!.Code);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task LiveOrStaleMeetingReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var hostId = Guid.NewGuid();
        var meeting = Meeting.Create(organizationId, hostId, "Cũ", Start, End, Start.AddDays(-1));
        var store = new FakeMeetingStore(meeting, hostId);
        store.ParticipantIds.Add(hostId);
        var handler = new UpdateMeetingCommandHandler(store, new TestUserContext(hostId), new TestTenantContext(organizationId));
        var stale = await handler.Handle(new UpdateMeetingCommand(meeting.Id, "Mới", Start, End, [],
            meeting.RowVersion + 1), CancellationToken.None);
        meeting.Start(Start);
        var live = await handler.Handle(new UpdateMeetingCommand(meeting.Id, "Mới", Start, End, [],
            meeting.RowVersion), CancellationToken.None);

        Assert.Equal(Error.Conflict("Cuộc họp đã thay đổi.").Code, stale.Error!.Code);
        Assert.Equal(Error.Conflict("Cuộc họp đã thay đổi.").Code, live.Error!.Code);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task InvalidMemberAndConcurrentSaveAreRejected()
    {
        var organizationId = Guid.NewGuid();
        var hostId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var meeting = Meeting.Create(organizationId, hostId, "Cũ", Start, End, Start.AddDays(-1));
        var store = new FakeMeetingStore(meeting, hostId) { CanSave = false };
        store.ParticipantIds.Add(hostId);
        var handler = new UpdateMeetingCommandHandler(store, new TestUserContext(hostId), new TestTenantContext(organizationId));
        var invalid = await handler.Handle(new UpdateMeetingCommand(meeting.Id, "Mới", Start, End, [outsiderId],
            meeting.RowVersion), CancellationToken.None);
        var concurrent = await handler.Handle(new UpdateMeetingCommand(meeting.Id, "Mới", Start, End, [],
            meeting.RowVersion), CancellationToken.None);

        Assert.Equal(Error.Validation("Người tham gia không hợp lệ.").Code, invalid.Error!.Code);
        Assert.Equal(Error.Conflict("Cuộc họp đã thay đổi.").Code, concurrent.Error!.Code);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task CreateRejectsUserOutsideOrganization()
    {
        var organizationId = Guid.NewGuid();
        var hostId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var meeting = Meeting.Create(organizationId, hostId, "Mẫu", Start, End, Start.AddDays(-1));
        var store = new FakeMeetingStore(meeting, hostId);
        var handler = new CreateMeetingCommandHandler(store, new TestUserContext(hostId),
            new TestTenantContext(organizationId), new TestClock());

        var result = await handler.Handle(new CreateMeetingCommand("Mới", Start, End, [outsiderId]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Error.Validation("Người tham gia không hợp lệ.").Code, result.Error!.Code);
    }

    [Fact]
    public async Task StartReturnsConflictWhenMeetingWasEditedConcurrently()
    {
        var organizationId = Guid.NewGuid();
        var hostId = Guid.NewGuid();
        var meeting = Meeting.Create(organizationId, hostId, "Mẫu", Start, End, Start.AddDays(-1));
        var store = new FakeMeetingStore(meeting, hostId) { CanSave = false };
        store.ParticipantIds.Add(hostId);
        var handler = new StartMeetingCommandHandler(store, new FakeRoomProvider(), new TestUserContext(hostId),
            new TestTenantContext(organizationId), new TestClock());

        var result = await handler.Handle(new StartMeetingCommand(meeting.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Error.Conflict("Cuộc họp đã thay đổi.").Code, result.Error!.Code);
    }

    private sealed class TestUserContext(Guid id) : IUserContext
    {
        public Guid? UserId { get; } = id;
        public bool IsAuthenticated => true;
    }

    private sealed class TestTenantContext(Guid organizationId) : ITenantContext
    {
        public Guid? OrganizationId { get; } = organizationId;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Start.AddDays(-1);
    }

    private sealed class FakeRoomProvider : IMeetingRoomProvider
    {
        public Task CreateAsync(string roomName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(string roomName, CancellationToken cancellationToken) => Task.CompletedTask;
        public MeetingJoinToken IssueToken(string roomName, Guid userId) => throw new NotImplementedException();
        public MeetingJoinToken IssueTranscriptWorkerToken(string roomName, Guid meetingId) => throw new NotImplementedException();
    }

    private sealed class FakeMeetingStore(Meeting meeting, params Guid[] eligibleIds) : IMeetingStore
    {
        public List<Guid> ParticipantIds { get; } = [];
        public bool CanSave { get; set; } = true;
        public int SaveCount { get; private set; }

        public Task<Meeting?> GetAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken) =>
            Task.FromResult<Meeting?>(organizationId == meeting.OrganizationId && meetingId == meeting.Id ? meeting : null);
        public Task<IReadOnlyList<Meeting>> ListAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<bool> IsParticipantAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(ParticipantIds.Contains(userId));
        public Task<bool> AreActiveMembersAsync(Guid organizationId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            Task.FromResult(userIds.All(eligibleIds.Contains));
        public Task<IReadOnlyList<MeetingUserOption>> GetParticipantUsersAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MeetingUserOption>>(ParticipantIds.Select(id => new MeetingUserOption(id, id.ToString(), "")).ToArray());
        public Task<PagedResult<MeetingUserOption>> FindEligibleUsersAsync(Guid organizationId, string? search, int page, int pageSize,
            CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SyncParticipantUsersAsync(Meeting value, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
        {
            ParticipantIds.Clear();
            ParticipantIds.AddRange(userIds);
            return Task.CompletedTask;
        }
        public Task<bool> TrySaveUpdateAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(CanSave);
        }
        public void Add(Meeting value, IReadOnlyCollection<MeetingParticipant> participants) => throw new NotImplementedException();
        public Task SaveAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
