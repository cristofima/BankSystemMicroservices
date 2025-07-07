using FluentAssertions;
using Security.Application.Dtos;

namespace Security.Application.UnitTests.Dtos;

public class RegisterDtoTests
{
    [Fact]
    public void RegisterDto_DefaultConstructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var dto = new RegisterDto();

        // Assert
        dto.Email.Should().BeEmpty();
        dto.Password.Should().BeEmpty();
        dto.ConfirmPassword.Should().BeEmpty();
        dto.FirstName.Should().BeEmpty();
        dto.LastName.Should().BeEmpty();
    }

    [Fact]
    public void RegisterDto_SetAllProperties_ShouldReturnCorrectValues()
    {
        // Arrange
        const string email = "john.doe@example.com";
        const string password = "SecurePassword123!";
        const string confirmPassword = "SecurePassword123!";
        const string firstName = "John";
        const string lastName = "Doe";

        // Act
        var dto = new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = confirmPassword,
            FirstName = firstName,
            LastName = lastName
        };

        // Assert
        dto.Email.Should().Be(email);
        dto.Password.Should().Be(password);
        dto.ConfirmPassword.Should().Be(confirmPassword);
        dto.FirstName.Should().Be(firstName);
        dto.LastName.Should().Be(lastName);
    }
}
