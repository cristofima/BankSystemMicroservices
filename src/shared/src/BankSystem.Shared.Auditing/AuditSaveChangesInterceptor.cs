using BankSystem.Shared.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BankSystem.Shared.Auditing;

/// <summary>
/// EF Core SaveChanges interceptor to auto-fill audit fields
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        SetAuditData(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        SetAuditData(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SetAuditData(DbContextEventData eventData)
    {
        var entries =
            eventData
                .Context?.ChangeTracker.Entries()
                .Where(e =>
                    e is { Entity: AuditedEntity, State: EntityState.Added or EntityState.Modified }
                ) ?? [];

        foreach (var entry in entries)
        {
            var entity = (AuditedEntity)entry.Entity;

            switch (entry.State)
            {
                case EntityState.Modified:
                    entity.UpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Added:
                    entity.CreatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}
