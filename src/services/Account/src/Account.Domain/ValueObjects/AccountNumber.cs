using BankSystem.Shared.Domain.Exceptions;

namespace Account.Domain.ValueObjects;

/// <summary>
/// Represents a bank account number with validation and formatting logic.
/// </summary>
public record AccountNumber
{
    private const int AccountNumberLength = 10;
    private const string AccountNumberPrefix = "ACC";

    public string Value { get; }

    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Account number cannot be null or empty");

        var cleanValue = value.Trim().ToUpperInvariant();
        
        if (!IsValidFormat(cleanValue))
            throw new DomainException($"Invalid account number format: {value}. Expected format: ACC followed by 10 digits");

        Value = cleanValue;
    }

    /// <summary>
    /// Generates a new unique account number.
    /// </summary>
    /// <returns>A new AccountNumber instance with generated value</returns>
    public static AccountNumber Generate()
    {
        var random = new Random();
        var digits = new char[AccountNumberLength];
        
        for (var i = 0; i < AccountNumberLength; i++)
        {
            digits[i] = (char)('0' + random.Next(0, 10));
        }
        
        var accountNumber = AccountNumberPrefix + new string(digits);
        return new AccountNumber(accountNumber);
    }

    /// <summary>
    /// Creates an AccountNumber from a string value.
    /// </summary>
    /// <param name="value">The account number string</param>
    /// <returns>AccountNumber instance</returns>
    public static AccountNumber FromString(string value)
    {
        return new AccountNumber(value);
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

        var cleanValue = value.Trim().ToUpperInvariant();

        // Must start with ACC prefix
        if (!cleanValue.StartsWith(AccountNumberPrefix))
            return false;

        // Must have exact length (prefix + digits)
        if (cleanValue.Length != AccountNumberPrefix.Length + AccountNumberLength)
            return false;

        // Remaining characters must be digits
        var digits = cleanValue[AccountNumberPrefix.Length..];
        return digits.All(char.IsDigit);
    }

    /// <summary>
    /// Gets the numeric part of the account number.
    /// </summary>
    /// <returns>The numeric portion as a string</returns>
    public string GetNumericPart()
    {
        return Value[AccountNumberPrefix.Length..];
    }

    /// <summary>
    /// Gets a masked version of the account number for display purposes.
    /// </summary>
    /// <returns>Masked account number (e.g., ACC****5678)</returns>
    public string GetMaskedValue()
    {
        if (Value.Length <= 7)
            return Value;

        var prefix = Value[..3]; // ACC
        var suffix = Value[^4..]; // Last 4 digits
        var maskedMiddle = new string('*', Value.Length - 7);
        
        return prefix + maskedMiddle + suffix;
    }

    public override string ToString() => Value;

    public static implicit operator string(AccountNumber accountNumber) => accountNumber.Value;

    public static explicit operator AccountNumber(string value) => new(value);
}
