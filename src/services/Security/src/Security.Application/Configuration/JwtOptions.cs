using System.ComponentModel.DataAnnotations;

namespace Security.Application.Configuration;

/// <summary>
/// Configuration options for JWT authentication
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32, ErrorMessage = "JWT key must be at least 32 characters")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT issuer is required")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT audience is required")]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)] // 1 minute to 24 hours
    public int AccessTokenExpiryInMinutes { get; set; } = 15;

    [Range(1, 30)] // 1 to 30 days
    public int RefreshTokenExpiryInDays { get; set; } = 7;

    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
}
