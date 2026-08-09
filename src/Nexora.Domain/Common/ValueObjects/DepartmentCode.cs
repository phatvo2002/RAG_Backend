using System.Text.RegularExpressions;

using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.Domain.Common.ValueObjects;

public sealed record DepartmentCode
{
    private static readonly Regex Format = new("^[A-Z][A-Z0-9_-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private DepartmentCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static DepartmentCode Create(string value)
    {
        var normalized = DomainGuard.RequiredText(value, nameof(value), 50).ToUpperInvariant();

        if (!Format.IsMatch(normalized))
        {
            throw new DomainException("department.invalid_code", DomainMessages.InvalidDepartmentCode);
        }

        return new DepartmentCode(normalized);
    }

    public override string ToString() => Value;
}
