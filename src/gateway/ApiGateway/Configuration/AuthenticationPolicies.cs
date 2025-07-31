using Microsoft.AspNetCore.Authorization;

namespace BankSystem.ApiGateway.Configuration;

public static class AuthenticationPolicies
{
    public const string PublicEndpoints = "PublicEndpoints";
    public const string AuthenticatedUsers = "AuthenticatedUsers";
    public const string AdminOnly = "AdminOnly";
    public const string ManagerOrAdmin = "ManagerOrAdmin";
    public const string AccountOwnerOrAdmin = "AccountOwnerOrAdmin";

    public static void ConfigureAuthorizationPolicies(this IServiceCollection services)
    {
        var defaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        services.AddAuthorizationBuilder()
            .AddPolicy(PublicEndpoints, policy => policy.RequireAssertion(_ => true))
            .AddPolicy(AuthenticatedUsers, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(AdminOnly, policy => policy.RequireClaim("role", "Admin"))
            .AddPolicy(ManagerOrAdmin, policy => policy.RequireClaim("role", "Manager", "Admin"))
            .AddPolicy(AccountOwnerOrAdmin, policy =>
                policy.RequireAssertion(context =>
                {
                    var userIdClaim = context.User.FindFirst("sub")?.Value ??
                                      context.User.FindFirst("userId")?.Value;
                    var resourceUserId = context.Resource as string;
                    var isAdmin = context.User.HasClaim("role", "Admin");
                    return isAdmin || (userIdClaim != null && userIdClaim == resourceUserId);
                }))
            .SetDefaultPolicy(defaultPolicy)
            .SetFallbackPolicy(defaultPolicy);
    }

    public static readonly IReadOnlyDictionary<string, string> RouteToPolicy = new Dictionary<string, string>
    {
        // Public endpoints - no auth required
        { "/api/v1/auth/login", PublicEndpoints },
        { "/api/v1/auth/register", PublicEndpoints },
        { "/api/v1/auth/refresh", PublicEndpoints },
        { "/api/v1/auth/forgot-password", PublicEndpoints },
        { "/api/v1/auth/reset-password", PublicEndpoints },
        { "/health", PublicEndpoints },
        { "/health-ui", PublicEndpoints },

        // User endpoints - authenticated users
        { "/api/v1/accounts", AuthenticatedUsers },
        { "/api/v1/transactions", AuthenticatedUsers },
        { "/api/v1/movements", AuthenticatedUsers },

        // Admin endpoints
        { "/api/v1/admin", AdminOnly },
        { "/api/v1/users/admin", AdminOnly },

        // Manager endpoints
        { "/api/v1/reports", ManagerOrAdmin },
        { "/api/v1/audit", ManagerOrAdmin }
    };
}