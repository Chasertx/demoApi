using Microsoft.EntityFrameworkCore;
using ModApi.Application.Abstractions;
using ModApi.Application.Exceptions;

namespace ModApi.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException("The record was changed or deleted by someone else. Reload it and try again.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new ConflictException("The change conflicts with existing data, e.g. a duplicate unique value or a missing related record.", ex);
        }
    }
}
