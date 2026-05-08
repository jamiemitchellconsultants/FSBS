using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FSBS.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps <c>CreatedAt</c>, <c>CreatedBy</c>, <c>UpdatedAt</c>, and <c>UpdatedBy</c>
/// on every <see cref="AuditableEntity"/> before <c>SaveChanges</c> completes.
/// Registered as a scoped service so it resolves <see cref="ICurrentUser"/> from
/// the correct request scope on every save.
/// </summary>
public sealed class AuditInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;

        var now = DateTimeOffset.UtcNow;
        // Null when unauthenticated (e.g. the dev seed endpoint, or background jobs).
        Guid? userId = currentUser.IsAuthenticated ? currentUser.UserId : null;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State is EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
            }
        }
    }
}
