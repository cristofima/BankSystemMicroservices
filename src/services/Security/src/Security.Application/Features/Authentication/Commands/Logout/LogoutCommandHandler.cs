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
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing logout for user {UserId}", request.UserId);

            await RevokeUserTokensAsync(request);
            await LogUserLogoutAsync(request);

            _logger.LogInformation("Successfully processed logout for user {UserId}", request.UserId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", request.UserId);
            return Result.Failure("An error occurred during logout");
        }
    }

    private async Task RevokeUserTokensAsync(LogoutCommand request)
    {
        await _refreshTokenService.RevokeAllUserTokensAsync(request.UserId, request.IpAddress, "User logout");
    }

    private async Task LogUserLogoutAsync(LogoutCommand request)
    {
        await _auditService.LogUserLogoutAsync(request.UserId, request.IpAddress);
    }
}
