using BankSystem.Shared.Domain.Exceptions;
using BankSystem.Shared.Domain.ValueObjects;

namespace BankSystem.Shared.Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    private readonly Currency _usd = new("USD");
    private readonly Currency _eur = new("EUR");

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Money(10, null!));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAmountHasTooManyDecimals()
    {
        var ex = Assert.Throws<DomainException>(() => new Money(10.123m, _usd));
        Assert.Contains("Amount has too many decimal places", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-5.50)]
    public void Constructor_ShouldCreate_WhenValid(decimal amount)
    {
        var money = new Money(amount, _usd);

        Assert.Equal(amount, money.Amount);
        Assert.Equal(_usd, money.Currency);
    }

    [Fact]
    public void Zero_ShouldReturnMoneyWithZeroAmount()
    {
        var money = Money.Zero(_usd);
        Assert.Equal(0, money.Amount);
        Assert.Equal(_usd, money.Currency);
    }

    [Fact]
    public void Add_ShouldReturnSum_WhenSameCurrency()
    {
        var m1 = new Money(50, _usd);
        var m2 = new Money(25, _usd);
        var result = m1.Add(m2);

        Assert.Equal(75, result.Amount);
        Assert.Equal(_usd, result.Currency);
    }

    [Fact]
    public void Add_ShouldThrow_WhenDifferentCurrency()
    {
        var m1 = new Money(50, _usd);
        var m2 = new Money(25, _eur);

        var ex = Assert.Throws<DomainException>(() => m1.Add(m2));
        Assert.Contains("Cannot add USD to EUR", ex.Message);
    }

    [Fact]
    public void Subtract_ShouldReturnDifference_WhenSameCurrency()
    {
        var m1 = new Money(50, _usd);
        var m2 = new Money(20, _usd);
        var result = m1.Subtract(m2);

        Assert.Equal(30, result.Amount);
        Assert.Equal(_usd, result.Currency);
    }

    [Fact]
    public void Subtract_ShouldThrow_WhenDifferentCurrency()
    {
        var m1 = new Money(50, _usd);
        var m2 = new Money(20, _eur);

        var ex = Assert.Throws<DomainException>(() => m1.Subtract(m2));
        Assert.Contains("Cannot subtract EUR from USD", ex.Message);
    }

    [Fact]
    public void IsGreaterThan_ShouldReturnTrue_WhenAmountIsGreater()
    {
        var m1 = new Money(100, _usd);
        var m2 = new Money(50, _usd);

        Assert.True(m1.IsGreaterThan(m2));
    }

    [Fact]
    public void IsGreaterThan_ShouldThrow_WhenDifferentCurrency()
    {
        var m1 = new Money(100, _usd);
        var m2 = new Money(50, _eur);

        Assert.Throws<DomainException>(() => m1.IsGreaterThan(m2));
    }

    [Fact]
    public void IsGreaterThanOrEqual_ShouldReturnExpectedResult()
    {
        var m1 = new Money(100, _usd);
        var m2 = new Money(100, _usd);
        var m3 = new Money(50, _usd);

        Assert.True(m1.IsGreaterThanOrEqual(m2));
        Assert.True(m1.IsGreaterThanOrEqual(m3));
        Assert.False(m3.IsGreaterThanOrEqual(m1));
    }

    [Fact]
    public void IsLessThan_ShouldReturnExpectedResult()
    {
        var m1 = new Money(25, _usd);
        var m2 = new Money(50, _usd);

        Assert.True(m1.IsLessThan(m2));
        Assert.False(m2.IsLessThan(m1));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(10, false)]
    [InlineData(-5, false)]
    public void IsZero_ShouldReturnExpectedResult(decimal amount, bool expected)
    {
        var money = new Money(amount, _usd);
        Assert.Equal(expected, money.IsZero);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(10, true)]
    [InlineData(-5, false)]
    public void IsPositive_ShouldReturnExpectedResult(decimal amount, bool expected)
    {
        var money = new Money(amount, _usd);
        Assert.Equal(expected, money.IsPositive);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(10, false)]
    [InlineData(-5, true)]
    public void IsNegative_ShouldReturnExpectedResult(decimal amount, bool expected)
    {
        var money = new Money(amount, _usd);
        Assert.Equal(expected, money.IsNegative);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedValueWithCurrency()
    {
        var money = new Money(99.99m, _usd);
        Assert.Equal("99.99 USD", money.ToString());
    }
}