using Microsoft.AspNetCore.Identity;
using Security.Domain.Entities;

namespace Security.Infrastructure.Validators;

/// <summary>
/// Custom password validator with enhanced security requirements following OWASP guidelines
/// </summary>
public class CustomPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        var errors = new List<IdentityError>();

        // Check if password is null or empty
        if (string.IsNullOrEmpty(password))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordRequired",
                Description = "Password is required."
            });
            return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
        }

        // Check for common passwords (OWASP recommended check)
        var commonPasswords = new[] { "password", "123456", "qwerty", "admin", "letmein", "welcome", "monkey", "dragon" };
        if (commonPasswords.Any(p => string.Equals(p, password, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add(new IdentityError
            {
                Code = "CommonPassword",
                Description = "Password is too common and easily guessable."
            });
        }

        // Check for username in password
        if (!string.IsNullOrEmpty(user.UserName) && 
            password.Contains(user.UserName, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordContainsUserName",
                Description = "Password cannot contain the username."
            });
        }

        // Check for email in password
        if (!string.IsNullOrEmpty(user.Email) && 
            password.Contains(user.Email.Split('@')[0], StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordContainsEmail",
                Description = "Password cannot contain parts of the email address."
            });
        }

        // Check for repeating characters (security best practice)
        if (HasRepeatingCharacters(password))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordHasRepeatingCharacters",
                Description = "Password cannot have more than 2 consecutive repeating characters."
            });
        }

        return Task.FromResult(errors.Count == 0 
            ? IdentityResult.Success 
            : IdentityResult.Failed(errors.ToArray()));
    }

    /// <summary>
    /// Checks if password has more than 2 consecutive repeating characters
    /// </summary>
    private static bool HasRepeatingCharacters(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i + 1] == password[i + 2])
            {
                return true;
            }
        }
        return false;
    }
}
