using System.Data.Common;

namespace ModApi.Infrastructure.Database;

/// <summary>Extracts the keys of an ADO.NET connection string.</summary>
public static class ConnectionStringKeys
{
    /// <summary>Returns the lower-cased keys, or an empty set when the string cannot be parsed.</summary>
    public static IReadOnlySet<string> Parse(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new HashSet<string>();
        }

        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            return builder.Keys.Cast<string>()
                .Select(key => key.Trim().ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return new HashSet<string>();
        }
    }
}
