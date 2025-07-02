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
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _auditService = auditService;
        _logger = logger;
        _securityOptions = securityOptions.Value;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Token refresh attempt from IP {IpAddress}", request.IpAddress);

            // Validate the access token (even if expired)
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                _logger.LogWarning("Token refresh failed - invalid access token from IP {IpAddress}", request.IpAddress);
                return Result<RefreshTokenResponse>.Failure("Invalid token");
            }

            // Extract JWT ID and user ID from claims
            var jwtId = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var userId = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(jwtId) || string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Token refresh failed - missing claims in access token from IP {IpAddress}", request.IpAddress);
                return Result<RefreshTokenResponse>.Failure("Invalid token");
            }

            // Validate refresh token
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
                return Result<RefreshTokenResponse>.Failure("Invalid refresh token");
            }

            // Get user
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Token refresh failed - user {UserId} not found or inactive", userId);
                return Result<RefreshTokenResponse>.Failure("User not found or inactive");
            }

            // Generate new tokens
            var newAccessToken = await GenerateAccessTokenAsync(user);
            var newRefreshToken = await _refreshTokenService.RefreshTokenAsync(
                refreshToken,
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

            await _auditService.LogTokenRefreshAsync(user.Id, request.IpAddress);

            _logger.LogInformation("Token successfully refreshed for user {UserId} from IP {IpAddress}", 
                user.Id, request.IpAddress);

            return Result<RefreshTokenResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh from IP {IpAddress}", request.IpAddress);
            return Result<RefreshTokenResponse>.Failure("An error occurred during token refresh");
        }
    }

    private async Task<(string Token, string JwtId, DateTime Expiry)> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.NameIdentifier, user.Id),
            new("clientId", user.ClientId.ToString()),
            new(ClaimTypes.Email, user.Email!)
        };

        // Add user roles (get fresh roles in case they changed)
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return await _tokenService.CreateAccessTokenAsync(user, claims);
    }
}
