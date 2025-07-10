namespace Account.Domain.Common;

/// <summary>
/// Represents a domain event that occurred within the banking system.
/// Domain events are used to communicate changes between aggregates and bounded contexts.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this domain event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the date and time when this domain event occurred.
    /// </summary>
    DateTime OccurredAt { get; }
}
