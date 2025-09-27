using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BankSystem.Shared.WebApiDefaults.Authentication;

/// <summary>
/// Authentication handler for API Key authentication.
/// Used for inter-service gRPC authentication in development and testing environments.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    )
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if API key header exists
        if (!Request.Headers.TryGetValue(Options.ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            Logger.LogDebug("API key header '{HeaderName}' not found", Options.ApiKeyHeaderName);
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            Logger.LogWarning("API key header '{HeaderName}' is empty", Options.ApiKeyHeaderName);
            return Task.FromResult(AuthenticateResult.Fail("API key is required"));
        }

        // Validate API key using constant-time comparison
        if (!IsValidApiKey(providedApiKey))
        {
            Logger.LogWarning(
                "Invalid API key provided from {RemoteIpAddress}",
                Request.HttpContext.Connection.RemoteIpAddress
            );
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Extract and validate service name
        var serviceName = GetServiceNameFromRequest();
        if (!string.IsNullOrEmpty(serviceName) && Options.ValidServices?.Any() == true)
        {
            if (!Options.ValidServices.Contains(serviceName, StringComparer.OrdinalIgnoreCase))
            {
                Logger.LogWarning(
                    "Service '{ServiceName}' is not in the allowed services list",
                    serviceName
                );
                return Task.FromResult(
                    AuthenticateResult.Fail($"Service '{serviceName}' is not authorized")
                );
            }
        }

        // Create claims for authenticated user
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, Options.UserName),
            new(ClaimTypes.NameIdentifier, Options.UserName),
            new(ClaimTypes.Role, Options.UserRole),
            new("scope", "inter-service"),
            new(ClaimTypes.AuthenticationMethod, "apikey"),
            new("api_key_type", "inter-service"),
        };

        // Add service name claim if available
        if (!string.IsNullOrEmpty(serviceName))
        {
            claims.Add(new("service_name", serviceName));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogDebug(
            "API key authentication successful for service '{ServiceName}'",
            serviceName ?? "unknown"
        );

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.Append("WWW-Authenticate", $"ApiKey realm=\"{Options.ApiKeyHeaderName}\"");
        return base.HandleChallengeAsync(properties);
    }

    /// <summary>
    /// Validates the provided API key against the configured value using constant-time comparison.
    /// </summary>
    /// <param name="providedApiKey">The API key provided in the request</param>
    /// <returns>True if the API key is valid, false otherwise</returns>
    private bool IsValidApiKey(string providedApiKey)
    {
        if (string.IsNullOrEmpty(Options.ApiKeyValue))
        {
            Logger.LogError("API key value is not configured");
            return false;
        }

        // Use constant-time comparison to prevent timing attacks
        return CryptographicEquals(providedApiKey, Options.ApiKeyValue);
    }

    /// <summary>
    /// Performs constant-time string comparison to prevent timing attacks.
    /// </summary>
    /// <param name="a">First string to compare</param>
    /// <param name="b">Second string to compare</param>
    /// <returns>True if strings are equal, false otherwise</returns>
    private static bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }

    /// <summary>
    /// Extracts the service name from the request for validation.
    /// </summary>
    /// <returns>The service name if found, null otherwise</returns>
    private string? GetServiceNameFromRequest()
    {
        // Try to get service name from custom header
        if (Request.Headers.TryGetValue("X-Service-Name", out var serviceNameHeader))
        {
            return serviceNameHeader.FirstOrDefault();
        }

        // Try to extract from User-Agent header
        if (Request.Headers.TryGetValue("User-Agent", out var userAgentHeader))
        {
            var userAgent = userAgentHeader.FirstOrDefault();
            if (!string.IsNullOrEmpty(userAgent))
            {
                // Expected format: ServiceName/Version or ServiceName-Api/Version
                var parts = userAgent.Split('/');
                if (parts.Length > 0)
                {
                    var serviceName = parts[0].Trim();
                    if (!string.IsNullOrEmpty(serviceName))
                    {
                        return serviceName;
                    }
                }
            }
        }

        // Try to extract from request path (for gRPC services)
        var path = Request.Path.Value;
        if (string.IsNullOrEmpty(path))
            return null;

        // gRPC service path format: /package.ServiceName/MethodName
        var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length <= 0)
            return null;

        var servicePath = pathParts[0];
        var lastDotIndex = servicePath.LastIndexOf('.');
        if (lastDotIndex >= 0 && lastDotIndex < servicePath.Length - 1)
        {
            return servicePath[(lastDotIndex + 1)..];
        }

        return null;
    }
}
