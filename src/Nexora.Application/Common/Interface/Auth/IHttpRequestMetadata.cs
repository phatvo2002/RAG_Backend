namespace Nexora.Application.Common.Abstractions;

public interface IHttpRequestMetadata
{
    string? IpAddress { get; }

    string? UserAgent { get; }
}
