using Microsoft.AspNetCore.Authentication;

namespace BankSystem.Shared.WebApiDefaults.Authentication;

/// <summary>
/// Options for API Key authentication scheme.
/// Used for inter-service gRPC authentication in development and testing environments.
/// </summary>
public sealed class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// The name of the header that contains the API key.
    /// Default: "X-Service-ApiKey"
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "X-Service-ApiKey";

    /// <summary>
    /// The expected API key value for authentication.
    /// Should be configured through application settings.
    /// </summary>
    public string ApiKeyValue { get; set; } = string.Empty;

    /// <summary>
    /// The name to use for the authenticated user when API key is valid.
    /// Default: "inter-service"
    /// </summary>
    public string UserName { get; set; } = "inter-service";

    /// <summary>
    /// The role to assign to the authenticated user when API key is valid.
    /// Default: "service"
    /// </summary>
    public string UserRole { get; set; } = "service";

    /// <summary>
    /// List of valid service names that are allowed to use this API key.
    /// If null or empty, all services are allowed.
    /// </summary>
    public IList<string>? ValidServices { get; set; }

    public override string ToString() =>
        $"{nameof(ApiKeyAuthenticationSchemeOptions)}(Header={ApiKeyHeaderName}, Services={ValidServices?.Count ?? 0})";
}
