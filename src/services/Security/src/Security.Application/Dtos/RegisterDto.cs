using System.ComponentModel.DataAnnotations;

namespace Security.Application.Dtos;

/// <summary>
/// Request model for user registration
/// </summary>
public record RegisterRequest
{
    /// <summary>
    /// Unique username for the account
    /// </summary>
    /// <example>john.doe</example>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(256, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 256 characters")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, underscores, and hyphens")]
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// Email address for the account
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// First name of the user
    /// </summary>
    /// <example>John</example>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Last name of the user
    /// </summary>
    /// <example>Doe</example>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Password for the account (minimum 8 characters)
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Password confirmation
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; init; } = string.Empty;
}

/// <summary>
/// Response model for successful user registration
/// </summary>
public record RegisterResponse(
    string UserId,
    string UserName,
    string Email,
    DateTime CreatedAt);