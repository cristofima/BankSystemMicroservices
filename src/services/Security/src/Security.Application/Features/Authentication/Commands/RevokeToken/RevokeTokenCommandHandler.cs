using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Domain.Common;

namespace Security.Application.Features.Authentication.Commands.RevokeToken;

/// <summary>
/// Handler for revoke token command
/// </summary>
public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<RevokeTokenCommandHandler> _logger;

    public RevokeTokenCommandHandler(
        IRefreshTokenService refreshTokenService,
        ISecurityAuditService auditService,
        ILogger<RevokeTokenCommandHandler> logger)
    {
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        try
        {
            _logger.LogInformation("Token revocation attempt from IP {IpAddress}", request.IpAddress);

            cancellationToken.ThrowIfCancellationRequested();

            var result = await RevokeTokenAsync(request, cancellationToken);
            
            cancellationToken.ThrowIfCancellationRequested();
            
            await HandleRevocationResultAsync(request, result);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Token revocation operation was cancelled from IP {IpAddress}", request.IpAddress);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation from IP {IpAddress}", request.IpAddress);
            return Result.Failure("An error occurred during token revocation");
        }
    }

    private async Task<Result> RevokeTokenAsync(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        return await _refreshTokenService.RevokeTokenAsync(
            request.Token, 
            request.IpAddress, 
            request.Reason, 
            cancellationToken);
    }

    private async Task HandleRevocationResultAsync(RevokeTokenCommand request, Result result)
    {
        if (result.IsSuccess)
        {
            await LogSuccessfulRevocationAsync(request);
        }
        else
        {
            LogFailedRevocation(request, result.Error);
        }
    }

    private async Task LogSuccessfulRevocationAsync(RevokeTokenCommand request)
    {
        await _auditService.LogTokenRevocationAsync(request.Token, request.IpAddress, request.Reason);
        _logger.LogInformation("Token successfully revoked from IP {IpAddress}", request.IpAddress);
    }

    private void LogFailedRevocation(RevokeTokenCommand request, string error)
    {
        _logger.LogWarning("Token revocation failed from IP {IpAddress}: {Error}", 
            request.IpAddress, error);
    }
}
