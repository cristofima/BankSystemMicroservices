using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Security.Application.Configuration;
using Security.Application.Interfaces;
using Security.Domain.Common;
using Security.Domain.Entities;
using System.Security.Claims;

namespace Security.Application.Features.Authentication.Commands.Login;

/// <summary>
/// Handler for login command
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly SecurityOptions _securityOptions;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ISecurityAuditService auditService,
        ILogger<LoginCommandHandler> logger,
        IOptions<SecurityOptions> securityOptions)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _auditService = auditService;
        _logger = logger;
        _securityOptions = securityOptions.Value;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Login attempt for user {UserName} from IP {IpAddress}", 
                request.UserName, request.IpAddress);

            // Find user
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                _logger.LogWarning("Login failed - user {UserName} not found", request.UserName);
                await _auditService.LogFailedAuthenticationAsync(request.UserName, request.IpAddress, "User not found");
                return Result<LoginResponse>.Failure("Invalid username or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed - user {UserName} is deactivated", request.UserName);
                await _auditService.LogFailedAuthenticationAsync(user.Id, request.IpAddress, "Account deactivated");
                return Result<LoginResponse>.Failure("Account is deactivated");
            }

            // Check lockout status
            if (user.IsLockedOut(_securityOptions.MaxFailedLoginAttempts, _securityOptions.LockoutDuration))
            {
                _logger.LogWarning("Login failed - user {UserName} is locked out", request.UserName);
                await _auditService.LogFailedAuthenticationAsync(user.Id, request.IpAddress, "Account locked out");
                return Result<LoginResponse>.Failure("Account is temporarily locked due to multiple failed attempts");
            }

            // Validate password
            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                user.RecordFailedLogin();
                await _userManager.UpdateAsync(user);

                _logger.LogWarning("Login failed - invalid password for user {UserName}", request.UserName);
                await _auditService.LogFailedAuthenticationAsync(user.Id, request.IpAddress, "Invalid password");
                return Result<LoginResponse>.Failure("Invalid username or password");
            }

            // Reset failed attempts on successful authentication
            user.RecordSuccessfulLogin();
            await _userManager.UpdateAsync(user);

            // Generate tokens
            var accessToken = await GenerateAccessTokenAsync(user);
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(
                user.Id, 
                accessToken.JwtId,
                request.IpAddress,
                request.DeviceInfo,
                cancellationToken);

            if (refreshToken == null)
            {
                _logger.LogError("Failed to create refresh token for user {UserId}", user.Id);
                return Result<LoginResponse>.Failure("Authentication failed");
            }

            var response = new LoginResponse(
                accessToken.Token,
                refreshToken.Token,
                accessToken.Expiry,
                refreshToken.ExpiryDate);

            await _auditService.LogSuccessfulAuthenticationAsync(user.Id, request.IpAddress);
            
            _logger.LogInformation("User {UserId} successfully authenticated from IP {IpAddress}", 
                user.Id, request.IpAddress);

            return Result<LoginResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {UserName}", request.UserName);
            return Result<LoginResponse>.Failure("An error occurred during authentication");
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

        // Add user roles (safely handle case where roles might not be configured)
        try
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any())
            {
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            }
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning("Role store not configured properly: {Error}. Proceeding without role claims.", ex.Message);
            // Continue without role claims - this allows the system to work even if roles aren't properly configured
        }

        return await _tokenService.CreateAccessTokenAsync(user, claims);
    }
}
