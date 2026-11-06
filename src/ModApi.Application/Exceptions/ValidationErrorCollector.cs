namespace ModApi.Application.Exceptions;

internal sealed class ValidationErrorCollector
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string key, string message)
    {
        if (!_errors.TryGetValue(key, out var messages))
        {
            messages = [];
            _errors[key] = messages;
        }

        messages.Add(message);
    }

    public void ThrowIfAny()
    {
        if (_errors.Count > 0)
        {
            throw new RequestValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }
}
