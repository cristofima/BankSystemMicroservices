using FluentAssertions;
using Security.Application.Dtos;

namespace Security.Application.UnitTests.Dtos;

public class ResetPasswordDtoTests
{
    [Fact]
    public void ResetPasswordDto_DefaultConstructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var dto = new ResetPasswordDto();

        // Assert
        dto.Token.Should().BeEmpty();
        dto.Email.Should().BeEmpty();
        dto.NewPassword.Should().BeEmpty();
    }

    [Fact]
    public void ResetPasswordDto_SetAllProperties_ShouldReturnCorrectValues()
    {
        // Arrange
        const string token = "reset-token-123456";
        const string email = "user@example.com";
        const string newPassword = "NewSecurePassword123!";

        // Act
        var dto = new ResetPasswordDto
        {
            Token = token,
            Email = email,
            NewPassword = newPassword
        };

        // Assert
        dto.Token.Should().Be(token);
        dto.Email.Should().Be(email);
        dto.NewPassword.Should().Be(newPassword);
    }
}
