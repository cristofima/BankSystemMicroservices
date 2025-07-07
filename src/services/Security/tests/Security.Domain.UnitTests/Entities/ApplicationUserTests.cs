using Security.Domain.Entities;
using FluentAssertions;

namespace Security.Domain.UnitTests.Entities;

public class ApplicationUserTests
{
    [Fact]
    public void RecordSuccessfulLogin_ShouldUpdateLastLoginAndResetFailedAttempts()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FailedLoginAttempts = 3,
            LastLoginAt = DateTime.UtcNow.AddDays(-1)
        };
        var beforeUpdate = DateTime.UtcNow;

        // Act
        user.RecordSuccessfulLogin();

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
        user.LastLoginAt.Should().BeAfter(beforeUpdate);
        user.UpdatedAt.Should().BeAfter(beforeUpdate);
    }

    [Fact]
    public void RecordFailedLogin_ShouldIncrementFailedAttemptsAndUpdateTimestamp()
    {
        // Arrange
        var user = new ApplicationUser { FailedLoginAttempts = 2 };
        var beforeUpdate = DateTime.UtcNow;

        // Act
        user.RecordFailedLogin();

        // Assert
        user.FailedLoginAttempts.Should().Be(3);
        user.UpdatedAt.Should().BeAfter(beforeUpdate);
    }

    [Theory]
    [InlineData(3, 5, 10, false)] // Below max attempts
    [InlineData(5, 5, 10, true)]  // At max attempts, within lockout
    [InlineData(6, 5, 10, true)]  // Above max attempts, within lockout
    [InlineData(5, 5, 20, false)] // At max attempts, outside lockout period
    public void IsLockedOut_ShouldReturnCorrectLockoutStatus(
        int failedAttempts,
        int maxAttempts,
        int minutesAgo,
        bool expectedResult)
    {
        // Arrange
        var user = new ApplicationUser
        {
            FailedLoginAttempts = failedAttempts,
            UpdatedAt = DateTime.UtcNow.AddMinutes(-minutesAgo)
        };
        var lockoutDuration = TimeSpan.FromMinutes(15);

        // Act
        var result = user.IsLockedOut(maxAttempts, lockoutDuration);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void IsLockedOut_WithNullUpdatedAt_ShouldReturnFalse()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FailedLoginAttempts = 10,
            UpdatedAt = null
        };

        // Act
        var result = user.IsLockedOut(5, TimeSpan.FromMinutes(15));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ResetLockout_ShouldResetFailedAttemptsAndUpdateTimestamp()
    {
        // Arrange
        var user = new ApplicationUser { FailedLoginAttempts = 5 };
        var beforeReset = DateTime.UtcNow;

        // Act
        user.ResetLockout();

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
        user.UpdatedAt.Should().BeAfter(beforeReset);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.RefreshTokens.Should().NotBeNull().And.BeEmpty();
        user.FailedLoginAttempts.Should().Be(0);
    }
}
