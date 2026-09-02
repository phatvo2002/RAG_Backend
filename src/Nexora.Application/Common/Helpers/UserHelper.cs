using System.Globalization;
using Nexora.Application.Common.Constants;

namespace Nexora.Application.Common.Helpers;

public static class UserHelper
{
    public static string NormalizeUserName(string? userName)
    {
        var trimmed = userName?.Trim() ?? string.Empty;
        var builder = new System.Text.StringBuilder(trimmed.Length);
        var previousWasSpace = false;

        foreach (var character in trimmed)
        {
            if (character == ' ')
            {
                if (!previousWasSpace)
                {
                    builder.Append(character);
                }

                previousWasSpace = true;
                continue;
            }

            builder.Append(character);
            previousWasSpace = false;
        }

        return builder.ToString();
    }

    public static bool IsValidUserName(string? userName)
    {
        var normalizedUserName = NormalizeUserName(userName);
        if (normalizedUserName.Length == 0)
        {
            return false;
        }

        for (var index = 0; index < normalizedUserName.Length; index++)
        {
            var character = normalizedUserName[index];
            if (character == ' ')
            {
                continue;
            }

            var category = CharUnicodeInfo.GetUnicodeCategory(normalizedUserName, index);
            if (category is not (UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark))
            {
                return false;
            }
        }

        return true;
    }

    public static string InvalidUserNameMessage => CommonMessages.User.UserNameInvalid;
}
