namespace Security.Domain.Common;

/// <summary>
/// Base class for entities that need audit tracking
/// </summary>
public abstract class AuditedEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
