using System.Text;

namespace ModApi.Application.Entities;

public static class RouteNaming
{
    // OrderLine -> order-lines, Category -> categories
    public static string ToRoute(string typeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

        var words = SplitWords(typeName);
        if (words.Count == 0)
        {
            throw new ArgumentException($"'{typeName}' does not contain any letters or digits.", nameof(typeName));
        }

        words[^1] = Pluralize(words[^1]);
        return string.Join('-', words).ToLowerInvariant();
    }

    public static string Pluralize(string word)
    {
        if (word.Length == 0)
        {
            return word;
        }

        var lower = word.ToLowerInvariant();
        if (lower.Length > 1 && lower[^1] == 'y' && !"aeiou".Contains(lower[^2]))
        {
            return word[..^1] + "ies";
        }

        if (lower.EndsWith('s') || lower.EndsWith('x') || lower.EndsWith('z') ||
            lower.EndsWith("ch", StringComparison.Ordinal) || lower.EndsWith("sh", StringComparison.Ordinal))
        {
            return word + "es";
        }

        return word + "s";
    }

    private static List<string> SplitWords(string name)
    {
        var words = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (!char.IsLetterOrDigit(c))
            {
                Flush();
                continue;
            }

            if (char.IsUpper(c) && current.Length > 0)
            {
                var previous = name[i - 1];
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

                // Break on "aB" and "1B", and on the last capital of an acronym ("HTTPRequest" -> HTTP, Request).
                if (char.IsLower(previous) || char.IsDigit(previous) || nextIsLower)
                {
                    Flush();
                }
            }

            current.Append(c);
        }

        Flush();
        return words;
    }
}
