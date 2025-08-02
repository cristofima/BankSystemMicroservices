using Security.Application.Dtos;

namespace Security.Application.UnitTests.Dtos;

public class LoginDtoTests
{
    [Fact]
    public void Constructor_WithValidUserNameAndPassword_ShouldSetProperties()
    {
        // Arrange
        const string userName = "test-user";
        const string password = "Password123!";

        // Act
        var dto = new LoginDto
        {
            UserName = userName,
            Password = password
        };

        // Assert
        Assert.Equal(userName, dto.UserName);
        Assert.Equal(password, dto.Password);
    }

    [Fact]
    public void DefaultConstructor_ShouldCreateInstanceWithNullProperties()
    {
        // Act
        var dto = new LoginDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Empty(dto.UserName);
        Assert.Empty(dto.Password);
    }
}
