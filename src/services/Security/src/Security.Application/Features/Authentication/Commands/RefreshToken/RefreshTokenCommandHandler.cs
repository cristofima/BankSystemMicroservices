using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Security.Application.Configuration;
using Security.Application.Interfaces;
using Security.Domain.Common;
using Security.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Security.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Handler for refresh token command
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;
    private readonly SecurityOptions _securityOptions;

    public RefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ISecurityAuditService auditService,
        ILogger<RefreshTokenCommandHandler> logger,
        IOptions<SecurityOptions> securityOptions)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _securityOptions = securityOptions?.Value ?? throw new ArgumentNullException(nameof(securityOptions));
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        try
        {
            _logger.LogInformation("Token refresh attempt from IP {IpAddress}", request.IpAddress);

            var tokenValidationResult = ValidateAccessToken(request);
            if (tokenValidationResult.IsFailure)
                return Result<RefreshTokenResponse>.Failure(tokenValidationResult.Error);

            var (jwtId, userId) = tokenValidationResult.Value;

            var refreshTokenValidationResult = await ValidateRefreshTokenAsync(request, jwtId, userId, cancellationToken);
            if (refreshTokenValidationResult.IsFailure)
                return Result<RefreshTokenResponse>.Failure(refreshTokenValidationResult.Error);

            var refreshToken = refreshTokenValidationResult.Value!; // Safe because we checked IsFailure above

            var userValidationResult = await ValidateUserAsync(userId);
            if (userValidationResult.IsFailure)
                return Result<RefreshTokenResponse>.Failure(userValidationResult.Error);

            var user = userValidationResult.Value!; // Safe because we checked IsFailure above

            var newTokensResult = await GenerateNewTokensAsync(user, refreshToken, request, cancellationToken);
            if (newTokensResult.IsFailure)
                return Result<RefreshTokenResponse>.Failure(newTokensResult.Error);

            await _auditService.LogTokenRefreshAsync(user.Id, request.IpAddress);

            _logger.LogInformation("Token successfully refreshed for user {UserId} from IP {IpAddress}", 
                user.Id, request.IpAddress);

            return Result<RefreshTokenResponse>.Success(newTokensResult.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh from IP {IpAddress}", request.IpAddress);
            return Result<RefreshTokenResponse>.Failure("An error occurred during token refresh");
        }
    }

    private Result<(string JwtId, string UserId)> ValidateAccessToken(RefreshTokenCommand request)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            _logger.LogWarning("Token refresh failed - invalid access token from IP {IpAddress}", request.IpAddress);
            return Result<(string, string)>.Failure("Invalid token");
        }

        var jwtId = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
        var userId = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(jwtId) || string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Token refresh failed - missing claims in access token from IP {IpAddress}", request.IpAddress);
            return Result<(string, string)>.Failure("Invalid token");
        }

        return Result<(string, string)>.Success((jwtId, userId));
    }

    private async Task<Result<Domain.Entities.RefreshToken>> ValidateRefreshTokenAsync(
        RefreshTokenCommand request, 
        string jwtId, 
        string userId, 
        CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenService.ValidateRefreshTokenAsync(
            request.RefreshToken, 
            jwtId, 
            userId, 
            cancellationToken);

        if (refreshToken == null)
        {
            _logger.LogWarning("Token refresh failed - invalid refresh token for user {UserId} from IP {IpAddress}", 
                userId, request.IpAddress);
            await _auditService.LogFailedAuthenticationAsync(userId, request.IpAddress, "Invalid refresh token");
            return Result<Domain.Entities.RefreshToken>.Failure("Invalid refresh token");
        }

        return Result<Domain.Entities.RefreshToken>.Success(refreshToken);
    }

    private async Task<Result<ApplicationUser>> ValidateUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Token refresh failed - user {UserId} not found or inactive", userId);
            return Result<ApplicationUser>.Failure("Invalid refresh token");
        }

        return Result<ApplicationUser>.Success(user);
    }

    private async Task<Result<RefreshTokenResponse>> GenerateNewTokensAsync(
        ApplicationUser user,
        Domain.Entities.RefreshToken oldRefreshToken,
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var newAccessToken = await GenerateAccessTokenAsync(user);
        var newRefreshToken = await _refreshTokenService.RefreshTokenAsync(
            oldRefreshToken,
            newAccessToken.JwtId,
            request.IpAddress,
            request.DeviceInfo,
            cancellationToken);

        if (newRefreshToken == null)
        {
            _logger.LogError("Failed to create new refresh token for user {UserId}", user.Id);
            return Result<RefreshTokenResponse>.Failure("Token refresh failed");
        }

        var response = new RefreshTokenResponse(
            newAccessToken.Token,
            newRefreshToken.Token,
            newAccessToken.Expiry,
            newRefreshToken.ExpiryDate);

        return Result<RefreshTokenResponse>.Success(response);
    }

    private async Task<(string Token, string JwtId, DateTime Expiry)> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var claims = CreateClaimsForUser(user);
        await AddRoleClaimsAsync(user, claims);

        return await _tokenService.CreateAccessTokenAsync(user, claims);
    }

    private static List<Claim> CreateClaimsForUser(ApplicationUser user)
    {
        return new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.NameIdentifier, user.Id),
            new("clientId", user.ClientId.ToString()),
            new(ClaimTypes.Email, user.Email!)
        };
    }

    private async Task AddRoleClaimsAsync(ApplicationUser user, List<Claim> claims)
    {
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
    }
}
