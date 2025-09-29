using System.Security.Cryptography;
using System.Text;
using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.WebApiDefaults.Common;
using BankSystem.Shared.WebApiDefaults.Configuration;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;

namespace BankSystem.Shared.WebApiDefaults.Interceptors;

/// <summary>
/// gRPC interceptor for inter-service authentication using API Key or mTLS.
/// Validates incoming requests based on the configured authentication method.
/// </summary>
public class InterServiceAuthenticationInterceptor : Interceptor
{
    private readonly InterServiceSecurityOptions _options;
    private readonly ILogger<InterServiceAuthenticationInterceptor> _logger;

    public InterServiceAuthenticationInterceptor(
        IOptions<InterServiceSecurityOptions> options,
        ILogger<InterServiceAuthenticationInterceptor> logger
    )
    {
        Guard.AgainstNull(options);
        Guard.AgainstNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation
    )
    {
        try
        {
            // Validate authentication based on configured method
            var authenticationResult = _options.Authentication.Method switch
            {
                AuthenticationMethod.ApiKey => await ValidateApiKeyAsync(context),
                AuthenticationMethod.MTls => await ValidateMTlsAsync(context),
                _ => AuthenticationResult.Failure(
                    "Unknown authentication method",
                    _options.Authentication.Method
                ),
            };

            if (!authenticationResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Inter-service authentication failed: {Reason}",
                    authenticationResult.ErrorMessage
                );

                throw new RpcException(
                    new Status(StatusCode.Unauthenticated, authenticationResult.ErrorMessage)
                );
            }

            // Add authentication context to gRPC context
            AddAuthenticationContext(
                context,
                authenticationResult,
                _options.Authentication.RequiredScope
            );

            _logger.LogDebug(
                "Inter-service authentication successful for service: {ServiceName}",
                authenticationResult.ServiceName
            );

            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during inter-service authentication");
            throw new RpcException(
                new Status(StatusCode.Internal, "Authentication service unavailable")
            );
        }
    }

    private Task<AuthenticationResult> ValidateApiKeyAsync(ServerCallContext context)
    {
        var metadata = context.RequestHeaders;
        var apiKeyEntry = metadata.FirstOrDefault(m =>
            string.Equals(m.Key, _options.ApiKey.HeaderName, StringComparison.OrdinalIgnoreCase)
        );

        if (apiKeyEntry == null)
        {
            return Task.FromResult(
                AuthenticationResult.Failure($"Missing {_options.ApiKey.HeaderName} header")
            );
        }

        var providedApiKey = apiKeyEntry.Value;
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticationResult.Failure("Empty API key provided"));
        }

        // Validate API key using constant-time comparison
        var expectedApiKey = _options.ApiKey.Value ?? string.Empty;
        var providedBytes = Encoding.UTF8.GetBytes(providedApiKey);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
        if (!CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
        {
            return Task.FromResult(AuthenticationResult.Failure("Invalid API key"));
        }

        if (!_options.ApiKey.IsValid())
        {
            return Task.FromResult(
                AuthenticationResult.Failure("API key configuration is invalid")
            );
        }

        // Extract service name from metadata using multiple strategies
        var serviceName = GetServiceNameFromMetadata(metadata);

        // Validate service is allowed
        if (
            _options.Authentication.AllowedServices.Any()
            && !_options.Authentication.AllowedServices.Contains(
                serviceName,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            return Task.FromResult(
                AuthenticationResult.Failure($"Service '{serviceName}' is not allowed")
            );
        }

        return Task.FromResult(
            AuthenticationResult.Success(serviceName, AuthenticationMethod.ApiKey)
        );
    }

    /// <summary>
    /// Extracts the service name from gRPC metadata using multiple strategies.
    /// </summary>
    /// <param name="metadata">The gRPC request metadata</param>
    /// <returns>The service name if found, "Unknown" otherwise</returns>
    private static string GetServiceNameFromMetadata(Metadata metadata)
    {
        // Strategy 1: Check for X-Service-Name header
        var serviceNameEntry = metadata.FirstOrDefault(m =>
            string.Equals(m.Key, "x-service-name", StringComparison.OrdinalIgnoreCase)
        );

        if (!string.IsNullOrEmpty(serviceNameEntry?.Value))
        {
            return serviceNameEntry.Value;
        }

        // Strategy 2: Check for service-name metadata (backward compatibility)
        serviceNameEntry = metadata.FirstOrDefault(m =>
            string.Equals(m.Key, "service-name", StringComparison.OrdinalIgnoreCase)
        );

        if (!string.IsNullOrEmpty(serviceNameEntry?.Value))
        {
            return serviceNameEntry.Value;
        }

        // Strategy 3: Check User-Agent header
        var userAgentEntry = metadata.FirstOrDefault(m =>
            string.Equals(m.Key, "user-agent", StringComparison.OrdinalIgnoreCase)
        );

        if (!string.IsNullOrEmpty(userAgentEntry?.Value))
        {
            var userAgent = userAgentEntry.Value;
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

        return "Unknown";
    }

    private Task<AuthenticationResult> ValidateMTlsAsync(ServerCallContext context)
    {
        // For mTLS, the certificate validation is typically handled at the HTTP/2 level
        // Here we can perform additional validations if needed

        var peerInfo = context.Peer; // Contains certificate information

        // Extract client certificate information from the gRPC context
        // This requires proper mTLS configuration at the Kestrel/HTTP level

        // For now, we'll validate based on peer information
        if (string.IsNullOrWhiteSpace(peerInfo))
        {
            return Task.FromResult(
                AuthenticationResult.Failure(
                    "No peer certificate information available",
                    AuthenticationMethod.MTls
                )
            );
        }

        // In a real implementation, you would:
        // 1. Extract the client certificate from the context
        // 2. Validate the certificate chain
        // 3. Check certificate subject/issuer
        // 4. Verify certificate is not revoked

        _logger.LogDebug("mTLS peer information: {PeerInfo}", peerInfo);

        // Extract service name from certificate subject or metadata
        var metadata = context.RequestHeaders;
        var serviceName = GetServiceNameFromMetadata(metadata);

        // Validate service is allowed
        if (
            _options.Authentication.AllowedServices.Any()
            && !_options.Authentication.AllowedServices.Contains(
                serviceName,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            return Task.FromResult(
                AuthenticationResult.Failure(
                    $"Service '{serviceName}' is not allowed",
                    AuthenticationMethod.MTls
                )
            );
        }

        return Task.FromResult(
            AuthenticationResult.Success(serviceName, AuthenticationMethod.MTls)
        );
    }

    private static void AddAuthenticationContext(
        ServerCallContext context,
        AuthenticationResult authResult,
        string requiredScope
    )
    {
        // Add authentication information to the gRPC context for downstream services
        var authContext = new Dictionary<string, string>
        {
            ["authenticated"] = "true",
            ["auth_method"] = authResult.Method.ToString().ToLowerInvariant(),
            ["service_name"] = authResult.ServiceName,
            ["scope"] = requiredScope,
            ["authenticated_at"] = DateTimeOffset.UtcNow.ToString("O"),
        };

        foreach (var kvp in authContext)
        {
            context.UserState[kvp.Key] = kvp.Value;
        }
    }
}
