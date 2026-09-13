using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.Domain.Common.ValueObjects;

public sealed record RouteTemplate
{
    private RouteTemplate(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static RouteTemplate Create(string value)
    {
        var normalized = DomainGuard.RequiredText(value, nameof(value), 300);

        // '#' is reserved for menu-only ApiFunction parent records. It is never
        // used as an HTTP route by the authorization middleware.
        if (normalized == "#")
        {
            return new RouteTemplate(normalized);
        }

        if (!normalized.StartsWith('/') ||
            normalized.Contains('?') ||
            normalized.Contains('#') ||
            normalized.Any(char.IsWhiteSpace))
        {
            throw new DomainException("api_function.invalid_route", DomainMessages.InvalidRouteTemplate);
        }

        return new RouteTemplate(normalized);
    }

    public override string ToString() => Value;
}
