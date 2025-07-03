using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Security.Application.Configuration;
using Security.Application.Interfaces;
using Security.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Security.Infrastructure.Services;

/// <summary>
/// Service for creating and validating JWT tokens
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly SymmetricSecurityKey _key;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public TokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        
        if (string.IsNullOrWhiteSpace(_jwtOptions.Key))
            throw new ArgumentException("JWT key cannot be null or empty", nameof(jwtOptions));

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public Task<(string Token, string JwtId, DateTime Expiry)> CreateAccessTokenAsync(
        ApplicationUser user, 
        IEnumerable<Claim> claims)
    {
        var jwtId = Guid.NewGuid().ToString();
        var expiry = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiryInMinutes);

        var allClaims = new List<Claim>(claims)
        {
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Sub, user.Id)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(allClaims),
            Expires = expiry,
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = _tokenHandler.WriteToken(token);

        return Task.FromResult((tokenString, jwtId, expiry));
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = _jwtOptions.ValidateIssuerSigningKey,
            IssuerSigningKey = _key,
            ValidateIssuer = _jwtOptions.ValidateIssuer,
            ValidateAudience = _jwtOptions.ValidateAudience,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            ValidateLifetime = false, // Don't validate expiry for refresh tokens
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = _tokenHandler.ValidateToken(accessToken, validationParameters, out var securityToken);
            
            if (!IsJwtWithValidSecurityAlgorithm(accessToken))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public bool IsJwtWithValidSecurityAlgorithm(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}