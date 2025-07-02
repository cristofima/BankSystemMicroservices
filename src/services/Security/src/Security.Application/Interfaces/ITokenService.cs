using Security.Domain.Entities;
using System.Security.Claims;

namespace Security.Application.Interfaces;

/// <summary>
/// Service for creating and validating JWT tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates an access token for the given user with claims
    /// </summary>
    Task<(string Token, string JwtId, DateTime Expiry)> CreateAccessTokenAsync(
        ApplicationUser user, 
        IEnumerable<Claim> claims);

    /// <summary>
    /// Validates an expired access token and returns the principal
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken);

    /// <summary>
    /// Validates the security algorithm of a JWT token
    /// </summary>
    bool IsJwtWithValidSecurityAlgorithm(string token);
}