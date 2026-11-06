namespace ModApi.Application.Abstractions;

/// <summary>Prepares the database at startup (nothing, ensure-created, or migrate depending on configuration).</summary>
public interface IDatabaseInitializer
{
    /// <summary>Runs the configured initialization strategy.</summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
