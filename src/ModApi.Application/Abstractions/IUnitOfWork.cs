namespace ModApi.Application.Abstractions;

/// <summary>Commits all staged changes atomically.</summary>
public interface IUnitOfWork
{
    /// <summary>Persists staged changes and returns the number of affected rows.</summary>
    /// <exception cref="Exceptions.ConflictException">A constraint or concurrency conflict occurred.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
