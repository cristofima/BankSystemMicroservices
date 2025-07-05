using System.ComponentModel.DataAnnotations;

namespace Security.Application.Dtos;

/// <summary>
/// Request model for initiating password reset
/// </summary>
public record ForgotPasswordDto
{
    /// <summary>
    /// Email address for password reset
    /// </summary>
    /// <example>user@example.com</example>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; init; } = string.Empty;
}