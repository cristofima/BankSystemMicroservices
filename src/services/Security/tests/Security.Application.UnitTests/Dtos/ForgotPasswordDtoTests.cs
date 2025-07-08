using Security.Application.Dtos;

namespace Security.Application.UnitTests.Dtos;

public class ForgotPasswordDtoTests
{
    [Fact]
    public void Constructor_WithValidEmail_ShouldSetEmailProperty()
    {
        // Arrange
        const string email = "test@example.com";

        // Act
        var dto = new ForgotPasswordDto { Email = email };

        // Assert
        Assert.Equal(email, dto.Email);
    }

    [Fact]
    public void Constructor_WithEmptyEmail_ShouldSetEmailToEmpty()
    {
        // Arrange
        const string email = "";

        // Act
        var dto = new ForgotPasswordDto { Email = email };

        // Assert
        Assert.Equal(email, dto.Email);
    }

    [Fact]
    public void DefaultConstructor_ShouldCreateInstanceWithNullEmail()
    {
        // Act
        var dto = new ForgotPasswordDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Empty(dto.Email);
    }
}
