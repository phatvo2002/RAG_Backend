using System.Text.RegularExpressions;

using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.Domain.Common.ValueObjects;

public sealed record TimeZoneId
{
    private static readonly Regex Format = new("^[A-Za-z0-9_+./-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private TimeZoneId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static TimeZoneId Create(string value)
    {
        var normalized = DomainGuard.RequiredText(value, nameof(value), 50);

        if (!Format.IsMatch(normalized))
        {
            throw new DomainException("timezone.invalid_id", DomainMessages.InvalidTimeZoneId);
        }

        return new TimeZoneId(normalized);
    }

    public override string ToString() => Value;
}
