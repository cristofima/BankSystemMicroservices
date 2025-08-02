using System.ComponentModel.DataAnnotations;

namespace Security.Application.Dtos;

/// <summary>
/// Request model for resetting password
/// </summary>
public record ResetPasswordDto
{
    /// <summary>
    /// Email address for password reset
    /// </summary>
    /// <example>user@example.com</example>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Password reset token
    /// </summary>
    [Required(ErrorMessage = "Reset token is required")]
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// New password for the account
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string NewPassword { get; init; } = string.Empty;
}