using BankSystem.Shared.Domain.Validation;

namespace BankSystem.Shared.Domain.UnitTests.Validation;

public class GuardTests
{
    [Fact]
    public void AgainstNull_ShouldThrow_WhenNull()
    {
        string? value = null;
        var ex = Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(value, nameof(value)));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void AgainstNull_ShouldNotThrow_WhenNotNull()
    {
        Guard.AgainstNull("test", "value");
        Assert.True(true);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstNullOrEmpty_String_ShouldThrow_WhenInvalid(string? input)
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(input!, "value"));
        Assert.Equal("value", ex.ParamName);
        Assert.StartsWith("Value cannot be null or empty", ex.Message);
    }

    [Fact]
    public void AgainstNullOrEmpty_String_ShouldNotThrow_WhenValid()
    {
        Guard.AgainstNullOrEmpty("value", "value");
        Assert.True(true);
    }

    [Fact]
    public void AgainstNegative_ShouldThrow_WhenNegative()
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstNegative(-1, "value"));
        Assert.Equal("value", ex.ParamName);
        Assert.Contains("-1", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AgainstNegative_ShouldNotThrow_WhenZeroOrPositive(decimal input)
    {
        Guard.AgainstNegative(input, "value");
        Assert.True(true);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void AgainstZeroOrNegative_Decimal_ShouldThrow(decimal input)
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstZeroOrNegative(input, "value"));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void AgainstZeroOrNegative_Decimal_ShouldNotThrow_WhenPositive()
    {
        Guard.AgainstZeroOrNegative(1, "value");
        Assert.True(true);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void AgainstZeroOrNegative_Int_ShouldThrow(int input)
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstZeroOrNegative(input, "value"));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void AgainstZeroOrNegative_Int_ShouldNotThrow_WhenPositive()
    {
        Guard.AgainstZeroOrNegative(1, "value");
        Assert.True(true);
    }

    [Fact]
    public void AgainstInvalidRange_ShouldThrow_WhenOutOfRange()
    {
        var ex1 = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidRange(5, 10, 20, "value"));
        Assert.Contains("[10, 20]", ex1.Message);

        var ex2 = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidRange(25, 10, 20, "value"));
        Assert.Contains("[10, 20]", ex2.Message);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public void AgainstInvalidRange_ShouldNotThrow_WhenInRange(decimal input)
    {
        Guard.AgainstInvalidRange(input, 10, 20, "value");
        Assert.True(true);
    }

    [Fact]
    public void AgainstEmptyGuid_ShouldThrow_WhenEmpty()
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstEmptyGuid(Guid.Empty, "value"));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void AgainstEmptyGuid_ShouldNotThrow_WhenValid()
    {
        Guard.AgainstEmptyGuid(Guid.NewGuid(), "value");
        Assert.True(true);
    }

    [Fact]
    public void AgainstNullOrEmpty_Collection_ShouldThrow_WhenNull()
    {
        List<int>? collection = null;
        var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(collection!, "collection"));
        Assert.Equal("collection", ex.ParamName);
    }

    [Fact]
    public void AgainstNullOrEmpty_Collection_ShouldThrow_WhenEmpty()
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(new List<int>(), "collection"));
        Assert.Equal("collection", ex.ParamName);
    }

    [Fact]
    public void AgainstNullOrEmpty_Collection_ShouldNotThrow_WhenHasItems()
    {
        Guard.AgainstNullOrEmpty(new List<int> { 1 }, "collection");
        Assert.True(true);
    }

    private enum TestEnum
    {
        Value1,
        Value2
    }

    [Fact]
    public void AgainstInvalidEnum_ShouldThrow_WhenInvalidValue()
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidEnum((TestEnum)999, "value"));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void AgainstInvalidEnum_ShouldNotThrow_WhenValidValue()
    {
        Guard.AgainstInvalidEnum(TestEnum.Value1, "value");
        Assert.True(true);
    }

    [Fact]
    public void AgainstExcessiveLength_ShouldThrow_WhenTooLong()
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstExcessiveLength("123456", 5, "value"));
        Assert.Equal("value", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    public void AgainstExcessiveLength_ShouldNotThrow_WhenValid(string? input)
    {
        Guard.AgainstExcessiveLength(input!, 5, "value");
        Assert.True(true);
    }

    [Fact]
    public void Against_WithCustomException_ShouldThrow_WhenNull()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Guard.Against<string, InvalidOperationException>(null, () => new InvalidOperationException()));
        Assert.NotNull(ex);
    }

    [Fact]
    public void Against_WithCustomException_ShouldNotThrow_WhenNotNull()
    {
        Guard.Against<string, InvalidOperationException>("value", () => new InvalidOperationException());
        Assert.True(true);
    }

    [Fact]
    public void AgainstCondition_ShouldThrow_WhenTrue()
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.Against(true, "Condition is true"));
        Assert.Contains("Condition is true", ex.Message);
    }

    [Fact]
    public void AgainstCondition_ShouldNotThrow_WhenFalse()
    {
        Guard.Against(false, "Condition is false");
        Assert.True(true);
    }

    [Fact]
    public void AgainstCondition_WithCustomException_ShouldThrow_WhenTrue()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Guard.Against(true, () => new InvalidOperationException()));
        Assert.NotNull(ex);
    }

    [Fact]
    public void AgainstCondition_WithCustomException_ShouldNotThrow_WhenFalse()
    {
        Guard.Against(false, () => new InvalidOperationException());
        Assert.True(true);
    }
}