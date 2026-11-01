using Nexora.Application.Common.Abstractions.Chat;

namespace Nexora.Application.Features.V1.SystemPrompts.Model;

internal static class SystemPromptResponseMapper
{
    public static SystemPromptResponse Map(SystemPromptData value) =>
        new(value.Id, value.PromptType, value.RowVersion, Map(value.ActiveVersion),
            value.Draft is null ? null : Map(value.Draft));

    public static SystemPromptVersionResponse Map(SystemPromptVersionData value) =>
        new(value.Id, value.VersionNo, value.Instructions, value.Status, value.CreatedByUserId,
            value.CreatedAt, value.PublishedByUserId, value.PublishedAt);
}
