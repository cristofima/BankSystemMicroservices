using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using BankSystem.Shared.WebApiDefaults.Constants;

namespace BankSystem.Shared.WebApiDefaults.Configuration;

/// <summary>
/// Configuration options for inter-service security.
/// Supports both API Key (development) and mTLS (production) authentication methods.
/// </summary>
[ExcludeFromCodeCoverage]
public class InterServiceSecurityOptions
{
    public const string SectionName = "InterServiceSecurity";

    /// <summary>
    /// gRPC-specific configuration
    /// </summary>
    public GrpcOptions Grpc { get; set; } = new();

    /// <summary>
    /// Authentication configuration for inter-service communication
    /// </summary>
    public AuthenticationOptions Authentication { get; set; } = new();

    /// <summary>
    /// API Key configuration for development environment
    /// </summary>
    public ApiKeyOptions ApiKey { get; set; } = new();

    /// <summary>
    /// mTLS configuration for production environment
    /// </summary>
    public MTlsOptions MTls { get; set; } = new();

    public class GrpcOptions
    {
        /// <summary>
        /// Maximum message size for gRPC communication (default: 4MB)
        /// </summary>
        [Range(1024, 16777216)] // 1KB to 16MB
        public int MaxMessageSize { get; set; } = 4 * 1024 * 1024;

        /// <summary>
        /// Enable detailed gRPC errors (should be false in production)
        /// </summary>
        public bool EnableDetailedErrors { get; set; } = false;

        /// <summary>
        /// gRPC reflection configuration
        /// </summary>
        public ReflectionOptions Reflection { get; set; } = new();

        public class ReflectionOptions
        {
            /// <summary>
            /// Enable gRPC reflection (should be false in production)
            /// </summary>
            public bool Enabled { get; set; } = false;
        }
    }

    public class AuthenticationOptions
    {
        /// <summary>
        /// Required scope for inter-service authentication
        /// </summary>
        [Required]
        public string RequiredScope { get; set; } = "inter-service";

        /// <summary>
        /// List of allowed service names for inter-service communication
        /// </summary>
        public List<string> AllowedServices { get; set; } = new();

        /// <summary>
        /// Authentication method: ApiKey (development) or MTls (production)
        /// </summary>
        [Required]
        public AuthenticationMethod Method { get; set; } = AuthenticationMethod.ApiKey;
    }

    public class ApiKeyOptions
    {
        /// <summary>
        /// HTTP header name containing the API key
        /// </summary>
        public string HeaderName { get; set; } = "X-Service-Key";

        /// <summary>
        /// API key value (should be configured via environment variables or Azure Key Vault)
        /// </summary>
        [Required]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Username for API key authentication context
        /// </summary>
        public string UserName { get; set; } = InterServiceConstants.ApiKeyScheme;

        /// <summary>
        /// Role assigned to API key authenticated requests
        /// </summary>
        public string UserRole { get; set; } = "InterService";

        /// <summary>
        /// Validate that API key configuration is present and valid
        /// </summary>
        public bool IsValid() => !string.IsNullOrWhiteSpace(Value) && Value.Length >= 16;
    }

    public class MTlsOptions
    {
        /// <summary>
        /// Enable mutual TLS authentication
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Path to the server certificate file
        /// </summary>
        public string ServerCertificatePath { get; set; } = string.Empty;

        /// <summary>
        /// Path to the server private key file
        /// </summary>
        public string ServerKeyPath { get; set; } = string.Empty;

        /// <summary>
        /// Path to the client certificate file (for outgoing requests)
        /// </summary>
        public string ClientCertificatePath { get; set; } = string.Empty;

        /// <summary>
        /// Path to the client private key file (for outgoing requests)
        /// </summary>
        public string ClientKeyPath { get; set; } = string.Empty;

        /// <summary>
        /// Path to the Certificate Authority certificate file
        /// </summary>
        public string CaCertificatePath { get; set; } = string.Empty;

        /// <summary>
        /// Azure Key Vault configuration for certificate management
        /// </summary>
        public AzureKeyVaultOptions AzureKeyVault { get; set; } = new();

        /// <summary>
        /// Validate that mTLS configuration is complete
        /// </summary>
        public bool IsValid() =>
            Enabled
            && (
                (
                    !string.IsNullOrWhiteSpace(ServerCertificatePath)
                    && !string.IsNullOrWhiteSpace(ServerKeyPath)
                ) || AzureKeyVault.IsValid()
            );

        public class AzureKeyVaultOptions
        {
            /// <summary>
            /// Enable Azure Key Vault integration for certificate management
            /// </summary>
            public bool Enabled { get; set; } = false;

            /// <summary>
            /// Azure Key Vault URL
            /// </summary>
            public string VaultUrl { get; set; } = string.Empty;

            /// <summary>
            /// Server certificate name in Key Vault
            /// </summary>
            public string ServerCertificateName { get; set; } = string.Empty;

            /// <summary>
            /// Client certificate name in Key Vault
            /// </summary>
            public string ClientCertificateName { get; set; } = string.Empty;

            /// <summary>
            /// CA certificate name in Key Vault
            /// </summary>
            public string CaCertificateName { get; set; } = string.Empty;

            /// <summary>
            /// Validate Azure Key Vault configuration
            /// </summary>
            public bool IsValid() =>
                Enabled
                && !string.IsNullOrWhiteSpace(VaultUrl)
                && !string.IsNullOrWhiteSpace(ServerCertificateName);
        }
    }
}

/// <summary>
/// Authentication methods for inter-service communication
/// </summary>
public enum AuthenticationMethod
{
    /// <summary>
    /// API Key authentication (suitable for development and testing)
    /// </summary>
    ApiKey = 0,

    /// <summary>
    /// Mutual TLS authentication (recommended for production)
    /// </summary>
    MTls = 1,
}
