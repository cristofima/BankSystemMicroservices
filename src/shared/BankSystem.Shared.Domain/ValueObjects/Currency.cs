namespace BankSystem.Shared.Domain.ValueObjects;

/// <summary>
/// Represents a currency with its ISO code and validation logic.
/// </summary>
public record Currency
{
    private static readonly HashSet<string> ValidCurrencyCodes =
    [
        "USD", "EUR", "GBP", "CAD", "AUD", "JPY"
    ];

    public string Code { get; }
    public string Name { get; }
    public string Symbol { get; }

    public Currency(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code cannot be null or empty", nameof(code));

        code = code.ToUpperInvariant();

        if (!IsValidCurrencyCode(code))
            throw new ArgumentException($"Invalid currency code: {code}", nameof(code));

        Code = code;
        Name = GetCurrencyName(code);
        Symbol = GetCurrencySymbol(code);
    }

    /// <summary>
    /// Creates a Currency instance from a currency code.
    /// </summary>
    /// <param name="code">The ISO currency code (e.g., "USD", "EUR")</param>
    /// <returns>A Currency instance</returns>
    public static Currency FromCode(string code) => new(code);

    /// <summary>
    /// Validates if the given currency code is supported.
    /// </summary>
    /// <param name="code">The currency code to validate</param>
    /// <returns>True if the currency code is valid, false otherwise</returns>
    public static bool IsValidCurrencyCode(string code)
    {
        return !string.IsNullOrWhiteSpace(code) && ValidCurrencyCodes.Contains(code.ToUpperInvariant());
    }

    /// <summary>
    /// Gets all supported currency codes.
    /// </summary>
    /// <returns>A collection of supported currency codes</returns>
    public static IReadOnlyCollection<string> GetSupportedCurrencies()
    {
        return ValidCurrencyCodes.ToList().AsReadOnly();
    }

    /// <summary>
    /// Predefined currency instances for common currencies.
    /// </summary>
    public static Currency USD => new("USD");
    public static Currency EUR => new("EUR");
    public static Currency GBP => new("GBP");

    /// <summary>
    /// Number of decimal places supported by this currency (e.g. JPY has 0, most others 2).
    /// </summary>
    public int DecimalPlaces => Code switch
    {
        "JPY" => 0,
        _     => 2
    };

    private static string GetCurrencyName(string code) => code switch
    {
        "USD" => "US Dollar",
        "EUR" => "Euro",
        "GBP" => "British Pound Sterling",
        "CAD" => "Canadian Dollar",
        "AUD" => "Australian Dollar",
        "JPY" => "Japanese Yen",
        _ => code
    };

    private static string GetCurrencySymbol(string code) => code switch
    {
        "USD" => "$",
        "EUR" => "€",
        "GBP" => "£",
        "CAD" => "C$",
        "AUD" => "A$",
        "JPY" => "¥",
        _ => code
    };

    public override string ToString() => Code;

    public static implicit operator string(Currency currency) => currency.Code;
    public static explicit operator Currency(string code) => new(code);
}
