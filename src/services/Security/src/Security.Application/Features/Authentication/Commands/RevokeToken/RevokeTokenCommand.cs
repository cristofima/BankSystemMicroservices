using MediatR;
using Security.Domain.Common;

namespace Security.Application.Features.Authentication.Commands.RevokeToken;

/// <summary>
/// Command to revoke a refresh token
/// </summary>
public record RevokeTokenCommand(
    string Token,
    string? IpAddress = null,
    string? Reason = null) : IRequest<Result>;
