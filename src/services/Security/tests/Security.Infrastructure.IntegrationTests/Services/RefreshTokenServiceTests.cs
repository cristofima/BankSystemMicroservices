using Security.Infrastructure.Data;
using Security.Infrastructure.IntegrationTests.Infrastructure;
using Security.Application.Interfaces;

namespace Security.Infrastructure.IntegrationTests.Services;

public class RefreshTokenServiceTests : BaseSecurityInfrastructureTest
{
    private IRefreshTokenService GetRefreshTokenService()
    {
        return GetService<IRefreshTokenService>();
    }

    private new SecurityDbContext GetDbContext()
    {
        return GetService<SecurityDbContext>();
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_ValidUser_ShouldCreateToken()
    {
        // Arrange
        var refreshTokenService = GetRefreshTokenService();
        var context = GetDbContext();
        var user = await CreateTestUserAsync();
        var jwtId = Guid.NewGuid().ToString();

        // Act
        var result = await refreshTokenService.CreateRefreshTokenAsync(user.Id, jwtId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(jwtId, result.JwtId);
        Assert.False(result.IsRevoked);
        Assert.True(result.ExpiryDate > DateTime.UtcNow);

        // Verify token was persisted to database
        var persistedToken = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == result.Token);
        
        Assert.NotNull(persistedToken);
        Assert.Equal(user.Id, persistedToken.UserId);
        Assert.Equal(jwtId, persistedToken.JwtId);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var jwtId = Guid.NewGuid().ToString();
        var refreshTokenService = GetRefreshTokenService();
        var createResult = await refreshTokenService.CreateRefreshTokenAsync(user.Id, jwtId);
        var token = createResult!.Token;

        // Act
        var result = await refreshTokenService.ValidateRefreshTokenAsync(token, jwtId, user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(jwtId, result.JwtId);
        Assert.False(result.IsRevoked);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_NonExistentToken_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentToken = Guid.NewGuid().ToString();
        var refreshTokenService = GetRefreshTokenService();

        // Act
        var result = await refreshTokenService.ValidateRefreshTokenAsync(nonExistentToken, string.Empty, string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_RevokedToken_ShouldReturnFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var jwtId = Guid.NewGuid().ToString();
        var refreshTokenService = GetService<IRefreshTokenService>();
        var createdToken = await refreshTokenService.CreateRefreshTokenAsync(user.Id, jwtId);
        Assert.NotNull(createdToken);
        var token = createdToken.Token;

        // Revoke the token
        await refreshTokenService.RevokeTokenAsync(token);

        // Act
        var result = await refreshTokenService.ValidateRefreshTokenAsync(token, jwtId, user.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ExpiredToken_ShouldReturnFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var jwtId = Guid.NewGuid().ToString();

        // Create an expired token directly in database
        var expiredToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            JwtId = jwtId,
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            IsRevoked = false
        };

        var context = GetDbContext();
        context.RefreshTokens.Add(expiredToken);
        await context.SaveChangesAsync();

        // Act
        var refreshTokenService = GetService<IRefreshTokenService>();
        var result = await refreshTokenService.ValidateRefreshTokenAsync(expiredToken.Token, jwtId, user.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ShouldCreateNewTokenAndRevokeOld()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var oldJwtId = Guid.NewGuid().ToString();
        var newJwtId = Guid.NewGuid().ToString();
        
        var refreshTokenService = GetService<IRefreshTokenService>();
        var oldRefreshToken = await refreshTokenService.CreateRefreshTokenAsync(user.Id, oldJwtId);
        Assert.NotNull(oldRefreshToken);

        // Act
        var newToken = await refreshTokenService.RefreshTokenAsync(oldRefreshToken, newJwtId);

        // Assert
        Assert.NotNull(newToken);
        Assert.Equal(user.Id, newToken.UserId);
        Assert.Equal(newJwtId, newToken.JwtId);
        Assert.False(newToken.IsRevoked);
        Assert.NotEqual(oldRefreshToken.Token, newToken.Token);

        // Verify old token is revoked
        var context = GetDbContext();
        var oldTokenFromDb = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == oldRefreshToken.Token);
        
        Assert.NotNull(oldTokenFromDb);
        Assert.True(oldTokenFromDb.IsRevoked);

        // Verify new token exists and is valid
        var newTokenFromDb = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == newToken.Token);
        
        Assert.NotNull(newTokenFromDb);
        Assert.False(newTokenFromDb.IsRevoked);
        Assert.Equal(newJwtId, newTokenFromDb.JwtId);
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var refreshTokenService = GetService<IRefreshTokenService>();
        var newJwtId = Guid.NewGuid().ToString();
        var oldToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            JwtId = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid().ToString(), // Non-existent user
            ExpiryDate = DateTime.UtcNow.AddDays(1),
            IsRevoked = false
        };

        // Act
        var result = await refreshTokenService.RefreshTokenAsync(oldToken, newJwtId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RevokeTokenAsync_ValidToken_ShouldRevokeToken()
    {
        // Arrange
        var refreshTokenService = GetService<IRefreshTokenService>();
        var user = await CreateTestUserAsync();
        var jwtId = Guid.NewGuid().ToString();
        var createResult = await refreshTokenService.CreateRefreshTokenAsync(user.Id, jwtId);
        var token = createResult!.Token;

        // Act
        var result = await refreshTokenService.RevokeTokenAsync(token);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify token is revoked in database
        var context = GetDbContext();
        var revokedToken = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
        
        Assert.NotNull(revokedToken);
        Assert.True(revokedToken.IsRevoked);
    }

    [Fact]
    public async Task RevokeTokenAsync_NonExistentToken_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentToken = Guid.NewGuid().ToString();

        // Act
        var refreshTokenService = GetService<IRefreshTokenService>();
        var result = await refreshTokenService.RevokeTokenAsync(nonExistentToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Token not found", result.Error);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_UserWithTokens_ShouldRevokeAllTokens()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var refreshTokenService = GetService<IRefreshTokenService>();
        
        // Create multiple tokens for the user
        var token1 = await refreshTokenService.CreateRefreshTokenAsync(user.Id, Guid.NewGuid().ToString());
        var token2 = await refreshTokenService.CreateRefreshTokenAsync(user.Id, Guid.NewGuid().ToString());
        var token3 = await refreshTokenService.CreateRefreshTokenAsync(user.Id, Guid.NewGuid().ToString());

        // Create a token for another user (should not be affected)
        var otherUser = await CreateTestUserAsync("otheruser@test.com");
        var otherUserToken = await refreshTokenService.CreateRefreshTokenAsync(otherUser.Id, Guid.NewGuid().ToString());

        // Act
        var result = await refreshTokenService.RevokeAllUserTokensAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify all tokens for the target user are revoked
        using var context = GetDbContext();
        var userTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .ToListAsync();
        
        Assert.All(userTokens, token => Assert.True(token.IsRevoked));

        // Verify other user's token is not affected
        var otherUserTokenFromDb = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == otherUserToken!.Token);
        
        Assert.NotNull(otherUserTokenFromDb);
        Assert.False(otherUserTokenFromDb.IsRevoked);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_UserWithNoTokens_ShouldReturnSuccess()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var refreshTokenService = GetService<IRefreshTokenService>();

        // Act
        var result = await refreshTokenService.RevokeAllUserTokensAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_WithExpiredTokens_ShouldRemoveExpiredTokens()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var refreshTokenService = GetService<IRefreshTokenService>();
        
        // Create valid tokens
        var validToken1 = await refreshTokenService.CreateRefreshTokenAsync(user.Id, Guid.NewGuid().ToString());
        var validToken2 = await refreshTokenService.CreateRefreshTokenAsync(user.Id, Guid.NewGuid().ToString());

        // Create tokens that should be cleaned up:
        // 1. Token expired beyond the cleanup cutoff (default 30 days)
        var oldExpiredToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            JwtId = Guid.NewGuid().ToString(),
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(-35), // Beyond 30-day cutoff
            IsRevoked = false
        };

        // 2. Recently expired but revoked token (should be cleaned up regardless of age)
        var revokedToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            JwtId = Guid.NewGuid().ToString(),
            UserId = user.Id,
            RevokedAt = DateTime.UtcNow.AddDays(-32),
            ExpiryDate = DateTime.UtcNow.AddDays(-1), // Recently expired
            IsRevoked = true // This makes it eligible for cleanup
        };

        // 3. Recently expired but not revoked token (should NOT be cleaned up)
        var recentExpiredToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            JwtId = Guid.NewGuid().ToString(),
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(-5), // Recently expired but within 30-day grace period
            IsRevoked = false
        };

        await using var context = GetDbContext();
        context.RefreshTokens.AddRange(oldExpiredToken, revokedToken, recentExpiredToken);
        await context.SaveChangesAsync();

        // Act
        await refreshTokenService.CleanupExpiredTokensAsync();

        // Assert - Use fresh context to avoid caching issues
        await using var assertContext = GetDbContext();
        var remainingTokens = await assertContext.RefreshTokens
            .Where(t => t.UserId == user.Id)
            .ToListAsync();

        // Should have 3 tokens remaining: 2 valid + 1 recently expired (not cleaned)
        Assert.Equal(3, remainingTokens.Count);
        
        // Verify the tokens that should have been removed are gone
        Assert.DoesNotContain(remainingTokens, t => t.Token == oldExpiredToken.Token);
        Assert.DoesNotContain(remainingTokens, t => t.Token == revokedToken.Token);
        
        // Verify the tokens that should remain are still there
        Assert.Contains(remainingTokens, t => t.Token == validToken1!.Token);
        Assert.Contains(remainingTokens, t => t.Token == validToken2!.Token);
        Assert.Contains(remainingTokens, t => t.Token == recentExpiredToken.Token);
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_WithNoExpiredTokens_ShouldReturnSuccess()
    {
        // Arrange
        var refreshTokenService = GetService<IRefreshTokenService>();
        var context = GetDbContext();
        
        var user = await CreateTestUserAsync();
        var validToken = await refreshTokenService.CreateRefreshTokenAsync(user.Id, Guid.NewGuid().ToString());

        var tokensBeforeCleanup = await context.RefreshTokens.CountAsync();

        // Act
        await refreshTokenService.CleanupExpiredTokensAsync();

        // Assert

        // Verify no tokens were removed
        var tokensAfterCleanup = await context.RefreshTokens.CountAsync();
        Assert.Equal(tokensBeforeCleanup, tokensAfterCleanup);
    }

    [Fact(Skip = "Check issues later. 4/5 failed")]
    public async Task CreateRefreshTokenAsync_ConcurrentCreation_ShouldHandleMultipleTokens()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var jwtIds = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid().ToString()).ToList();

        // Act - Create multiple tokens concurrently using separate service instances
        var tasks = jwtIds.Select(async jwtId =>
        {
            var refreshTokenService = GetService<IRefreshTokenService>();
            return await refreshTokenService.CreateRefreshTokenAsync(user.Id, jwtId);
        });
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => result.Should().NotBeNull());
        Assert.Equal(5, results.Length);

        // Verify all tokens have unique values
        var tokens = results.Select(r => r!.Token).ToList();
        Assert.Equal(5, tokens.Distinct().Count());

        // Verify all tokens are persisted using a fresh context
        await using var context = GetDbContext();
        var persistedTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .ToListAsync();
        
        Assert.Equal(5, persistedTokens.Count);
    }

    /// <summary>
    /// Helper method to create a test user in the database.
    /// </summary>
    /// <param name="email">Optional email address for the user.</param>
    /// <returns>The created ApplicationUser.</returns>
    private async Task<ApplicationUser> CreateTestUserAsync(string? email = null)
    {
        var uniqueId = Guid.NewGuid();
        email ??= $"testuser-{uniqueId:N}@test.com";

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            NormalizedUserName = email.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            ClientId = uniqueId // Set unique ClientId to avoid constraint violations
        };

        var context = GetDbContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        return user;
    }


}
