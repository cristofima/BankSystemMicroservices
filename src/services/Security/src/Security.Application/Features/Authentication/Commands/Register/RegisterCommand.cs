using MediatR;
using Security.Application.Dtos;
using Security.Domain.Common;

namespace Security.Application.Features.Authentication.Commands.Register;

/// <summary>
/// Command to register a new user
/// </summary>
public record RegisterCommand(
    string UserName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? FirstName = null,
    string? LastName = null,
    string? IpAddress = null,
    string? DeviceInfo = null) : IRequest<Result<UserResponse>>;
