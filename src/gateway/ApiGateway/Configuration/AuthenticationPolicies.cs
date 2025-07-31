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
        services.AddAuthorization(options =>
        {
            // Public endpoints - no authentication required
            options.AddPolicy(PublicEndpoints, policy =>
                policy.RequireAssertion(_ => true));

            // Authenticated users only
            options.AddPolicy(AuthenticatedUsers, policy =>
                policy.RequireAuthenticatedUser());

            // Admin only access
            options.AddPolicy(AdminOnly, policy =>
                policy.RequireClaim("role", "Admin"));

            // Manager or Admin access
            options.AddPolicy(ManagerOrAdmin, policy =>
                policy.RequireClaim("role", "Manager", "Admin"));

            // Account owner or Admin access (allows user to access their own data or admin to access any)
            options.AddPolicy(AccountOwnerOrAdmin, policy =>
                policy.RequireAssertion(context =>
                {
                    var userIdClaim = context.User.FindFirst("sub")?.Value ??
                                     context.User.FindFirst("userId")?.Value;
                    var resourceUserId = context.Resource as string;
                    var isAdmin = context.User.HasClaim("role", "Admin");

                    return isAdmin || (userIdClaim != null && userIdClaim == resourceUserId);
                }));

            // Default policy - require authentication
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Fallback policy for unmatched routes
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }

    public static readonly Dictionary<string, string> RouteToPolicy = new()
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
