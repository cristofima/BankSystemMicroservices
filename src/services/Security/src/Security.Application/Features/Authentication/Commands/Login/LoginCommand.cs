using BankSystem.Shared.Domain.Common;
using MediatR;

namespace Security.Application.Features.Authentication.Commands.Login;

/// <summary>
/// Command to authenticate a user and return access/refresh tokens
/// </summary>
public record LoginCommand(
    string UserName,
    string Password,
    string? IpAddress = null,
    string? DeviceInfo = null
) : IRequest<Result<LoginResponse>>;

/// <summary>
/// Response containing authentication tokens
/// </summary>
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiry,
    DateTimeOffset RefreshTokenExpiry
);
