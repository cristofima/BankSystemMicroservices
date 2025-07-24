using BankSystem.Shared.Domain.ValueObjects;

namespace BankSystem.Shared.Domain.UnitTests.ValueObjects;

public class CurrencyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNullOrWhitespace(string invalid)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Currency(invalid!));
        Assert.Contains("Currency code cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("usdollar")]
    [InlineData("EURO")]
    [InlineData("ABC")]
    public void Constructor_ShouldThrow_WhenInvalidCurrencyCode(string invalid)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Currency(invalid));
        Assert.StartsWith("Invalid currency code", exception.Message);
    }

    [Theory]
    [InlineData("USD", "US Dollar", "$")]
    [InlineData("usd", "US Dollar", "$")]
    [InlineData("EUR", "Euro", "€")]
    [InlineData("eur", "Euro", "€")]
    [InlineData("GBP", "British Pound Sterling", "£")]
    [InlineData("gbp", "British Pound Sterling", "£")]
    public void Constructor_ShouldCreate_WhenValid(string input, string expectedName, string expectedSymbol)
    {
        var currency = new Currency(input);

        Assert.Equal(input.ToUpperInvariant(), currency.Code);
        Assert.Equal(expectedName, currency.Name);
        Assert.Equal(expectedSymbol, currency.Symbol);
    }

    [Theory]
    [InlineData("USD", true)]
    [InlineData("usd", true)]
    [InlineData("EUR", true)]
    [InlineData("eur", true)]
    [InlineData("GBP", true)]
    [InlineData("gbp", true)]
    [InlineData("JPY", false)]
    [InlineData("", false)]
    public void IsValidCurrencyCode_ShouldReturnExpected(string code, bool expected)
    {
        Assert.Equal(expected, Currency.IsValidCurrencyCode(code!));
    }

    [Fact]
    public void GetSupportedCurrencies_ShouldReturnExpectedCodes()
    {
        var supported = Currency.GetSupportedCurrencies();

        Assert.Contains("USD", supported);
        Assert.Contains("EUR", supported);
        Assert.Contains("GBP", supported);
        Assert.Equal(3, supported.Count);
    }

    [Fact]
    public void PredefinedCurrencies_ShouldBeCorrect()
    {
        var usd = Currency.USD;
        Assert.Equal("USD", usd.Code);
        Assert.Equal("US Dollar", usd.Name);
        Assert.Equal("$", usd.Symbol);

        var eur = Currency.EUR;
        Assert.Equal("EUR", eur.Code);
        Assert.Equal("Euro", eur.Name);
        Assert.Equal("€", eur.Symbol);

        var gbp = Currency.GBP;
        Assert.Equal("GBP", gbp.Code);
        Assert.Equal("British Pound Sterling", gbp.Name);
        Assert.Equal("£", gbp.Symbol);
    }

    [Fact]
    public void DecimalPlaces_ShouldBeTwo()
    {
        Assert.Equal(2, Currency.DecimalPlaces);
    }

    [Fact]
    public void ToString_ShouldReturnCurrencyCode()
    {
        var currency = new Currency("USD");
        Assert.Equal("USD", currency.ToString());
    }

    [Fact]
    public void ImplicitOperator_ToString_ShouldReturnCode()
    {
        var currency = new Currency("EUR");
        string code = currency;
        Assert.Equal("EUR", code);
    }

    [Fact]
    public void ExplicitOperator_FromString_ShouldReturnCurrency()
    {
        var currency = (Currency)"GBP";
        Assert.Equal("GBP", currency.Code);
    }
}