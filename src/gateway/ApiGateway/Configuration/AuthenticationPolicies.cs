namespace BankSystem.ApiGateway.Configuration;

public static class AuthenticationPolicies
{
    public const string PublicEndpoints = "PublicEndpoints";
    public const string AuthenticatedUsers = "AuthenticatedUsers";
    public const string AdminOnly = "AdminOnly";
    public const string ManagerOrAdmin = "ManagerOrAdmin";

    public static void ConfigureAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(PublicEndpoints, policy => policy.RequireAssertion(_ => true))
            .AddPolicy(AuthenticatedUsers, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(AdminOnly, policy => policy.RequireClaim("role", "Admin"))
            .AddPolicy(ManagerOrAdmin, policy => policy.RequireClaim("role", "Manager", "Admin"));
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