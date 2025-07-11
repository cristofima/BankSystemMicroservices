namespace BankSystem.Shared.Domain.Common;

/// <summary>
/// Base class for entities that need audit tracking
/// </summary>
public abstract class AuditedEntity
{
    /// <summary>
    /// Gets the date and time when the Entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the date and time when the Entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
