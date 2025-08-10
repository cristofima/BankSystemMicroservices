using MediatR;

namespace BankSystem.Shared.Kernel.Events;

/// <summary>
/// Represents a domain event that occurred within the domain model.
/// Domain events are used for internal communication within the same bounded context.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Event contract version for backward compatibility.
    /// </summary>
    int Version { get; }
}
