using Nexora.Domain.Common.Constants;

namespace Nexora.Domain.Common.Exceptions;

public sealed class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(DomainMessages.DomainErrorCodeRequired, nameof(code));
        }

        Code = code;
    }

    public string Code { get; }
}
