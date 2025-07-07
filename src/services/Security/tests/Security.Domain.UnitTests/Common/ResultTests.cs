using FluentAssertions;
using Security.Domain.Common;

namespace Security.Domain.UnitTests.Common;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Failure_WithErrorMessage_ShouldCreateFailedResult()
    {
        // Arrange
        const string errorMessage = "Something went wrong";

        // Act
        var result = Result.Failure(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public void Failure_WithNullError_ShouldCreateFailedResultWithNullError()
    {
        // Act
        var result = Result.Failure(null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_WithEmptyError_ShouldCreateFailedResultWithEmptyError()
    {
        // Act
        var result = Result.Failure(string.Empty);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(string.Empty);
    }

    [Fact]
    public void ImplicitOperator_FromString_ShouldCreateFailureResult()
    {
        // Arrange
        const string errorMessage = "Validation failed";

        // Act
        Result result = errorMessage;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public void ImplicitOperator_FromNullString_ShouldCreateFailureResult()
    {
        // Act
        Result result = (string)null;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeNull();
    }
}

public class ResultOfTTests
{
    [Fact]
    public void Success_WithValue_ShouldCreateSuccessfulResult()
    {
        // Arrange
        const string value = "test value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(value);
        result.Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Success_WithNullValue_ShouldCreateSuccessfulResultWithNullValue()
    {
        // Act
        var result = Result<string>.Success(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Failure_WithErrorMessage_ShouldCreateFailedResult()
    {
        // Arrange
        const string errorMessage = "Operation failed";

        // Act
        var result = Result<int>.Failure(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(0);
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public void Failure_WithNullError_ShouldCreateFailedResultWithNullError()
    {
        // Act
        var result = Result<string>.Failure(null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ImplicitOperator_FromValue_ShouldCreateSuccessResult()
    {
        // Arrange
        const int value = 42;

        // Act
        Result<int> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(value);
        result.Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ImplicitOperator_FromString_ShouldCreateFailureResult()
    {
        // Arrange
        const string errorMessage = "Invalid operation";

        // Act
        Result<int> result = errorMessage;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(0);
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public void Constructor_WithComplexObject_ShouldWork()
    {
        // Arrange
        var complexObject = new { Name = "Test", Value = 123 };

        // Act
        var result = Result<object>.Success(complexObject);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(complexObject);
    }
}
