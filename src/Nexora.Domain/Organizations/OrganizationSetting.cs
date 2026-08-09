using System.Text.Json;

using Nexora.Domain.Common;
using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common.ValueObjects;

namespace Nexora.Domain.Organizations;

public sealed class OrganizationSetting : AuditableEntityBase
{
    private OrganizationSetting()
    {
    }

    private OrganizationSetting(Guid id, Guid organizationId, LanguageCode defaultLanguage, TimeZoneId timeZone, JsonDocument? settingsJson, DateTimeOffset now)
        : base(id, now)
    {
        OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId));
        DefaultLanguage = defaultLanguage;
        TimeZone = timeZone;
        SettingsJson = settingsJson;
    }

    public Guid OrganizationId { get; private set; }

    public LanguageCode DefaultLanguage { get; private set; } = null!;

    public TimeZoneId TimeZone { get; private set; } = null!;

    public JsonDocument? SettingsJson { get; private set; }

    public Organization? Organization { get; private set; }

    public static OrganizationSetting Create(
        Guid organizationId, LanguageCode defaultLanguage, TimeZoneId timeZone,
        DateTimeOffset now, JsonDocument? settingsJson = null)
    {
        return new OrganizationSetting(Guid.NewGuid(), organizationId, defaultLanguage, timeZone, settingsJson, now);
    }

    public void UpdateLanguage(LanguageCode defaultLanguage)
    {
        DefaultLanguage = defaultLanguage;
    }

    public void UpdateTimeZone(TimeZoneId timeZone)
    {
        TimeZone = timeZone;
    }

    public void UpdateSettings(JsonDocument? settingsJson)
    {
        SettingsJson = settingsJson;
    }
}
