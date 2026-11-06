using System.ComponentModel;
using System.Globalization;

namespace ModApi.Application.Querying;

public static class ValueParser
{
    private const DateTimeStyles AssumeUtc = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;

    // The literal "null" maps to null for nullable targets. Dates without an offset are treated as UTC.
    public static bool TryParse(string? raw, Type targetType, out object? value)
    {
        value = null;

        var underlying = Nullable.GetUnderlyingType(targetType);
        var type = underlying ?? targetType;

        if (raw is null || raw.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return underlying is not null || !targetType.IsValueType;
        }

        try
        {
            value = Convert(raw, type);
            return value is not null;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException or NotSupportedException or InvalidCastException)
        {
            value = null;
            return false;
        }
    }

    private static object? Convert(string raw, Type type)
    {
        var culture = CultureInfo.InvariantCulture;

        if (type == typeof(string))
        {
            return raw;
        }

        if (type.IsEnum)
        {
            return Enum.TryParse(type, raw, ignoreCase: true, out var result) && Enum.IsDefined(type, result!) ? result : null;
        }

        if (type == typeof(Guid))
        {
            return Guid.TryParse(raw, out var guid) ? guid : null;
        }

        if (type == typeof(bool))
        {
            return bool.TryParse(raw, out var b) ? b : null;
        }

        if (type == typeof(DateTime))
        {
            return DateTime.TryParse(raw, culture, AssumeUtc, out var dt) ? dt : null;
        }

        if (type == typeof(DateTimeOffset))
        {
            return DateTimeOffset.TryParse(raw, culture, AssumeUtc, out var dto) ? dto : null;
        }

        if (type == typeof(DateOnly))
        {
            return DateOnly.TryParse(raw, culture, DateTimeStyles.None, out var date) ? date : null;
        }

        if (type == typeof(TimeOnly))
        {
            return TimeOnly.TryParse(raw, culture, DateTimeStyles.None, out var time) ? time : null;
        }

        if (type == typeof(TimeSpan))
        {
            return TimeSpan.TryParse(raw, culture, out var span) ? span : null;
        }

        var converter = TypeDescriptor.GetConverter(type);
        return converter.CanConvertFrom(typeof(string)) ? converter.ConvertFromInvariantString(raw) : null;
    }
}
