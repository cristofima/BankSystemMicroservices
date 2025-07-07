using Security.Application.Dtos;

namespace Security.Application.UnitTests.Dtos;

public class UserDtoTests
{
    [Fact]
    public void UserResponse_Constructor_ShouldSetAllProperties()
    {
        // Arrange
        const string id = "user123";
        const string userName = "testuser@example.com";
        const string email = "testuser@example.com";
        const string firstName = "John";
        const string lastName = "Doe";
        const bool isEmailConfirmed = true;
        const bool isActive = true;
        var createdAt = DateTime.UtcNow;

        // Act
        var response = new UserResponse(
            id, 
            userName, 
            email, 
            firstName, 
            lastName, 
            isEmailConfirmed, 
            isActive, 
            createdAt);

        // Assert
        Assert.Equal(id, response.Id);
        Assert.Equal(userName, response.UserName);
        Assert.Equal(email, response.Email);
        Assert.Equal(firstName, response.FirstName);
        Assert.Equal(lastName, response.LastName);
        Assert.Equal(isEmailConfirmed, response.IsEmailConfirmed);
        Assert.Equal(isActive, response.IsActive);
        Assert.Equal(createdAt, response.CreatedAt);
    }

    [Fact]
    public void UserResponse_WithNullOptionalProperties_ShouldAllowNullValues()
    {
        // Arrange
        const string id = "user123";
        const string userName = "testuser@example.com";
        const string email = "testuser@example.com";
        string? firstName = null;
        string? lastName = null;
        const bool isEmailConfirmed = false;
        const bool isActive = false;
        var createdAt = DateTime.UtcNow;

        // Act
        var response = new UserResponse(
            id, 
            userName, 
            email, 
            firstName, 
            lastName, 
            isEmailConfirmed, 
            isActive, 
            createdAt);

        // Assert
        Assert.Equal(id, response.Id);
        Assert.Equal(userName, response.UserName);
        Assert.Equal(email, response.Email);
        Assert.Null(response.FirstName);
        Assert.Null(response.LastName);
        Assert.False(response.IsEmailConfirmed);
        Assert.False(response.IsActive);
        Assert.Equal(createdAt, response.CreatedAt);
    }

    [Fact]
    public void UserResponse_WithEmptyStrings_ShouldAcceptEmptyValues()
    {
        // Arrange
        const string id = "";
        const string userName = "";
        const string email = "";
        const string firstName = "";
        const string lastName = "";
        const bool isEmailConfirmed = true;
        const bool isActive = true;
        var createdAt = DateTime.MinValue;

        // Act
        var response = new UserResponse(
            id, 
            userName, 
            email, 
            firstName, 
            lastName, 
            isEmailConfirmed, 
            isActive, 
            createdAt);

        // Assert
        Assert.Equal(string.Empty, response.Id);
        Assert.Equal(string.Empty, response.UserName);
        Assert.Equal(string.Empty, response.Email);
        Assert.Equal(string.Empty, response.FirstName);
        Assert.Equal(string.Empty, response.LastName);
        Assert.True(response.IsEmailConfirmed);
        Assert.True(response.IsActive);
        Assert.Equal(DateTime.MinValue, response.CreatedAt);
    }

    [Fact]
    public void UserResponse_RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange
        const string id = "user123";
        const string userName = "testuser@example.com";
        const string email = "testuser@example.com";
        const string firstName = "John";
        const string lastName = "Doe";
        const bool isEmailConfirmed = true;
        const bool isActive = true;
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);

        var response1 = new UserResponse(
            id, userName, email, firstName, lastName, isEmailConfirmed, isActive, createdAt);
        var response2 = new UserResponse(
            id, userName, email, firstName, lastName, isEmailConfirmed, isActive, createdAt);

        // Act & Assert
        Assert.Equal(response1, response2);
        Assert.Equal(response1.GetHashCode(), response2.GetHashCode());
    }

    [Fact]
    public void UserResponse_ToString_ShouldContainAllProperties()
    {
        // Arrange
        var response = new UserResponse(
            "user123",
            "testuser@example.com", 
            "testuser@example.com",
            "John",
            "Doe",
            true,
            true,
            DateTime.UtcNow);

        // Act
        var result = response.ToString();

        // Assert
        Assert.Contains("user123", result);
        Assert.Contains("testuser@example.com", result);
        Assert.Contains("John", result);
        Assert.Contains("Doe", result);
        Assert.Contains("True", result);
    }
}
