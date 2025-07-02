using MediatR;
using Security.Domain.Common;

namespace Security.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Command to logout user by revoking all their tokens
/// </summary>
public record LogoutCommand(
    string UserId,
    string? IpAddress = null) : IRequest<Result>;
