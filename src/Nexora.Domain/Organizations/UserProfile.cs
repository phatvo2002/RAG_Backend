using Nexora.Domain.Common;
using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common.ValueObjects;

namespace Nexora.Domain.Organizations;

public sealed class UserProfile : UserProfileEntityBase
{
    private UserProfile()
    {
    }

    private UserProfile(Guid userId, string displayName, LanguageCode preferredLanguage, TimeZoneId? timeZone, DateTimeOffset now)
        : base(userId, now)
    {
        DisplayName = DomainGuard.RequiredText(displayName, nameof(displayName), 200);
        PreferredLanguage = preferredLanguage;
        TimeZone = timeZone;
    }

    public string DisplayName { get; private set; } = string.Empty;

    public LanguageCode PreferredLanguage { get; private set; } = null!;

    public TimeZoneId? TimeZone { get; private set; }

    public string? AvatarObjectKey { get; private set; }

    public static UserProfile Create(Guid userId, string displayName, LanguageCode preferredLanguage, TimeZoneId? timeZone, DateTimeOffset now)
    {
        return new UserProfile(userId, displayName, preferredLanguage, timeZone, now);
    }

    public void UpdateDisplayName(string displayName)
    {
        DisplayName = DomainGuard.RequiredText(displayName, nameof(displayName), 200);
    }

    public void UpdatePreferredLanguage(LanguageCode preferredLanguage)
    {
        PreferredLanguage = preferredLanguage;
    }

    public void UpdateTimeZone(TimeZoneId? timeZone)
    {
        TimeZone = timeZone;
    }

    public void UpdateAvatarObjectKey(string? avatarObjectKey)
    {
        AvatarObjectKey = avatarObjectKey is null
            ? null
            : DomainGuard.RequiredText(avatarObjectKey, nameof(avatarObjectKey), 500);
    }
}
