using Security.Application.Dtos;

namespace Security.Application.UnitTests.Dtos;

public class TokenResponseTests
{
    [Fact]
    public void TokenResponse_Constructor_ShouldSetAllProperties()
    {
        // Arrange
        const string accessToken = "access_token_123";
        const string refreshToken = "refresh_token_456";
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        // Act
        var tokenResponse = new TokenResponse(accessToken, refreshToken, accessTokenExpiry, refreshTokenExpiry);

        // Assert
        Assert.Equal(accessToken, tokenResponse.AccessToken);
        Assert.Equal(refreshToken, tokenResponse.RefreshToken);
        Assert.Equal(accessTokenExpiry, tokenResponse.AccessTokenExpiry);
        Assert.Equal(refreshTokenExpiry, tokenResponse.RefreshTokenExpiry);
    }

    [Fact]
    public void TokenResponse_PropertyGetters_ShouldReturnCorrectValues()
    {
        // Arrange
        const string accessToken = "test_access_token";
        const string refreshToken = "test_refresh_token";
        var accessTokenExpiry = new DateTime(2024, 12, 25, 10, 30, 0);
        var refreshTokenExpiry = new DateTime(2025, 1, 1, 10, 30, 0);

        // Act
        var tokenResponse = new TokenResponse(accessToken, refreshToken, accessTokenExpiry, refreshTokenExpiry);

        // Assert
        Assert.Equal(accessToken, tokenResponse.AccessToken);
        Assert.Equal(refreshToken, tokenResponse.RefreshToken);
        Assert.Equal(accessTokenExpiry, tokenResponse.AccessTokenExpiry);
        Assert.Equal(refreshTokenExpiry, tokenResponse.RefreshTokenExpiry);
    }

    [Fact]
    public void TokenResponse_WithEmptyTokens_ShouldSetEmptyValues()
    {
        // Arrange
        var accessToken = string.Empty;
        var refreshToken = string.Empty;
        var accessTokenExpiry = DateTime.MinValue;
        var refreshTokenExpiry = DateTime.MinValue;

        // Act
        var tokenResponse = new TokenResponse(accessToken, refreshToken, accessTokenExpiry, refreshTokenExpiry);

        // Assert
        Assert.Equal(string.Empty, tokenResponse.AccessToken);
        Assert.Equal(string.Empty, tokenResponse.RefreshToken);
        Assert.Equal(DateTime.MinValue, tokenResponse.AccessTokenExpiry);
        Assert.Equal(DateTime.MinValue, tokenResponse.RefreshTokenExpiry);
    }
}

public class RefreshTokenRequestTests
{
    [Fact]
    public void RefreshTokenRequest_Constructor_ShouldSetAllProperties()
    {
        // Arrange
        const string accessToken = "access_token_123";
        const string refreshToken = "refresh_token_456";

        // Act
        var request = new RefreshTokenRequest(accessToken, refreshToken);

        // Assert
        Assert.Equal(accessToken, request.AccessToken);
        Assert.Equal(refreshToken, request.RefreshToken);
    }

    [Fact]
    public void RefreshTokenRequest_PropertyGetters_ShouldReturnCorrectValues()
    {
        // Arrange
        const string accessToken = "test_access_token";
        const string refreshToken = "test_refresh_token";

        // Act
        var request = new RefreshTokenRequest(accessToken, refreshToken);

        // Assert
        Assert.Equal(accessToken, request.AccessToken);
        Assert.Equal(refreshToken, request.RefreshToken);
    }

    [Fact]
    public void RefreshTokenRequest_WithEmptyTokens_ShouldSetEmptyValues()
    {
        // Arrange
        var accessToken = string.Empty;
        var refreshToken = string.Empty;

        // Act
        var request = new RefreshTokenRequest(accessToken, refreshToken);

        // Assert
        Assert.Equal(string.Empty, request.AccessToken);
        Assert.Equal(string.Empty, request.RefreshToken);
    }
}

public class RevokeTokenRequestTests
{
    [Fact]
    public void RevokeTokenRequest_Constructor_ShouldSetTokenProperty()
    {
        // Arrange
        const string token = "token_to_revoke_123";

        // Act
        var request = new RevokeTokenRequest(token);

        // Assert
        Assert.Equal(token, request.Token);
    }

    [Fact]
    public void RevokeTokenRequest_PropertyGetter_ShouldReturnCorrectValue()
    {
        // Arrange
        const string token = "test_token";

        // Act
        var request = new RevokeTokenRequest(token);

        // Assert
        Assert.Equal(token, request.Token);
    }

    [Fact]
    public void RevokeTokenRequest_WithEmptyToken_ShouldSetEmptyValue()
    {
        // Arrange
        var token = string.Empty;

        // Act
        var request = new RevokeTokenRequest(token);

        // Assert
        Assert.Equal(string.Empty, request.Token);
    }

    [Fact]
    public void RevokeTokenRequest_WithNullToken_ShouldSetNullValue()
    {
        // Arrange
        string? token = null;

        // Act
        var request = new RevokeTokenRequest(token!);

        // Assert
        Assert.Null(request.Token);
    }
}
