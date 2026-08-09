using System.Text.RegularExpressions;

using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.Domain.Common.ValueObjects;

public sealed record LanguageCode
{
    private static readonly Regex Format = new("^[a-z]{2,3}(?:-[a-z]{2})?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private LanguageCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static LanguageCode Create(string value)
    {
        var normalized = DomainGuard.RequiredText(value, nameof(value), 10).ToLowerInvariant();

        if (!Format.IsMatch(normalized))
        {
            throw new DomainException("language.invalid_code", DomainMessages.InvalidLanguageCode);
        }

        return new LanguageCode(normalized);
    }

    public override string ToString() => Value;
}
