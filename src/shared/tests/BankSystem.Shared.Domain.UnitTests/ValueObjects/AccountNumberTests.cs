using BankSystem.Shared.Domain.ValueObjects;
using BankSystem.Shared.Domain.Exceptions;

namespace BankSystem.Shared.Domain.UnitTests.ValueObjects;

public class AccountNumberTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNullOrWhiteSpace(string invalid)
    {
        Assert.Throws<DomainException>(() => new AccountNumber(invalid!));
    }

    [Theory]
    [InlineData("12345")] // too short
    [InlineData("12345678901")] // too long
    [InlineData("abcdefghij")] // non-numeric
    [InlineData("1234abc678")] // contains letters
    [InlineData("123456789 ")] // trailing space
    [InlineData(" 123456789")] // leading space
    public void Constructor_ShouldThrow_WhenInvalidFormat(string invalid)
    {
        Assert.Throws<DomainException>(() => new AccountNumber(invalid));
    }

    [Fact]
    public void Constructor_ShouldCreate_WhenValid()
    {
        const string valid = "1234567890";
        var accountNumber = new AccountNumber(valid);
        Assert.Equal(valid, accountNumber.Value);
    }

    [Theory]
    [InlineData("1234567890", true)]
    [InlineData("0123456789", true)]
    [InlineData(" 1234567890 ", true)] // trims whitespace
    [InlineData("123456789", false)] // too short
    [InlineData("abcdefghij", false)]
    [InlineData("12345abcde", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidFormat_ShouldReturnExpectedResult(string? value, bool expected)
    {
        Assert.Equal(expected, AccountNumber.IsValidFormat(value!));
    }

    [Fact]
    public void Generate_ShouldReturnValidAccountNumber()
    {
        var generated = AccountNumber.Generate();
        Assert.NotNull(generated);
        Assert.Equal(10, generated.Value.Length);
        Assert.True(generated.Value.All(char.IsDigit));
    }

    [Fact]
    public void GetMaskedValue_ShouldReturnMaskedMiddleAndLastFourDigits()
    {
        var accountNumber = new AccountNumber("1234567890");
        var masked = accountNumber.GetMaskedValue();
        Assert.Equal("******7890", masked);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var accountNumber = new AccountNumber("1234567890");
        Assert.Equal("1234567890", accountNumber.ToString());
    }

    [Fact]
    public void ImplicitOperator_ToString_ShouldReturnValue()
    {
        var accountNumber = new AccountNumber("1234567890");
        string value = accountNumber;
        Assert.Equal("1234567890", value);
    }

    [Fact]
    public void ExplicitOperator_FromString_ShouldCreateAccountNumber()
    {
        var accountNumber = (AccountNumber)"1234567890";
        Assert.Equal("1234567890", accountNumber.Value);
    }
}