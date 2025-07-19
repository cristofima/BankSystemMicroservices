using BankSystem.Shared.Domain.Exceptions;

namespace BankSystem.Shared.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; init; }
    public Currency Currency { get; init; }

    // Private constructor for EF Core
    private Money() { }

    // Primary constructor with validation
    public Money(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        // Allow negative balances, but enforce precision
        if (decimal.Round(amount, Currency.DecimalPlaces) != amount)
            throw new DomainException($"Amount has too many decimal places for currency {currency.Code}");

        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(Currency currency) => new(0, currency);

    /// <summary>
    /// Adds two Money values. Both must have the same currency.
    /// </summary>
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot add {Currency.Code} to {other.Currency.Code}");

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Subtracts another Money value from this one. Both must have the same currency.
    /// </summary>
    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot subtract {other.Currency.Code} from {Currency.Code}");

        return new Money(Amount - other.Amount, Currency);
    }

    /// <summary>
    /// Checks if this Money value is greater than another.
    /// </summary>
    public bool IsGreaterThan(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot compare {Currency.Code} with {other.Currency.Code}");

        return Amount > other.Amount;
    }

    /// <summary>
    /// Checks if this Money value is greater than or equal to another.
    /// </summary>
    public bool IsGreaterThanOrEqual(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot compare {Currency.Code} with {other.Currency.Code}");

        return Amount >= other.Amount;
    }

    /// <summary>
    /// Checks if this Money value is less than another.
    /// </summary>
    public bool IsLessThan(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot compare {Currency.Code} with {other.Currency.Code}");

        return Amount < other.Amount;
    }

    /// <summary>
    /// Checks if this Money value is zero.
    /// </summary>
    public bool IsZero => Amount == 0;

    /// <summary>
    /// Checks if this Money value is positive.
    /// </summary>
    public bool IsPositive => Amount > 0;

    /// <summary>
    /// Checks if this Money value is negative.
    /// </summary>
    public bool IsNegative => Amount < 0;

    public override string ToString() => $"{Amount:F2} {Currency.Code}";
}