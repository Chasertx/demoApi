namespace ModApi.Infrastructure.Database;

public interface IDatabaseProviderResolver
{
    IDatabaseProviderConfigurator Resolve(string? providerName, string connectionString);
}

public sealed class DatabaseProviderResolver(IEnumerable<IDatabaseProviderConfigurator> configurators) : IDatabaseProviderResolver
{
    private readonly IDatabaseProviderConfigurator[] _configurators = configurators.ToArray();

    public IDatabaseProviderConfigurator Resolve(string? providerName, string connectionString)
    {
        if (!string.IsNullOrWhiteSpace(providerName))
        {
            // Last one wins, so an app can replace a built-in provider by registering its own.
            return _configurators.LastOrDefault(c => c.Matches(providerName))
                ?? throw new InvalidOperationException(
                    $"Unknown database provider '{providerName}'. Available providers: {AvailableNames()}.");
        }

        var candidates = _configurators.Where(c => c.CanHandle(connectionString)).ToArray();

        return candidates.Length switch
        {
            1 => candidates[0],
            0 => throw new InvalidOperationException(
                $"Could not work out the database provider from the connection string. Set 'Database:Provider' to one of: {AvailableNames()}."),
            _ => throw new InvalidOperationException(
                $"The connection string matches more than one provider ({string.Join(", ", candidates.Select(c => c.Name))}). Set 'Database:Provider' explicitly."),
        };
    }

    private string AvailableNames() => string.Join(", ", _configurators.Select(c => c.Name).Distinct());
}
