using System.Globalization;

using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.Domain.Common;

internal static class DomainGuard
{
    public static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("domain.required_id", string.Format(CultureInfo.InvariantCulture, DomainMessages.RequiredId, parameterName));
        }

        return value;
    }

    public static string RequiredText(string? value, string parameterName, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainException("domain.required_text", string.Format(CultureInfo.InvariantCulture, DomainMessages.RequiredText, parameterName));
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainException("domain.text_too_long", string.Format(CultureInfo.InvariantCulture, DomainMessages.TextTooLong, parameterName, maxLength));
        }

        return normalized;
    }

    public static DateTimeOffset Utc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException("domain.utc_required", string.Format(CultureInfo.InvariantCulture, DomainMessages.UtcRequired, parameterName));
        }

        return value;
    }

    public static void EnsureBefore(DateTimeOffset start, DateTimeOffset end, string code, string message)
    {
        if (start >= end)
        {
            throw new DomainException(code, message);
        }
    }
}
