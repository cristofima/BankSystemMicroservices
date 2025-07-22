namespace BankSystem.Shared.Domain.Common;

/// <summary>
/// Base class for entities with an identity.
/// </summary>
public abstract class Entity<TId> : AuditedEntity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public TId Id { get; protected set; }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> entity)
            return false;

        if (ReferenceEquals(this, entity))
            return true;

        if (GetType() != entity.GetType())
            return false;

        return !EqualityComparer<TId>.Default.Equals(Id, default!) &&
               EqualityComparer<TId>.Default.Equals(Id, entity.Id);
    }

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);
}