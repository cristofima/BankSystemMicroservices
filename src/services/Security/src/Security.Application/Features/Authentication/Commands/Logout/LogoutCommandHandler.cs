using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Domain.Common;

namespace Security.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Handler for logout command - revokes all user tokens
/// </summary>
public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenService refreshTokenService,
        ISecurityAuditService auditService,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokenService = refreshTokenService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing logout for user {UserId}", request.UserId);

            // Revoke all user tokens through the service
            await _refreshTokenService.RevokeAllUserTokensAsync(request.UserId, request.IpAddress, "User logout");

            // Audit log
            await _auditService.LogUserLogoutAsync(request.UserId, request.IpAddress);

            _logger.LogInformation("Successfully processed logout for user {UserId}", request.UserId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", request.UserId);
            return Result.Failure("An error occurred during logout");
        }
    }
}
