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
        _refreshTokenService = refreshTokenService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Token revocation attempt from IP {IpAddress}", request.IpAddress);

            var result = await _refreshTokenService.RevokeTokenAsync(
                request.Token, 
                request.IpAddress, 
                request.Reason, 
                cancellationToken);

            if (result.IsSuccess)
            {
                await _auditService.LogTokenRevocationAsync(request.Token, request.IpAddress, request.Reason);
                _logger.LogInformation("Token successfully revoked from IP {IpAddress}", request.IpAddress);
            }
            else
            {
                _logger.LogWarning("Token revocation failed from IP {IpAddress}: {Error}", 
                    request.IpAddress, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation from IP {IpAddress}", request.IpAddress);
            return Result.Failure("An error occurred during token revocation");
        }
    }
}
