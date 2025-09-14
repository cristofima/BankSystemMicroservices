namespace BankSystem.Shared.Domain.Common;

/// <summary>
/// Base class for entities that need audit tracking
/// </summary>
public abstract class AuditedEntity
{
    /// <summary>
    /// Gets the date and time when the Entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets the date and time when the Entity was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
