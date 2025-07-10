using System.Net.Mail;

namespace Account.Domain.ValueObjects;

/// <summary>
/// Represents an email address value object with validation
/// </summary>
public record EmailAddress
{
    public string Value { get; }

    public EmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email address cannot be null or empty", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email address format", nameof(email));

        Value = email.ToLowerInvariant().Trim();
    }

    /// <summary>
    /// Gets the domain part of the email address
    /// </summary>
    public string Domain => Value.Split('@')[1];

    /// <summary>
    /// Gets the local part of the email address
    /// </summary>
    public string LocalPart => Value.Split('@')[0];

    /// <summary>
    /// Creates a masked version of the email for display purposes
    /// </summary>
    public string ToMaskedString()
    {
        var parts = Value.Split('@');
        if (parts.Length != 2)
            return "***@***.***";

        var localPart = parts[0];
        var domain = parts[1];

        var maskedLocal = localPart.Length <= 2
            ? new string('*', localPart.Length)
            : $"{localPart[0]}{new string('*', localPart.Length - 2)}{localPart[^1]}";

        var domainParts = domain.Split('.');
        var maskedDomain = domainParts.Length > 1
            ? $"{new string('*', domainParts[0].Length)}.{domainParts[^1]}"
            : new string('*', domain.Length);

        return $"{maskedLocal}@{maskedDomain}";
    }

    /// <summary>
    /// Validates email address format
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    public static implicit operator string(EmailAddress email) => email.Value;

    public static explicit operator EmailAddress(string email) => new(email);

    public override string ToString() => Value;
}