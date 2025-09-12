namespace Security.Application.Dtos;

/// <summary>
/// Response model containing authentication tokens
/// </summary>
public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiry,
    DateTimeOffset RefreshTokenExpiry
);

/// <summary>
/// Request model for token refresh
/// </summary>
public record RefreshTokenRequest(string AccessToken, string RefreshToken);

/// <summary>
/// Request model for token revocation
/// </summary>
public record RevokeTokenRequest(string Token);
