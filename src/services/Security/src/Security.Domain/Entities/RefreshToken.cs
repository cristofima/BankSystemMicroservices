using Security.Domain.Common;

namespace Security.Domain.Entities;

/// <summary>
/// Represents a refresh token with proper security and audit tracking
/// </summary>
public class RefreshToken : AuditedEntity
{
    /// <summary>
    /// Unique token value (primary key)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// JWT ID that this refresh token is paired with
    /// </summary>
    public string JwtId { get; set; } = string.Empty;

    /// <summary>
    /// When this refresh token expires
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Whether this token has been manually invalidated/revoked
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Date when token was revoked (if applicable)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Reason for revocation
    /// </summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    /// IP address where token was created
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// IP address where token was revoked (if applicable)
    /// </summary>
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// User this token belongs to
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Token that replaced this one (for rotation tracking)
    /// </summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// Device/User agent information
    /// </summary>
    public string? DeviceInfo { get; set; }

    // Domain methods
    public bool IsExpired => DateTime.UtcNow >= ExpiryDate;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? ipAddress = null, string? reason = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = ipAddress;
        RevocationReason = reason;
    }

    public void ReplaceWith(string newToken)
    {
        ReplacedByToken = newToken;
        Revoke(reason: "Replaced by new token");
    }
}
