namespace BankSystem.Shared.Domain.ValueObjects;

/// <summary>
/// Represents a phone number value object with validation and formatting
/// </summary>
public record PhoneNumber
{
    public string Value { get; }
    public string CountryCode { get; }
    public string NationalNumber { get; }

    public PhoneNumber(string phoneNumber, string? countryCode = null)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be null or empty", nameof(phoneNumber));

        var cleaned = CleanPhoneNumber(phoneNumber);

        if (countryCode != null)
        {
            countryCode = CleanCountryCode(countryCode);
            if (!IsValidCountryCode(countryCode))
                throw new ArgumentException("Invalid country code format", nameof(countryCode));
        }

        // Extract country code from number if not provided
        if (countryCode == null && cleaned.StartsWith('+'))
        {
            var extracted = ExtractCountryCode(cleaned);
            countryCode = extracted.countryCode;
            cleaned = extracted.nationalNumber;
        }

        CountryCode = countryCode ?? "+1"; // Default to US
        NationalNumber = cleaned;

        if (!IsValidNationalNumber(NationalNumber))
            throw new ArgumentException("Invalid phone number format", nameof(phoneNumber));

        Value = $"{CountryCode}{NationalNumber}";
    }

    /// <summary>
    /// Formats the phone number for display
    /// </summary>
    public string ToFormattedString()
    {
        return CountryCode switch
        {
            "+1" when NationalNumber.Length == 10 =>
                $"{CountryCode} ({NationalNumber[..3]}) {NationalNumber[3..6]}-{NationalNumber[6..]}",
            "+44" when NationalNumber.Length >= 10 =>
                $"{CountryCode} {NationalNumber[..4]} {NationalNumber[4..7]} {NationalNumber[7..]}",
            _ => $"{CountryCode} {NationalNumber}"
        };
    }

    /// <summary>
    /// Creates a masked version for display purposes
    /// </summary>
    public string ToMaskedString()
    {
        if (NationalNumber.Length <= 4)
            return $"{CountryCode} {new string('*', NationalNumber.Length)}";

        const int visibleDigits = 2;
        var maskedMiddle = new string('*', NationalNumber.Length - (visibleDigits * 2));
        return $"{CountryCode} {NationalNumber[..visibleDigits]}{maskedMiddle}{NationalNumber[^visibleDigits..]}";
    }

    /// <summary>
    /// Gets the international format (E.164)
    /// </summary>
    public string ToInternationalFormat() => Value;

    /// <summary>
    /// Cleans the phone number by removing formatting characters
    /// </summary>
    private static string CleanPhoneNumber(string phoneNumber)
    {
        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Cleans the country code
    /// </summary>
    private static string CleanCountryCode(string countryCode)
    {
        var cleaned = new string(countryCode.Where(c => char.IsDigit(c) || c == '+').ToArray());
        return cleaned.StartsWith('+') ? cleaned : $"+{cleaned}";
    }

    /// <summary>
    /// Validates country code format
    /// </summary>
    private static bool IsValidCountryCode(string countryCode)
    {
        return countryCode.StartsWith('+') &&
               countryCode.Length is >= 2 and <= 4 &&
               countryCode[1..].All(char.IsDigit);
    }

    /// <summary>
    /// Validates national number format
    /// </summary>
    private static bool IsValidNationalNumber(string nationalNumber)
    {
        return nationalNumber.Length is >= 7 and <= 15 &&
               nationalNumber.All(char.IsDigit);
    }

    /// <summary>
    /// Extracts country code from international format
    /// </summary>
    private static (string countryCode, string nationalNumber) ExtractCountryCode(string phoneNumber)
    {
        if (!phoneNumber.StartsWith('+'))
            return ("+1", phoneNumber);

        var digits = phoneNumber[1..];

        // Common country codes (simplified)
        var countryCodes = new[] { "1", "44", "49", "33", "39", "34", "81", "86", "91" };

        foreach (var code in countryCodes.OrderByDescending(c => c.Length))
        {
            if (digits.StartsWith(code))
            {
                return ($"+{code}", digits[code.Length..]);
            }
        }

        // Default: assume first 1-3 digits are country code
        var countryCodeLength = Math.Min(3, digits.Length - 7);
        return ($"+{digits[..countryCodeLength]}", digits[countryCodeLength..]);
    }

    public static implicit operator string(PhoneNumber phone) => phone.Value;

    public override string ToString() => ToFormattedString();
}