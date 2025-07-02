using Microsoft.AspNetCore.Identity;
using Security.Domain.Common;

namespace Security.Domain.Entities;

/// <summary>
/// Application user entity extending IdentityUser with business-specific properties
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's first name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Links this user to a client record, used in JWT claims
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Collection of refresh tokens for this user
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last login date
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    // Domain methods
    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsLockedOut(int maxAttempts, TimeSpan lockoutDuration)
    {
        return FailedLoginAttempts >= maxAttempts && 
               UpdatedAt.HasValue && 
               DateTime.UtcNow.Subtract(UpdatedAt.Value) < lockoutDuration;
    }

    public void ResetLockout()
    {
        FailedLoginAttempts = 0;
        UpdatedAt = DateTime.UtcNow;
    }
}
