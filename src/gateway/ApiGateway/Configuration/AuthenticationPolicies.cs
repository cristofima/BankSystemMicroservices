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
        services.AddAuthorization(options =>
        {
            // Configure a fallback policy that allows anonymous access by default
            // This prevents ASP.NET Core from requiring authentication globally
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAssertion(_ => true) // Allow anonymous access by default
                .Build();
        });

        services
            .AddAuthorizationBuilder()
            .AddPolicy(PublicEndpoints, policy => policy.RequireAssertion(_ => true))
            .AddPolicy(AuthenticatedUsers, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(AdminOnly, policy => policy.RequireRole("Admin"))
            .AddPolicy(ManagerOrAdmin, policy => policy.RequireRole("Manager", "Admin"));
    }
}
