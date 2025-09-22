using BankSystem.Shared.WebApiDefaults.Constants;
using Microsoft.AspNetCore.Authorization;

namespace BankSystem.ApiGateway.Configuration;

public static class AuthenticationPolicies
{
    private const string PublicEndpoints = "PublicEndpoints";
    private const string AuthenticatedUsers = "AuthenticatedUsers";
    private const string AdminOnly = "AdminOnly";
    private const string ManagerOrAdmin = "ManagerOrAdmin";

    public static void ConfigureAuthorizationPolicies(this IServiceCollection services)
    {
        // Configure a fallback policy that allows anonymous access by default
        // This prevents ASP.NET Core from requiring authentication globally
        var fallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true) // Allow anonymous access by default
            .Build();

        services
            .AddAuthorizationBuilder()
            .SetFallbackPolicy(fallbackPolicy)
            .AddPolicy(PublicEndpoints, policy => policy.RequireAssertion(_ => true))
            .AddPolicy(AuthenticatedUsers, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(
                AdminOnly,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole(RoleConstants.Admin);
                }
            )
            .AddPolicy(
                ManagerOrAdmin,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole(RoleConstants.Manager, RoleConstants.Admin);
                }
            );
    }
}
