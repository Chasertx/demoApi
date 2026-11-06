using Microsoft.EntityFrameworkCore;
using ModApi.Application.Abstractions;
using ModApi.Application.Exceptions;

namespace ModApi.Infrastructure.Persistence;

/// <summary>
/// Commits the scoped <see cref="AppDbContext"/> and translates provider-specific EF Core exceptions into
/// provider-agnostic application exceptions, so the API layer never depends on EF Core.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException("The record was modified or deleted by another process. Reload it and try again.", exception);
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException(
                "The change conflicts with existing data (for example a duplicate unique value or a missing related record).",
                exception);
        }
    }
}
