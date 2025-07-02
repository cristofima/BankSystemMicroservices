using MediatR;
using Security.Domain.Common;

namespace Security.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Command to refresh access token using refresh token
/// </summary>
public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken,
    string? IpAddress = null,
    string? DeviceInfo = null) : IRequest<Result<RefreshTokenResponse>>;

/// <summary>
/// Response containing new authentication tokens
/// </summary>
public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DateTime RefreshTokenExpiry);
