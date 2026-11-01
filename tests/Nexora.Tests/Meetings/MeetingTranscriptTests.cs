using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Meetings.Command;
using Nexora.Application.Features.V1.Meetings.Query;
using Nexora.Domain.Meetings;

namespace Nexora.Tests.Meetings;

public sealed class MeetingTranscriptTests
{
    [Fact]
    public async Task ParticipantCanChooseTargetLanguageButOutsiderAndInvalidValueCannot()
    {
        var now = DateTimeOffset.UtcNow;
        var organization = Guid.NewGuid();
        var speaker = Guid.NewGuid();
        var meeting = Meeting.Create(organization, speaker, "Test", now.AddMinutes(-1), now.AddHours(1), now.AddMinutes(-2));
        var transcriptStore = new FakeTranscriptStore(meeting, speaker);
        var meetingStore = new FakeMeetingStore(meeting, speaker);
        var handler = new SetMeetingTargetLanguageCommandHandler(meetingStore, transcriptStore,
            new TestUserContext(speaker), new TestTenantContext(organization));

        Assert.True((await handler.Handle(new SetMeetingTargetLanguageCommand(meeting.Id, "vi"), CancellationToken.None)).IsSuccess);
        Assert.Equal("vi", transcriptStore.TargetLanguage);
        Assert.Equal(Error.Validation("language").Code,
            (await handler.Handle(new SetMeetingTargetLanguageCommand(meeting.Id, "fr"), CancellationToken.None)).Error!.Code);

        var outsider = new SetMeetingTargetLanguageCommandHandler(meetingStore, transcriptStore,
            new TestUserContext(Guid.NewGuid()), new TestTenantContext(organization));
        Assert.Equal(Error.Forbidden("participant").Code,
            (await outsider.Handle(new SetMeetingTargetLanguageCommand(meeting.Id, "en"), CancellationToken.None)).Error!.Code);
        Assert.Equal("vi", transcriptStore.TargetLanguage);
    }

    [Fact]
    public async Task AgentCanSaveFinalSegmentIdempotentlyForParticipant()
    {
        var now = DateTimeOffset.UtcNow;
        var organization = Guid.NewGuid();
        var speaker = Guid.NewGuid();
        var meeting = Meeting.Create(organization, speaker, "Test", now.AddMinutes(-1), now.AddHours(1), now.AddMinutes(-2));
        meeting.Start(now);
        var store = new FakeTranscriptStore(meeting, speaker);
        var handler = new SaveMeetingTranscriptCommandHandler(store);
        var segmentId = Guid.NewGuid();
        var command = new SaveMeetingTranscriptCommand(meeting.Id, segmentId, speaker, now.AddSeconds(1), now.AddSeconds(2), "vi", "Xin chào");

        Assert.True((await handler.Handle(command, CancellationToken.None)).IsSuccess);
        Assert.True((await handler.Handle(command, CancellationToken.None)).IsSuccess);
        Assert.Single(store.Segments);
        Assert.False((await handler.Handle(command with { Text = "Other" }, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task AgentCannotSaveForOutsiderOrInvalidLanguage()
    {
        var now = DateTimeOffset.UtcNow;
        var speaker = Guid.NewGuid();
        var meeting = Meeting.Create(Guid.NewGuid(), speaker, "Test", now.AddMinutes(-1), now.AddHours(1), now.AddMinutes(-2));
        meeting.Start(now);
        var handler = new SaveMeetingTranscriptCommandHandler(new FakeTranscriptStore(meeting, speaker));
        var command = new SaveMeetingTranscriptCommand(meeting.Id, Guid.NewGuid(), Guid.NewGuid(), now.AddSeconds(1), now.AddSeconds(2), "vi", "Hello");

        Assert.Equal(Error.Forbidden("speaker").Code, (await handler.Handle(command, CancellationToken.None)).Error!.Code);
        Assert.Equal(Error.Validation("language").Code, (await handler.Handle(command with { SourceLanguage = "fr" }, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task AgentLanguageLookupRejectsOutsider()
    {
        var now = DateTimeOffset.UtcNow;
        var speaker = Guid.NewGuid();
        var meeting = Meeting.Create(Guid.NewGuid(), speaker, "Test", now.AddMinutes(-1), now.AddHours(1), now.AddMinutes(-2));
        meeting.Start(now);
        var handler = new GetAgentSpeakerLanguageQueryHandler(new FakeTranscriptStore(meeting, speaker));

        Assert.Equal("vi", (await handler.Handle(new GetAgentSpeakerLanguageQuery(meeting.Id, speaker), CancellationToken.None)).Value);
        Assert.Equal(Error.Forbidden("speaker").Code,
            (await handler.Handle(new GetAgentSpeakerLanguageQuery(meeting.Id, Guid.NewGuid()), CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task JobListIncludesOnlyActiveMeetingWithWorkerToken()
    {
        var now = DateTimeOffset.UtcNow;
        var speaker = Guid.NewGuid();
        var meeting = Meeting.Create(Guid.NewGuid(), speaker, "Test", now.AddMinutes(-1), now.AddHours(1), now.AddMinutes(-2));
        var handler = new GetAgentTranscriptionJobsQueryHandler(new FakeTranscriptStore(meeting, speaker), new FakeRoomProvider());
        Assert.Empty(await handler.Handle(new GetAgentTranscriptionJobsQuery(), CancellationToken.None));

        meeting.Start(now);
        var job = Assert.Single(await handler.Handle(new GetAgentTranscriptionJobsQuery(), CancellationToken.None));
        Assert.Equal(meeting.Id, job.MeetingId);
        Assert.Equal(meeting.RoomExternalId, job.RoomName);
        Assert.Equal("worker-token", job.Token);

        meeting.BeginEnding();
        Assert.Empty(await handler.Handle(new GetAgentTranscriptionJobsQuery(), CancellationToken.None));
        meeting.Finish(now.AddMinutes(1));
        Assert.Empty(await handler.Handle(new GetAgentTranscriptionJobsQuery(), CancellationToken.None));
    }

    private sealed class FakeRoomProvider : IMeetingRoomProvider
    {
        public Task CreateAsync(string roomName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(string roomName, CancellationToken cancellationToken) => Task.CompletedTask;
        public MeetingJoinToken IssueToken(string roomName, Guid userId) => throw new NotImplementedException();
        public MeetingJoinToken IssueTranscriptWorkerToken(string roomName, Guid meetingId) => new("wss://example", "worker-token");
    }

    private sealed class TestUserContext(Guid userId) : IUserContext
    {
        public Guid? UserId => userId;
        public bool IsAuthenticated => true;
    }

    private sealed class TestTenantContext(Guid organizationId) : ITenantContext
    {
        public Guid? OrganizationId => organizationId;
    }

    private sealed class FakeMeetingStore(Meeting meeting, Guid participantId) : IMeetingStore
    {
        public Task<Meeting?> GetAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken) =>
            Task.FromResult<Meeting?>(organizationId == meeting.OrganizationId && meetingId == meeting.Id ? meeting : null);
        public Task<bool> IsParticipantAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(organizationId == meeting.OrganizationId && meetingId == meeting.Id && userId == participantId);
        public Task<IReadOnlyList<Meeting>> ListAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> AreActiveMembersAsync(Guid organizationId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<MeetingUserOption>> GetParticipantUsersAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<PagedResult<MeetingUserOption>> FindEligibleUsersAsync(Guid organizationId, string? search, int page, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SyncParticipantUsersAsync(Meeting meeting, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> TrySaveUpdateAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public void Add(Meeting meeting, IReadOnlyCollection<MeetingParticipant> participants) => throw new NotImplementedException();
        public Task SaveAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class FakeTranscriptStore(Meeting meeting, Guid allowedSpeaker) : IMeetingTranscriptStore
    {
        public List<MeetingTranscriptSegment> Segments { get; } = [];
        public string? TargetLanguage { get; private set; }
        public Task<string?> GetMyLanguageAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(userId == allowedSpeaker ? "vi" : null);
        public Task<bool> SetMyLanguageAsync(Guid organizationId, Guid meetingId, Guid userId, string language, CancellationToken cancellationToken) =>
            Task.FromResult(userId == allowedSpeaker);
        public Task<string?> GetMyTargetLanguageAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(userId == allowedSpeaker ? TargetLanguage : null);
        public Task<bool> SetMyTargetLanguageAsync(Guid organizationId, Guid meetingId, Guid userId, string language, CancellationToken cancellationToken)
        {
            if (organizationId != meeting.OrganizationId || meetingId != meeting.Id || userId != allowedSpeaker)
                return Task.FromResult(false);
            TargetLanguage = language;
            return Task.FromResult(true);
        }
        public Task<IReadOnlyList<MeetingTranscriptSegment>> ListAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MeetingTranscriptSegment>>(Segments);
        public Task<Meeting?> FindMeetingForAgentAsync(Guid meetingId, CancellationToken cancellationToken) =>
            Task.FromResult<Meeting?>(meetingId == meeting.Id ? meeting : null);
        public Task<IReadOnlyList<Meeting>> ListActiveForAgentAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Meeting>>(meeting.Status == "Live" ? [meeting] : []);
        public Task<string?> GetSpeakerLanguageForAgentAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(organizationId == meeting.OrganizationId && meetingId == meeting.Id && userId == allowedSpeaker ? "vi" : null);
        public Task<bool> SaveFinalAsync(MeetingTranscriptSegment segment, CancellationToken cancellationToken)
        {
            var existing = Segments.SingleOrDefault(s => s.Id == segment.Id);
            if (existing is null) Segments.Add(segment);
            return Task.FromResult(existing is null || existing.Text == segment.Text);
        }
    }
}
