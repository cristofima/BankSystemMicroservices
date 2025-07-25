using BankSystem.Shared.Domain.Exceptions;

namespace BankSystem.Account.Domain.ValueObjects;

/// <summary>
/// Represents a bank account number with validation and formatting logic.
/// </summary>
public record AccountNumber
{
    private const int AccountNumberLength = 10;
    private static readonly Random Random = new();
    private static readonly Lock RandomLock = new();

    public string Value { get; }

    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Account number cannot be null or empty");

        var cleanValue = value.Trim();

        if (!IsValidFormat(cleanValue))
            throw new DomainException($"Invalid account number format: {value}. Expected format are 10 digits");

        Value = cleanValue;
    }

    /// <summary>
    /// Generates a new unique account number.
    /// </summary>
    /// <returns>A new AccountNumber instance with generated value</returns>
    public static AccountNumber Generate()
    {
        var digits = new char[AccountNumberLength];

        lock (RandomLock)
        {
            for (var i = 0; i < AccountNumberLength; i++)
            {
                digits[i] = (char)('0' + Random.Next(0, 10));
            }
        }

        return new AccountNumber(new string(digits));
    }

    /// <summary>
    /// Validates the account number format.
    /// </summary>
    /// <param name="value">The account number to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidFormat(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var cleanValue = value.Trim();

        // Must have exact length and characters must be digits
        return cleanValue.Length == AccountNumberLength && cleanValue.All(char.IsDigit);
    }

    /// <summary>
    /// Gets a masked version of the account number for display purposes.
    /// </summary>
    /// <returns>Masked account number (e.g., ******5678)</returns>
    public string GetMaskedValue()
    {
        var suffix = Value[^4..]; // Last 4 digits
        var maskedMiddle = new string('*', Value.Length - 4);

        return maskedMiddle + suffix;
    }

    public override string ToString() => Value;

    public static implicit operator string(AccountNumber accountNumber) => accountNumber.Value;

    public static explicit operator AccountNumber(string value) => new(value);
}