# .NET Aspire Trace Filtering Configuration

## Overview

This document explains the OpenTelemetry trace filtering implementation in our .NET Aspire microservices architecture. The filtering is designed to reduce telemetry noise and focus on business-critical endpoints.

## Implementation

### Location

The trace filtering logic is implemented in:

- **File**: `src/aspire-app/ServiceDefaults/Extensions.cs`
- **Method**: `ConfigureOpenTelemetry()` in the `AddAspNetCoreInstrumentation()` configuration

### Filtering Logic

#### Priority 1: Include Versioned API Endpoints

```csharp
if (path?.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase) == true)
    return true;
```

**What it does**: Only traces HTTP requests that start with `/api/v` (case-insensitive)

**Examples of traced endpoints**:

- `/api/v1/accounts`
- `/api/v2/transactions`
- `/api/v1/users/authenticate`
- `/API/V1/payments` (case-insensitive)

#### Priority 2: Exclude Infrastructure Endpoints

```csharp
return !IsExcludedPath(path);
```

**What it does**: Excludes common infrastructure and documentation endpoints

**Examples of excluded endpoints**:

- `/health` - Health check endpoints
- `/swagger` - Swagger UI
- `/scalar` - Scalar API documentation
- `/openapi` - OpenAPI specification
- `/favicon.ico` - Browser favicon requests
- `/metrics` - Prometheus metrics
- And many more (see complete list below)

### Complete List of Excluded Paths

```csharp
var excludedPaths = new[]
{
    "/health",           // Health checks
    "/alive",            // Liveness checks
    "/ready",            // Readiness checks
    "/live",             // Alternative liveness
    "/healthz",          // Kubernetes health checks
    "/livez",            // Kubernetes liveness
    "/readyz",           // Kubernetes readiness
    "/swagger",          // Swagger UI
    "/swagger-ui",       // Alternative Swagger UI
    "/scalar",           // Scalar API documentation
    "/openapi",          // OpenAPI specification
    "/api-docs",         // API documentation
    "/favicon.ico",      // Favicon requests
    "/robots.txt",       // Robots file
    "/sitemap.xml",      // Sitemap
    "/.well-known",      // Well-known URIs
    "/metrics",          // Prometheus metrics
    "/ping",             // Simple ping endpoint
    "/version",          // Version endpoint
    "/status"            // Status endpoint
};
```

## Benefits

### 1. Reduced Telemetry Noise

- Eliminates traces from health checks that run every few seconds
- Removes documentation endpoint traces that don't represent business operations
- Filters out browser requests for static resources

### 2. Cost Optimization

- Reduces the volume of traces sent to observability platforms
- Lowers telemetry ingestion costs
- Improves query performance in monitoring dashboards

### 3. Focused Monitoring

- Concentrates traces on actual business API calls
- Makes it easier to identify performance issues in business logic
- Simplifies alerts and monitoring rules

### 4. Better Performance

- Reduces overhead from unnecessary trace collection
- Minimizes memory usage for trace buffers
- Improves application performance by reducing instrumentation overhead

## Configuration Examples

### Trace Collection Results

#### ✅ Will be traced:

```
GET /api/v1/accounts/123
POST /api/v2/transactions
PUT /api/v1/users/profile
DELETE /api/v1/accounts/456
```

#### ❌ Will NOT be traced:

```
GET /health
GET /swagger/index.html
GET /scalar/v1
GET /openapi.json
GET /favicon.ico
GET /metrics
```

## Customization

### Adding New Excluded Paths

To exclude additional paths, modify the `excludedPaths` array in the `IsExcludedPath` method:

```csharp
var excludedPaths = new[]
{
    // ... existing paths ...
    "/your-new-path",    // Your custom path to exclude
    "/admin"             // Another example
};
```

### Changing API Version Pattern

To change the included API pattern, modify the filter condition:

```csharp
// Example: Include all /api/* endpoints
if (path?.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) == true)
    return true;

// Example: Include specific versions only
if (path?.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase) == true ||
    path?.StartsWith("/api/v2/", StringComparison.OrdinalIgnoreCase) == true)
    return true;
```

## Testing

### Verify Filtering is Working

1. **Start the application** with Aspire
2. **Make requests** to different endpoints:

   ```bash
   # Should be traced
   curl http://localhost:5000/api/v1/accounts

   # Should NOT be traced
   curl http://localhost:5000/health
   curl http://localhost:5000/swagger
   ```

3. **Check traces** in your observability platform (Azure Application Insights, Jaeger, etc.)
4. **Verify** that only `/api/v*` endpoints appear in traces

### Local Testing with Aspire Dashboard

1. Run the Aspire application
2. Access the Aspire Dashboard (typically at `http://localhost:15888`)
3. Navigate to the "Traces" section
4. Verify that only business API calls are being traced

## Troubleshooting

### Common Issues

#### All Requests Being Traced

- Check that the filter is properly configured in `AddAspNetCoreInstrumentation()`
- Verify no other configuration is overriding the filter

#### No Requests Being Traced

- Ensure your API endpoints start with `/api/v`
- Check for typos in the path pattern matching
- Verify OpenTelemetry is properly configured

#### Performance Issues

- If filtering is too aggressive, consider including more endpoint patterns
- Monitor application performance before and after implementing filtering

## Integration with Monitoring Tools

### Azure Application Insights

The filtered traces will automatically appear in Azure Application Insights with reduced noise, making it easier to:

- Create meaningful dashboards
- Set up alerts on business operations
- Analyze performance trends

### Prometheus/Grafana

When using Prometheus metrics, the `/metrics` endpoint is excluded from tracing to prevent circular monitoring.

### Jaeger/Zipkin

Traces sent to distributed tracing systems will only include business-relevant operations, improving query performance and storage efficiency.

## Security Considerations

- **No sensitive data exposure**: The filter only examines the request path, not headers or body
- **Performance impact**: Minimal overhead as filtering happens early in the request pipeline
- **Logging**: Consider logging filtered requests if debugging is needed (not implemented by default for performance)

## Future Enhancements

Potential improvements to consider:

1. **Configuration-based filtering**: Move excluded paths to configuration files
2. **Dynamic filtering**: Allow runtime changes to filter rules
3. **Metrics on filtering**: Track how many requests are filtered vs traced
4. **Custom attributes**: Add custom attributes to traces based on filtering rules

## Related Documentation

- [.NET Aspire Overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [ASP.NET Core Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore)
