using System.Text.RegularExpressions;

using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.Domain.Common.ValueObjects;

public sealed record ApiFunctionCode
{
    private static readonly Regex Format = new("^[A-Z][A-Z0-9_.-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private ApiFunctionCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ApiFunctionCode Create(string value)
    {
        var normalized = DomainGuard.RequiredText(value, nameof(value), 150).ToUpperInvariant();

        if (!Format.IsMatch(normalized))
        {
            throw new DomainException("api_function.invalid_code", DomainMessages.InvalidApiFunctionCode);
        }

        return new ApiFunctionCode(normalized);
    }

    public override string ToString() => Value;
}
