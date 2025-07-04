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
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogInformation("Processing logout for user {UserId}", request.UserId);

            // Validate user ID
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                _logger.LogWarning("Logout failed - invalid user ID");
                return Result.Failure("Invalid user ID");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var revokeResult = await RevokeUserTokensAsync(request, cancellationToken);
            if (!revokeResult.IsSuccess)
            {
                _logger.LogWarning("Failed to revoke tokens for user {UserId}: {Error}", request.UserId, revokeResult.Error);
                return revokeResult;
            }
            
            cancellationToken.ThrowIfCancellationRequested();
            
            await LogUserLogoutAsync(request);

            _logger.LogInformation("Successfully processed logout for user {UserId}", request.UserId);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Logout operation was cancelled for user {UserId}", request.UserId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", request.UserId);
            return Result.Failure("An error occurred during logout");
        }
    }

    private async Task<Result> RevokeUserTokensAsync(LogoutCommand request, CancellationToken cancellationToken)
    {
        return await _refreshTokenService.RevokeAllUserTokensAsync(request.UserId, request.IpAddress, "User logout", cancellationToken);
    }

    private async Task LogUserLogoutAsync(LogoutCommand request)
    {
        await _auditService.LogUserLogoutAsync(request.UserId, request.IpAddress);
    }
}
