using System.ComponentModel.DataAnnotations;

namespace Security.Application.Dtos;

/// <summary>
/// Request model for user login
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(256, ErrorMessage = "Username cannot exceed 256 characters")]
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    public string Password { get; init; } = string.Empty;
}