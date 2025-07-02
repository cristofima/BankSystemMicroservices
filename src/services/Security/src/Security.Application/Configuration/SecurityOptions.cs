using System.ComponentModel.DataAnnotations;

namespace Security.Application.Configuration;

/// <summary>
/// Configuration options for security policies
/// </summary>
public class SecurityOptions
{
    public const string SectionName = "Security";

    public int MaxFailedLoginAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

    [Required]
    public PasswordPolicyOptions PasswordPolicy { get; set; } = new();

    [Required]
    public TokenSecurityOptions TokenSecurity { get; set; } = new();

    [Required]
    public AuditOptions Audit { get; set; } = new();

    public class PasswordPolicyOptions
    {
        public int MinLength { get; set; } = 8;
        public bool RequireSpecialCharacters { get; set; } = true;
        public bool RequireNumbers { get; set; } = true;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
    }

    public class TokenSecurityOptions
    {
        public bool EnableTokenRotation { get; set; } = true;
        public bool EnableRevocationCheck { get; set; } = true;
        public int MaxConcurrentSessions { get; set; } = 5;
        public int CleanupExpiredTokensAfterDays { get; set; } = 30;
    }

    public class AuditOptions
    {
        public bool EnableAuditLogging { get; set; } = true;
        public bool LogSuccessfulAuthentication { get; set; } = true;
        public bool LogFailedAuthentication { get; set; } = true;
        public bool LogTokenOperations { get; set; } = true;
        public bool LogUserOperations { get; set; } = true;
    }
}
