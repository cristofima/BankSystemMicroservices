using FluentAssertions;
using Security.Application.Services;
using Security.Application.UnitTests.Common;

namespace Security.Application.UnitTests.Services;

/// <summary>
/// Unit tests for GrpcValidationService
/// </summary>
public class GrpcValidationServiceTests : TestBase
{
    private readonly GrpcValidationService _service;

    public GrpcValidationServiceTests()
    {
        _service = new GrpcValidationService();
    }

    #region ValidateAndParseCustomerId Tests

    /// <summary>
    /// Verifies ValidateAndParseCustomerId returns success for valid GUID
    /// </summary>
    [Fact]
    public void ValidateAndParseCustomerId_ShouldReturnSuccess_WhenCustomerIdIsValidGuid()
    {
        // Arrange
        var validGuid = Guid.NewGuid();
        var customerId = validGuid.ToString();

        // Act
        var result = _service.ValidateAndParseCustomerId(customerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(validGuid);
        result.Error.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies ValidateAndParseCustomerId returns success for valid GUID with different formats
    /// </summary>
    [Theory]
    [InlineData("D")] // 32 digits separated by hyphens: 00000000-0000-0000-0000-000000000000
    [InlineData("N")] // 32 digits: 00000000000000000000000000000000
    [InlineData("B")] // 32 digits separated by hyphens, enclosed in braces: {00000000-0000-0000-0000-000000000000}
    [InlineData("P")] // 32 digits separated by hyphens, enclosed in parentheses: (00000000-0000-0000-0000-000000000000)
    [InlineData("X")] // Four hexadecimal values enclosed in braces: {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}
    public void ValidateAndParseCustomerId_ShouldReturnSuccess_ForDifferentGuidFormats(
        string format
    )
    {
        // Arrange
        var validGuid = Guid.NewGuid();
        var customerId = validGuid.ToString(format);

        // Act
        var result = _service.ValidateAndParseCustomerId(customerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(validGuid);
        result.Error.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies ValidateAndParseCustomerId returns failure for null customer ID
    /// </summary>
    [Fact]
    public void ValidateAndParseCustomerId_ShouldReturnFailure_WhenCustomerIdIsNull()
    {
        // Arrange
        string customerId = null!;

        // Act
        var result = _service.ValidateAndParseCustomerId(customerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(Guid.Empty);
        result.Error.Should().Be("Customer ID cannot be null or empty");
    }

    /// <summary>
    /// Verifies ValidateAndParseCustomerId returns failure for empty customer ID
    /// </summary>
    [Fact]
    public void ValidateAndParseCustomerId_ShouldReturnFailure_WhenCustomerIdIsEmpty()
    {
        // Arrange
        var customerId = string.Empty;

        // Act
        var result = _service.ValidateAndParseCustomerId(customerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(Guid.Empty);
        result.Error.Should().Be("Customer ID cannot be null or empty");
    }

    /// <summary>
    /// Verifies ValidateAndParseCustomerId returns failure for whitespace customer ID
    /// </summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("   ")]
    public void ValidateAndParseCustomerId_ShouldReturnFailure_WhenCustomerIdIsWhitespace(
        string customerId
    )
    {
        // Act
        var result = _service.ValidateAndParseCustomerId(customerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(Guid.Empty);
        result.Error.Should().Be("Customer ID cannot be null or empty");
    }

    /// <summary>
    /// Verifies ValidateAndParseCustomerId returns failure for invalid GUID format
    /// </summary>
    [Theory]
    [InlineData("invalid-guid")]
    [InlineData("123456789")]
    [InlineData("not-a-guid-at-all")]
    [InlineData("12345678-1234-1234-1234-12345678901X")] // Invalid character
    [InlineData("12345678-1234-1234-1234-1234567890123")] // Too long
    [InlineData("12345678-1234-1234-1234-123456")] // Too short
    public void ValidateAndParseCustomerId_ShouldReturnFailure_WhenCustomerIdIsInvalidFormat(
        string customerId
    )
    {
        // Act
        var result = _service.ValidateAndParseCustomerId(customerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(Guid.Empty);
        result.Error.Should().Be($"Invalid Customer ID format: {customerId}");
    }

    /// <summary>
    /// Verifies ValidateAndParseCustomerId returns failure for empty GUID
    /// </summary>
    [Fact]
    public void ValidateAndParseCustomerId_ShouldReturnFailure_WhenCustomerIdIsEmptyGuid()
    {
        // Arrange
        var customerId = Guid.Empty.ToString();

        // Act
        var result = _service.ValidateAndParseCustomerId(customerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(Guid.Empty);
        result.Error.Should().Be("Customer ID cannot be empty GUID");
    }

    #endregion ValidateAndParseCustomerId Tests

    #region ValidateCustomerIds Tests

    /// <summary>
    /// Verifies ValidateCustomerIds returns success for valid list of customer IDs
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldReturnSuccess_WhenAllCustomerIdsAreValid()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var guid3 = Guid.NewGuid();
        var customerIds = new List<string> { guid1.ToString(), guid2.ToString(), guid3.ToString() };

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(guid1);
        result.Value.Should().Contain(guid2);
        result.Value.Should().Contain(guid3);
        result.Error.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies ValidateCustomerIds returns success for single valid customer ID
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldReturnSuccess_ForSingleValidCustomerId()
    {
        // Arrange
        var validGuid = Guid.NewGuid();
        var customerIds = new List<string> { validGuid.ToString() };

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().Contain(validGuid);
        result.Error.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies ValidateCustomerIds returns failure for empty collection
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldReturnFailure_WhenCollectionIsEmpty()
    {
        // Arrange
        var customerIds = new List<string>();

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Customer IDs collection cannot be empty");
    }

    /// <summary>
    /// Verifies ValidateCustomerIds returns failure when collection exceeds maximum limit
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldReturnFailure_WhenCollectionExceedsMaximumLimit()
    {
        // Arrange - Create 101 valid GUIDs to exceed the 100 limit
        var customerIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid().ToString()).ToList();

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Too many customer IDs requested. Maximum allowed: 100");
    }

    /// <summary>
    /// Verifies ValidateCustomerIds returns success when collection is exactly at maximum limit
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldReturnSuccess_WhenCollectionIsExactlyAtMaximumLimit()
    {
        // Arrange - Create exactly 100 valid GUIDs
        var guids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var customerIds = guids.Select(g => g.ToString()).ToList();

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(100);
        result.Value.Should().BeEquivalentTo(guids);
    }

    /// <summary>
    /// Verifies ValidateCustomerIds returns failure when some customer IDs are invalid
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldReturnFailure_WhenSomeCustomerIdsAreInvalid()
    {
        // Arrange
        var validGuid = Guid.NewGuid();
        var customerIds = new List<string>
        {
            validGuid.ToString(),
            "invalid-guid",
            Guid.NewGuid().ToString(),
        };

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid Customer ID format: invalid-guid");
    }

    /// <summary>
    /// Verifies ValidateCustomerIds returns failure when first customer ID is invalid
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldReturnFailure_WhenFirstCustomerIdIsInvalid()
    {
        // Arrange
        var customerIds = new List<string>
        {
            "invalid-guid",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
        };

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid Customer ID format: invalid-guid");
    }

    /// <summary>
    /// Verifies ValidateCustomerIds returns failure when customer ID is empty GUID
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldReturnFailure_WhenCustomerIdIsEmptyGuid()
    {
        // Arrange
        var customerIds = new List<string>
        {
            Guid.NewGuid().ToString(),
            Guid.Empty.ToString(), // Empty GUID should fail
            Guid.NewGuid().ToString(),
        };

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Customer ID cannot be empty GUID");
    }

    /// <summary>
    /// Verifies ValidateCustomerIds preserves order of valid IDs
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldPreserveOrder_OfValidIds()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var guid3 = Guid.NewGuid();
        var customerIds = new List<string> { guid1.ToString(), guid2.ToString(), guid3.ToString() };

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].Should().Be(guid1);
        result.Value[1].Should().Be(guid2);
        result.Value[2].Should().Be(guid3);
    }

    /// <summary>
    /// Verifies ValidateCustomerIds handles different GUID formats in collection
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldHandleDifferentGuidFormats_InCollection()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var guid3 = Guid.NewGuid();
        var customerIds = new List<string>
        {
            guid1.ToString("D"), // Standard format
            guid2.ToString("N"), // No hyphens
            guid3.ToString(
                "B"
            ) // Braces format
            ,
        };

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(guid1);
        result.Value.Should().Contain(guid2);
        result.Value.Should().Contain(guid3);
    }

    /// <summary>
    /// Verifies ValidateCustomerIds handles whitespace-only strings correctly
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldReturnFailure_WhenContainsWhitespaceOnlyString()
    {
        // Arrange
        var validGuid = Guid.NewGuid();
        var customerIds = new List<string>
        {
            validGuid.ToString(),
            "   ", // Whitespace only
            Guid.NewGuid().ToString(),
        };

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Customer ID cannot be null or empty");
    }

    /// <summary>
    /// Verifies ValidateCustomerIds handles duplicate GUIDs correctly
    /// </summary>
    [Fact]
    public void ValidateCustomerIds_ShouldAllowDuplicateGuids()
    {
        // Arrange
        var duplicateGuid = Guid.NewGuid();
        var uniqueGuid = Guid.NewGuid();
        var customerIds = new List<string>
        {
            duplicateGuid.ToString(),
            uniqueGuid.ToString(),
            duplicateGuid.ToString() // Same GUID again
            ,
        };

        // Act
        var result = _service.ValidateCustomerIds(customerIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3); // All three are included, duplicates allowed
        result.Value.Should().Contain(duplicateGuid);
        result.Value.Should().Contain(uniqueGuid);
        result.Value.Where(id => id == duplicateGuid).Should().HaveCount(2);
    }

    #endregion ValidateCustomerIds Tests
}
