namespace ModApi.Application.Exceptions;

/// <summary>Accumulates validation errors so every problem is reported in a single response.</summary>
internal sealed class ValidationErrorCollector
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.OrdinalIgnoreCase);

    public bool HasErrors => _errors.Count > 0;

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
        if (HasErrors)
        {
            throw new RequestValidationException(
                _errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
        }
    }
}
