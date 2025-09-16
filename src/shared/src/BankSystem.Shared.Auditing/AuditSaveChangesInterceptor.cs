using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BankSystem.Shared.Auditing;

/// <summary>
/// EF Core SaveChanges interceptor to auto-fill audit fields
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;

    public AuditSaveChangesInterceptor(ICurrentUser currentUser)
    {
        Guard.AgainstNull(currentUser);
        _currentUser = currentUser;
    }

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

    private void SetAuditData(DbContextEventData eventData)
    {
        var entries =
            eventData
                .Context?.ChangeTracker.Entries()
                .Where(e =>
                    e is { Entity: AuditedEntity, State: EntityState.Added or EntityState.Modified }
                ) ?? [];

        var userName = string.IsNullOrWhiteSpace(_currentUser.UserName)
            ? "system"
            : _currentUser.UserName;

        foreach (var entry in entries)
        {
            var entity = (AuditedEntity)entry.Entity;

            switch (entry.State)
            {
                case EntityState.Modified:
                    entity.UpdatedAt = DateTimeOffset.UtcNow;
                    entity.UpdatedBy = userName;
                    break;

                case EntityState.Added:
                    entity.CreatedAt = DateTimeOffset.UtcNow;
                    entity.CreatedBy = userName;
                    break;
            }
        }
    }
}
