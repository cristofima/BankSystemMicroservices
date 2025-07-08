using FluentAssertions;
using Security.Domain.Entities;

namespace Security.Domain.UnitTests.Entities;

public class RefreshTokenTests
{
    private static ApplicationUser CreateTestUser()
    {
        return new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser@example.com",
            Email = "testuser@example.com"
        };
    }

    private static RefreshToken CreateRefreshToken(DateTime expiryDate)
    {
        var user = CreateTestUser();

        return new RefreshToken
        {
            Token = "test-token",
            UserId = user.Id,
            User = user,
            ExpiryDate = expiryDate
        };
    }

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var expiryDate = DateTime.UtcNow.AddDays(7);

        // Act
        var refreshToken = CreateRefreshToken(expiryDate);

        // Assert
        refreshToken.Token.Should().Be("test-token");
        refreshToken.JwtId.Should().NotBeNull();
        refreshToken.UserId.Should().Be("test-user-id");
        refreshToken.ExpiryDate.Should().Be(expiryDate);
        refreshToken.IsRevoked.Should().BeFalse();
        refreshToken.ReplacedByToken.Should().BeNull();
    }

    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = CreateRefreshToken(DateTime.UtcNow.AddDays(1));

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenRevoked_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = CreateRefreshToken(DateTime.UtcNow.AddDays(1));

        refreshToken.Revoke();

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = CreateRefreshToken(DateTime.UtcNow.AddDays(-1));

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenPastExpiryDate_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = CreateRefreshToken(DateTime.UtcNow.AddDays(-1));

        // Act
        var isExpired = refreshToken.IsExpired;

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void Revoke_ShouldSetIsRevokedToTrue()
    {
        // Arrange
        var refreshToken = CreateRefreshToken(DateTime.UtcNow.AddDays(2));

        // Act
        refreshToken.Revoke();

        // Assert
        refreshToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void ReplaceWith_ShouldSetReplacedByTokenAndRevoke()
    {
        // Arrange
        var originalToken = CreateRefreshToken(DateTime.UtcNow.AddDays(1));

        const string newTokenValue = "new-token";

        // Act
        originalToken.ReplaceWith(newTokenValue);

        // Assert
        originalToken.ReplacedByToken.Should().Be(newTokenValue);
        originalToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Properties_ShouldReturnCorrectValues()
    {
        // Arrange
        const string replacedByToken = "replacement-token";

        var refreshToken = CreateRefreshToken(DateTime.UtcNow.AddDays(7));
        refreshToken.ReplaceWith(replacedByToken);

        // Act & Assert
        refreshToken.Token.Should().Be("test-token");
        refreshToken.JwtId.Should().NotBeNull();
        refreshToken.UserId.Should().Be("test-user-id");
        refreshToken.ExpiryDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(1));
        refreshToken.ReplacedByToken.Should().Be(replacedByToken);
        refreshToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ComplexScenario_WhenRevokedAndExpired_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = CreateRefreshToken(DateTime.UtcNow.AddDays(-1));

        refreshToken.Revoke(); // Also revoked

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }
}