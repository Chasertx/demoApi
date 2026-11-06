using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ModApi.Domain.Common;

namespace ModApi.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps <see cref="IAuditable"/> entities on save: <c>CreatedAtUtc</c> on insert, <c>UpdatedAtUtc</c> on update,
/// and prevents <c>CreatedAtUtc</c> from ever being modified. Uses <see cref="TimeProvider"/> for testability.
/// </summary>
public sealed class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider;

    public AuditingInterceptor(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditInformation(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>Applies audit timestamps to every tracked <see cref="IAuditable"/> entry.</summary>
    public void ApplyAuditInformation(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.UpdatedAtUtc = null;
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.CreatedAtUtc).IsModified = false;
                    entry.Entity.UpdatedAtUtc = now;
                    break;
            }
        }
    }
}
