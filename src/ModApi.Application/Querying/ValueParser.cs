using System.ComponentModel;
using System.Globalization;

namespace ModApi.Application.Querying;

/// <summary>Converts raw query-string values to strongly typed CLR values using the invariant culture.</summary>
public static class ValueParser
{
    /// <summary>
    /// Tries to convert <paramref name="raw"/> to <paramref name="targetType"/>.
    /// The literal <c>null</c> (case-insensitive) converts to <c>null</c> for nullable targets.
    /// Dates without an offset are interpreted as UTC.
    /// </summary>
    public static bool TryParse(string? raw, Type targetType, out object? value)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        value = null;

        var underlying = Nullable.GetUnderlyingType(targetType);
        var type = underlying ?? targetType;
        var allowsNull = underlying is not null || !targetType.IsValueType;

        if (raw is null || string.Equals(raw, "null", StringComparison.OrdinalIgnoreCase))
        {
            return allowsNull;
        }

        try
        {
            value = Convert(raw, type);
            return value is not null;
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException or NotSupportedException or InvalidCastException)
        {
            value = null;
            return false;
        }
    }

    private static object? Convert(string raw, Type type)
    {
        const DateTimeStyles utc = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;
        var culture = CultureInfo.InvariantCulture;

        if (type == typeof(string))
        {
            return raw;
        }

        if (type.IsEnum)
        {
            return Enum.TryParse(type, raw, ignoreCase: true, out var enumValue) && Enum.IsDefined(type, enumValue!)
                ? enumValue
                : null;
        }

        if (type == typeof(Guid))
        {
            return Guid.TryParse(raw, out var guid) ? guid : null;
        }

        if (type == typeof(bool))
        {
            return bool.TryParse(raw, out var boolean) ? boolean : null;
        }

        if (type == typeof(DateTime))
        {
            return DateTime.TryParse(raw, culture, utc, out var dateTime) ? dateTime : null;
        }

        if (type == typeof(DateTimeOffset))
        {
            return DateTimeOffset.TryParse(raw, culture, utc, out var offset) ? offset : null;
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
