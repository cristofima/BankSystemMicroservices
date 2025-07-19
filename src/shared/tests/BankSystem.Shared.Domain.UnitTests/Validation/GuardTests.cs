using BankSystem.Shared.Domain.Validation;

namespace BankSystem.Shared.Domain.UnitTests.Validation;

public class GuardTests
{
    [Fact]
    public void AgainstNull_ShouldThrow_WhenNull()
    {
        string? value = null;
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(value, nameof(value)));
    }

    [Fact]
    public void AgainstNull_ShouldNotThrow_WhenNotNull()
    {
        Guard.AgainstNull("test", "value");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstNullOrEmpty_String_ShouldThrow_WhenInvalid(string? input)
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(input!, "value"));
    }

    [Fact]
    public void AgainstNullOrEmpty_String_ShouldNotThrow_WhenValid()
    {
        Guard.AgainstNullOrEmpty("value", "value");
    }

    [Fact]
    public void AgainstNegative_ShouldThrow_WhenNegative()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNegative(-1, "value"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AgainstNegative_ShouldNotThrow_WhenZeroOrPositive(decimal input)
    {
        Guard.AgainstNegative(input, "value");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void AgainstZeroOrNegative_Decimal_ShouldThrow(decimal input)
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstZeroOrNegative(input, "value"));
    }

    [Fact]
    public void AgainstZeroOrNegative_Decimal_ShouldNotThrow_WhenPositive()
    {
        Guard.AgainstZeroOrNegative(1, "value");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void AgainstZeroOrNegative_Int_ShouldThrow(int input)
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstZeroOrNegative(input, "value"));
    }

    [Fact]
    public void AgainstZeroOrNegative_Int_ShouldNotThrow_WhenPositive()
    {
        Guard.AgainstZeroOrNegative(1, "value");
    }

    [Fact]
    public void AgainstInvalidRange_ShouldThrow_WhenOutOfRange()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidRange(5, 10, 20, "value"));
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidRange(25, 10, 20, "value"));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public void AgainstInvalidRange_ShouldNotThrow_WhenInRange(decimal input)
    {
        Guard.AgainstInvalidRange(input, 10, 20, "value");
    }

    [Fact]
    public void AgainstEmptyGuid_ShouldThrow_WhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstEmptyGuid(Guid.Empty, "value"));
    }

    [Fact]
    public void AgainstEmptyGuid_ShouldNotThrow_WhenValid()
    {
        Guard.AgainstEmptyGuid(Guid.NewGuid(), "value");
    }

    [Fact]
    public void AgainstNullOrEmpty_Collection_ShouldThrow_WhenNull()
    {
        List<int>? collection = null;
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(collection!, "collection"));
    }

    [Fact]
    public void AgainstNullOrEmpty_Collection_ShouldThrow_WhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(new List<int>(), "collection"));
    }

    [Fact]
    public void AgainstNullOrEmpty_Collection_ShouldNotThrow_WhenHasItems()
    {
        Guard.AgainstNullOrEmpty(new List<int> { 1 }, "collection");
    }

    private enum TestEnum
    { Value1, Value2 }

    [Fact]
    public void AgainstInvalidEnum_ShouldThrow_WhenInvalidValue()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidEnum((TestEnum)999, "value"));
    }

    [Fact]
    public void AgainstInvalidEnum_ShouldNotThrow_WhenValidValue()
    {
        Guard.AgainstInvalidEnum(TestEnum.Value1, "value");
    }

    [Fact]
    public void AgainstExcessiveLength_ShouldThrow_WhenTooLong()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstExcessiveLength("123456", 5, "value"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    public void AgainstExcessiveLength_ShouldNotThrow_WhenValid(string? input)
    {
        Guard.AgainstExcessiveLength(input!, 5, "value");
    }

    [Fact]
    public void Against_WithCustomException_ShouldThrow_WhenNull()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Guard.Against<string, InvalidOperationException>(null, () => new InvalidOperationException()));
    }

    [Fact]
    public void Against_WithCustomException_ShouldNotThrow_WhenNotNull()
    {
        Guard.Against<string, InvalidOperationException>("value", () => new InvalidOperationException());
    }

    [Fact]
    public void AgainstCondition_ShouldThrow_WhenTrue()
    {
        Assert.Throws<ArgumentException>(() => Guard.Against(true, "Condition is true"));
    }

    [Fact]
    public void AgainstCondition_ShouldNotThrow_WhenFalse()
    {
        Guard.Against(false, "Condition is false");
    }

    [Fact]
    public void AgainstCondition_WithCustomException_ShouldThrow_WhenTrue()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Guard.Against<InvalidOperationException>(true, () => new InvalidOperationException()));
    }

    [Fact]
    public void AgainstCondition_WithCustomException_ShouldNotThrow_WhenFalse()
    {
        Guard.Against<InvalidOperationException>(false, () => new InvalidOperationException());
    }
}