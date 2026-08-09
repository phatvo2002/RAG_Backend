using Nexora.Domain.Common.Exceptions;
using Nexora.Domain.Common.ValueObjects;

namespace Nexora.Tests.Domain;

public sealed class ValueObjectTests
{
    [Fact]
    public void OrganizationSlug_WithInvalidFormat_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            OrganizationSlug.Create("Acme Vietnam"));

        Assert.Equal("organization.invalid_slug", exception.Code);
    }

    [Fact]
    public void LanguageCode_WithMixedCase_ShouldNormalizeToLowerCase()
    {
        var language = LanguageCode.Create("EN-US");

        Assert.Equal("en-us", language.Value);
    }

    [Fact]
    public void RouteTemplate_WithQueryString_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            RouteTemplate.Create("/api/users?include=roles"));

        Assert.Equal("api_function.invalid_route", exception.Code);
    }
}
