using System.Text.RegularExpressions;

using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.Domain.Common.ValueObjects;

public sealed record OrganizationSlug
{
    private static readonly Regex Format = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private OrganizationSlug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static OrganizationSlug Create(string value)
    {
        var normalized = DomainGuard.RequiredText(value, nameof(value), 80).ToLowerInvariant();

        if (!Format.IsMatch(normalized))
        {
            throw new DomainException("organization.invalid_slug", DomainMessages.InvalidOrganizationSlug);
        }

        return new OrganizationSlug(normalized);
    }

    public override string ToString() => Value;
}
