using System.Security.Claims;
using BankSystem.Shared.Domain.Common;

namespace BankSystem.Shared.WebApiDefaults.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId { get; } =
        Guid.TryParse(
            httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId
        )
            ? userId
            : Guid.Empty;

    public Guid CustomerId { get; } =
        Guid.TryParse(
            httpContextAccessor.HttpContext?.User.FindFirstValue("clientId"),
            out var customerId
        )
            ? customerId
            : Guid.Empty;

    public string UserName { get; } =
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
}
