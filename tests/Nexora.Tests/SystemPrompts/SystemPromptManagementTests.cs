using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Domain.Chat;

namespace Nexora.Tests.SystemPrompts;

public sealed class SystemPromptManagementTests
{
    [Fact]
    public void Each_prompt_type_has_a_safe_nonempty_default()
    {
        Assert.Equal(2, SystemPromptTypes.All.Count);
        Assert.All(SystemPromptTypes.All, type => Assert.False(string.IsNullOrWhiteSpace(
            type == SystemPromptTypes.Chat ? SystemPromptDefaults.Chat : SystemPromptDefaults.MeetingTranslation)));
    }

    [Fact]
    public void Published_version_cannot_be_edited_or_published_again()
    {
        var profile = OrganizationSystemPrompt.Create(Guid.NewGuid(), SystemPromptTypes.Chat, DateTimeOffset.UtcNow);
        var version = SystemPromptVersion.Create(profile.Id, 1, "Initial", null, DateTimeOffset.UtcNow);
        version.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);
        profile.Activate(version);

        Assert.Throws<InvalidOperationException>(() => version.UpdateDraft("Changed"));
        Assert.Throws<InvalidOperationException>(() => version.Publish(null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Draft_can_be_updated_until_published()
    {
        var version = SystemPromptVersion.Create(Guid.NewGuid(), 2, "First", Guid.NewGuid(), DateTimeOffset.UtcNow);
        version.UpdateDraft("Second");
        Assert.Equal("Second", version.Instructions);
        Assert.Equal(SystemPromptVersion.DraftStatus, version.Status);
    }
}
