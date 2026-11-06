using System.Data.Common;

namespace ModApi.Infrastructure.Database;

public static class ConnectionStringKeys
{
    public static IReadOnlySet<string> Parse(string? connectionString)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return keys;
        }

        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            foreach (string key in builder.Keys)
            {
                keys.Add(key.Trim());
            }
        }
        catch (ArgumentException)
        {
            // not a valid connection string; treat it as having no keys
        }

        return keys;
    }
}
