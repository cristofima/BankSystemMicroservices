# Health Checks Configuration

## Overview

The API Gateway health checks system has been updated to use configuration-based setup instead of hardcoded values. This allows for better flexibility across different environments and easier maintenance.

## Configuration Structure

### Health Check Options

```json
{
  "HealthChecks": {
    "TimeoutSeconds": 10,
    "Self": {
      "Name": "self",
      "Message": "The API Gateway is healthy and operational"
    },
    "Services": [
      {
        "Name": "security-api",
        "Uri": "http://localhost:5153/health",
        "DisplayName": "Security API Health Check",
        "TimeoutSeconds": 10,
        "FailureStatus": "Degraded",
        "Tags": ["security", "authentication"]
      }
    ]
  }
}
```

### Configuration Properties

#### Root Level Properties

- **`TimeoutSeconds`** (number, default: 10): Global timeout for all health checks
- **`Self`** (object): Configuration for the gateway's self health check
- **`Services`** (array): List of external service health checks

#### Self Health Check Properties

- **`Name`** (string, default: "self"): Name identifier for the self health check
- **`Message`** (string, default: "The API Gateway is healthy"): Message returned when healthy

#### Service Health Check Properties

- **`Name`** (string, required): Unique identifier for the service
- **`Uri`** (string, required): Health check endpoint URL
- **`DisplayName`** (string, optional): Human-readable name for the health check
- **`TimeoutSeconds`** (number, optional): Override global timeout for this service
- **`FailureStatus`** (string, default: "Degraded"): Status when health check fails (Healthy, Degraded, Unhealthy)
- **`Tags`** (array, optional): Tags for grouping and filtering health checks

## Environment-Specific Configuration

### Development Environment

The development configuration includes:

- Longer timeouts (15 seconds)
- Development-specific display names
- Additional tags for environment identification
- Extended service list for local development

### Production Environment

Production configuration should include:

- Shorter timeouts for faster failure detection
- Production service URLs
- Appropriate failure statuses
- Production-relevant tags

## Usage Examples

### Basic Configuration

```json
{
  "HealthChecks": {
    "TimeoutSeconds": 5,
    "Services": [
      {
        "Name": "accounts",
        "Uri": "https://accounts-api.prod.com/health"
      }
    ]
  }
}
```

### Advanced Configuration

```json
{
  "HealthChecks": {
    "TimeoutSeconds": 10,
    "Self": {
      "Name": "gateway",
      "Message": "API Gateway is operational"
    },
    "Services": [
      {
        "Name": "security-api",
        "Uri": "https://security-api.prod.com/health",
        "DisplayName": "Security & Authentication Service",
        "TimeoutSeconds": 5,
        "FailureStatus": "Unhealthy",
        "Tags": ["security", "critical", "authentication"]
      },
      {
        "Name": "account-api",
        "Uri": "https://account-api.prod.com/health",
        "DisplayName": "Account Management Service",
        "TimeoutSeconds": 8,
        "FailureStatus": "Degraded",
        "Tags": ["accounts", "core", "business"]
      },
      {
        "Name": "notification-api",
        "Uri": "https://notification-api.prod.com/health",
        "DisplayName": "Notification Service",
        "TimeoutSeconds": 15,
        "FailureStatus": "Degraded",
        "Tags": ["notifications", "non-critical"]
      }
    ]
  }
}
```

## Health Check Implementation

The health checks are implemented in `ServiceExtensions.cs` using the new `GatewayHealthCheckOptions` configuration class. The system:

1. **Loads configuration** from the `HealthChecks` section
2. **Configures self health check** with custom name and message
3. **Adds external service checks** with individual timeouts and failure statuses
4. **Supports tagging** for organizing and filtering health checks
5. **Validates configuration** using data annotations

## Benefits

### Flexibility

- Environment-specific configurations
- Different timeouts per service
- Custom failure statuses
- Service-specific tags

### Maintainability

- No hardcoded values in source code
- Centralized configuration management
- Easy to add or remove services

### Monitoring

- Clear service identification
- Configurable failure thresholds
- Support for external monitoring tools

## Migration from Hardcoded Values

The previous implementation used hardcoded values:

```csharp
// Old implementation
.AddUrlGroup(
    uri: new Uri("http://localhost:5153/health"),
    name: "Security API Health Check",
    failureStatus: HealthStatus.Degraded,
    timeout: TimeSpan.FromSeconds(10)
)
```

The new implementation uses configuration:

```csharp
// New implementation
healthChecksBuilder.AddUrlGroup(
    uri: new Uri(serviceCheck.Uri),
    name: displayName,
    failureStatus: failureStatus,
    timeout: timeout,
    tags: serviceCheck.Tags
);
```

This change provides better separation of concerns and improved maintainability.
