using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Security.Application.Configuration;
using Security.Infrastructure.Services;
using Security.Infrastructure.IntegrationTests.Infrastructure;

namespace Security.Infrastructure.IntegrationTests.Services;

public class TokenServiceTests : BaseSecurityInfrastructureTest
{
    private TokenService CreateTokenService()
    {
        var options = GetService<IOptions<JwtOptions>>();
        return new TokenService(options);
    }

    private async Task<ApplicationUser> CreateTestUserAsync(string? email = null)
    {
        var uniqueId = Guid.NewGuid();
        email ??= $"tokenuser-{uniqueId:N}@test.com";
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
            ClientId = uniqueId
        };
        var context = GetDbContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateAccessTokenAsync_ShouldReturnValidTokenAndClaims()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Email, user.Email!)
        };
        var tokenService = CreateTokenService();

        // Act
        var (token, jwtId, expiry) = await tokenService.CreateAccessTokenAsync(user, claims);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(string.IsNullOrWhiteSpace(jwtId));
        Assert.True(expiry > DateTime.UtcNow);

        // Validate token structure
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Equal(jwtId, jwt.Id);
        Assert.Equal(user.Id, jwt.Subject);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == user.UserName);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public async Task GetPrincipalFromExpiredToken_ShouldReturnPrincipal_WhenTokenIsValid()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id!),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Email, user.Email!)
        };
        var tokenService = CreateTokenService();
        var (token, jwtId, expiry) = await tokenService.CreateAccessTokenAsync(user, claims);

        // Act
        var principal = tokenService.GetPrincipalFromExpiredToken(token);

        // Assert
        Assert.NotNull(principal);

        // Debug: print all claims if sub is missing
        var subClaim = principal!.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var nameIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        // Accept either sub or name identifier for user id
        Assert.True(
            subClaim == user.Id || nameIdClaim == user.Id,
            $"Expected sub or name identifier claim to be '{user.Id}', but got sub='{subClaim}', nameid='{nameIdClaim}'"
        );
        Assert.Equal(user.UserName, principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal(user.Email, principal.FindFirstValue(ClaimTypes.Email));
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldReturnNull_WhenTokenIsInvalid()
    {
        // Arrange
        var tokenService = CreateTokenService();
        const string invalidToken = "invalid.token.value";

        // Act
        var principal = tokenService.GetPrincipalFromExpiredToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public async Task IsJwtWithValidSecurityAlgorithm_ShouldReturnTrue_ForValidToken()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tokenService = CreateTokenService();
        var (token, _, _) = await tokenService.CreateAccessTokenAsync(user, []);

        // Act
        var result = tokenService.IsJwtWithValidSecurityAlgorithm(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsJwtWithValidSecurityAlgorithm_ShouldReturnFalse_ForMalformedToken()
    {
        // Arrange
        var tokenService = CreateTokenService();
        const string malformedToken = "not.a.jwt";

        // Act
        var result = tokenService.IsJwtWithValidSecurityAlgorithm(malformedToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenJwtKeyIsNullOrEmpty()
    {
        // Arrange
        var options = Options.Create(new JwtOptions
        {
            Key = "", // Empty key
            Issuer = "issuer",
            Audience = "audience",
            AccessTokenExpiryInMinutes = 5,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true
        });

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new TokenService(options));
        Assert.Contains("JWT key cannot be null or empty", ex.Message);
    }

    [Fact]
    public async Task GetPrincipalFromExpiredToken_ShouldReturnNull_WhenTokenHasInvalidAlgorithm()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tokenService = CreateTokenService();

        // Create a token with a different algorithm (e.g., none)
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "issuer",
            audience: "audience",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, user.Id)],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: null // No signature, alg=none
        );
        var token = handler.WriteToken(jwt);

        // Act
        var principal = tokenService.GetPrincipalFromExpiredToken(token);

        // Assert
        Assert.Null(principal);
    }
}
