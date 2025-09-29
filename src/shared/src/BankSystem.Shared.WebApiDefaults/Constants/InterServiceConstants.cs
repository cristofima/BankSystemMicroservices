using System.Diagnostics.CodeAnalysis;

namespace BankSystem.Shared.WebApiDefaults.Constants;

/// <summary>
/// Constants for inter-service gRPC communication and authentication.
/// Centralizes policy names, environment variables, and error messages to improve maintainability.
/// </summary>
[ExcludeFromCodeCoverage]
public static class InterServiceConstants
{
    #region Authentication Schemes and Policies

    /// <summary>
    /// Authentication scheme and policy name for inter-service API Key authentication.
    /// Used in development and testing environments.
    /// </summary>
    public const string ApiKeyScheme = "InterServiceApiKey";

    /// <summary>
    /// Authentication scheme and policy name for inter-service mTLS authentication.
    /// Used in production environments for enhanced security.
    /// </summary>
    public const string MTlsScheme = "InterServiceMTls";

    #endregion

    #region Claims

    /// <summary>
    /// Scope claim type used for inter-service authorization.
    /// </summary>
    public const string ScopeClaim = "scope";

    #endregion

    #region Error Messages

    /// <summary>
    /// Error message when mTLS authentication is requested but the required package is not available.
    /// </summary>
    public const string MTlsPackageRequiredError =
        "mTLS authentication requires Microsoft.AspNetCore.Authentication.Certificate package. "
        + "Please install the package or use ApiKey authentication for development.";

    /// <summary>
    /// Error message when Azure Key Vault certificate loading is not implemented.
    /// </summary>
    public const string KeyVaultNotImplementedError =
        "Azure Key Vault certificate loading not implemented yet";

    /// <summary>
    /// Error message when mTLS client certificate configuration fails.
    /// </summary>
    public const string MTlsClientConfigError = "Failed to configure mTLS client certificate";

    /// <summary>
    /// Error message when mTLS server certificate configuration fails.
    /// </summary>
    public const string MTlsServerConfigError = "Failed to configure mTLS server certificate";

    #endregion
}
