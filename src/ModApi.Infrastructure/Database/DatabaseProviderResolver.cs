namespace ModApi.Infrastructure.Database;

/// <summary>Chooses the <see cref="IDatabaseProviderConfigurator"/> for the current configuration.</summary>
public interface IDatabaseProviderResolver
{
    /// <summary>
    /// Returns the configurator named by <paramref name="providerName"/>, or – when no name is given –
    /// the single configurator that recognises <paramref name="connectionString"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">No provider (or more than one) matches.</exception>
    IDatabaseProviderConfigurator Resolve(string? providerName, string connectionString);
}

/// <inheritdoc />
public sealed class DatabaseProviderResolver : IDatabaseProviderResolver
{
    private readonly IReadOnlyList<IDatabaseProviderConfigurator> _configurators;

    public DatabaseProviderResolver(IEnumerable<IDatabaseProviderConfigurator> configurators)
    {
        ArgumentNullException.ThrowIfNull(configurators);
        _configurators = configurators.ToArray();
    }

    /// <inheritdoc />
    public IDatabaseProviderConfigurator Resolve(string? providerName, string connectionString)
    {
        if (!string.IsNullOrWhiteSpace(providerName))
        {
            // Last registration wins so hosts can override a built-in provider.
            return _configurators.LastOrDefault(c => c.Matches(providerName))
                ?? throw new InvalidOperationException(
                    $"Unknown database provider '{providerName}'. Available providers: {AvailableNames()}.");
        }

        var candidates = _configurators.Where(c => c.CanHandle(connectionString)).ToArray();
        return candidates.Length switch
        {
            1 => candidates[0],
            0 => throw new InvalidOperationException(
                $"Could not infer the database provider from the connection string. Set 'Database:Provider' to one of: {AvailableNames()}."),
            _ => throw new InvalidOperationException(
                $"The connection string matches several providers ({string.Join(", ", candidates.Select(c => c.Name))}). Set 'Database:Provider' explicitly."),
        };
    }

    private string AvailableNames() => string.Join(", ", _configurators.Select(c => c.Name).Distinct());
}
