using BankSystem.Account.Application.Interfaces;
using System.Security.Claims;

namespace BankSystem.Account.Api.Services;

public class AuthenticatedUserService(IHttpContextAccessor httpContextAccessor) : IAuthenticatedUserService
{
    public Guid UserId { get; } = Guid.TryParse(
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId
        : Guid.Empty;

    public Guid CustomerId { get; } = Guid.TryParse(
        httpContextAccessor.HttpContext?.User.FindFirstValue("clientId"), out var customerId)
        ? customerId
        : Guid.Empty;

    public string UserName { get; } = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
}