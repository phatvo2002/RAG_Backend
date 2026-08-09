using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Nexora.Infrastructure.Persistence.Configurations;

internal static class EnumStringConversion
{
    public static ValueConverter<TEnum, string> Create<TEnum>()
        where TEnum : struct, Enum
    {
        return new ValueConverter<TEnum, string>(
            value => value.ToString().ToUpperInvariant(),
            value => Enum.Parse<TEnum>(value, ignoreCase: true));
    }
}
