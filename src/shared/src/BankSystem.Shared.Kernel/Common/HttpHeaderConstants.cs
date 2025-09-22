namespace BankSystem.Shared.Kernel.Common;

/// <summary>
/// Contains constants for HTTP headers commonly used across the Bank System microservices.
/// </summary>
public static class HttpHeaderConstants
{
    /// <summary>
    /// The correlation ID header used to track requests across microservices.
    /// This header helps with distributed tracing and logging correlation.
    /// </summary>
    public const string CorrelationId = "X-Correlation-ID";

    /// <summary>
    /// Standard Authorization header for bearer tokens.
    /// </summary>
    public const string Authorization = "Authorization";

    /// <summary>
    /// Standard Content-Type header for request/response content type specification.
    /// </summary>
    public const string ContentType = "Content-Type";

    /// <summary>
    /// Custom header for API version specification (alternative to query parameter).
    /// </summary>
    public const string ApiVersion = "X-API-Version";

    /// <summary>
    /// Custom header for request ID tracking (different from correlation ID).
    /// </summary>
    public const string RequestId = "X-Request-ID";

    /// <summary>
    /// Custom header for client information.
    /// </summary>
    public const string ClientInfo = "X-Client-Info";

    /// <summary>
    /// Array of headers commonly used in CORS configuration for allowed headers.
    /// </summary>
    public static readonly string[] CommonAllowedHeaders =
    [
        Authorization,
        ContentType,
        CorrelationId,
        ApiVersion,
        RequestId,
        ClientInfo
    ];

    /// <summary>
    /// Array of headers commonly exposed in CORS configuration.
    /// </summary>
    public static readonly string[] CommonExposedHeaders =
    [
        CorrelationId,
        ApiVersion,
        RequestId
    ];
}