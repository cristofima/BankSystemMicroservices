using Security.Application.Interfaces;
using Security.Infrastructure.IntegrationTests.Common;

namespace Security.Infrastructure.IntegrationTests.Simplified;

/// <summary>
/// Basic integration tests to verify core functionality
/// </summary>
[TestFixture]
public class BasicIntegrationTests : SecurityTestBase
{
    [Test]
    public async Task DbContext_CanConnectToDatabase()
    {
        // Arrange & Act
        var canConnect = await DbContext.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
    }

    [Test]
    public async Task CreateTestUser_ShouldSucceed()
    {
        // Arrange & Act
        var user = await CreateTestUserAsync();

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeNullOrEmpty();
        user.Email.Should().NotBeNullOrEmpty();
        user.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task CreateTestRefreshToken_ShouldSucceed()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        var token = await CreateTestRefreshTokenAsync(user.Id);

        // Assert
        token.Should().NotBeNull();
        token.UserId.Should().Be(user.Id);
        token.IsRevoked.Should().BeFalse();
        token.ExpiryDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Test]
    public async Task Database_CanQueryUsers()
    {
        // Arrange
        await CreateTestUserAsync();
        await CreateTestUserAsync();

        // Act
        var userCount = await DbContext.Users.CountAsync();

        // Assert
        userCount.Should().BeGreaterOrEqualTo(2);
    }

    [Test]
    public async Task Database_CanQueryRefreshTokens()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await CreateTestRefreshTokenAsync(user.Id);
        await CreateTestRefreshTokenAsync(user.Id);

        // Act
        var tokenCount = await DbContext.RefreshTokens.CountAsync();

        // Assert
        tokenCount.Should().BeGreaterOrEqualTo(2);
    }

    [Test]
    public void ServiceProvider_CanResolveRefreshTokenService()
    {
        // Act
        var service = ServiceProvider.GetService<IRefreshTokenService>();

        // Assert
        service.Should().NotBeNull();
    }

    [Test]
    public void ServiceProvider_CanResolveSecurityAuditService()
    {
        // Act
        var service = ServiceProvider.GetService<ISecurityAuditService>();

        // Assert
        service.Should().NotBeNull();
    }

    [Test]
    public void ServiceProvider_CanResolveTokenService()
    {
        // Act
        var service = ServiceProvider.GetService<ITokenService>();

        // Assert
        service.Should().NotBeNull();
    }
}
