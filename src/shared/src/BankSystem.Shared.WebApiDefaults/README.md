# BankSystem.Shared.WebApiDefaults

## Overview

`BankSystem.Shared.WebApiDefaults` is a shared library that provides standardized Web API and gRPC configurations, authentication, authorization, and common components for all microservices in the Bank System. This library ensures consistency across services and reduces code duplication.

## Features

- **🔐 Inter-Service Authentication**: API Key and mTLS authentication for service-to-service communication
- **🌐 Web API Defaults**: Standardized REST API configuration with versioning, CORS, and rate limiting
- **📡 gRPC Support**: Complete gRPC setup with authentication and interceptors
- **🛡️ Security**: Authentication handlers, authorization policies, and security interceptors
- **📊 Observability**: Health checks, logging, and monitoring integration
- **🔧 Extensions**: Easy-to-use extension methods for service registration

## Quick Start

### Web API Service

```csharp
// Program.cs
builder.Services.AddWebApiDefaults(builder.Configuration, "My Service API");
builder.Services.AddInterServiceGrpc(builder.Configuration);

var app = builder.Build();

app.UseWebApiDefaults();
await app.RunAsync();
```

### gRPC Service

```csharp
// Program.cs
builder.Services.AddInterServiceGrpc(builder.Configuration);

var app = builder.Build();
app.MapGrpcService<MyGrpcService>();
await app.RunAsync();
```

## Documentation

### Core Components

- **[Authentication Guide](./docs/authentication.md)** - Inter-service authentication with API Keys and mTLS
- **[gRPC Configuration](./docs/grpc-configuration.md)** - Complete gRPC setup and usage
- **[Web API Extensions](./docs/web-api-extensions.md)** - REST API defaults and configuration
- **[Security Policies](./docs/security-policies.md)** - Authorization policies and role-based access

### Configuration References

- **[Configuration Options](./docs/configuration-options.md)** - All available configuration settings
- **[Environment Setup](./docs/environment-setup.md)** - Local development and testing setup
- **[Troubleshooting](./docs/troubleshooting.md)** - Common issues and solutions

### Development Guides

- **[Local Testing](./docs/local-testing.md)** - How to test inter-service communication locally
- **[Extensions Development](./docs/extensions-development.md)** - Creating custom extensions

## Configuration Example

```json
{
  "InterServiceSecurity": {
    "Authentication": {
      "Method": "ApiKey",
      "AllowedServices": ["Movement.Api", "Account.Api", "Transaction.Api"]
    },
    "ApiKey": {
      "HeaderName": "X-Service-ApiKey",
      "Value": "your-secure-api-key-here",
      "UserName": "InterServiceApiKey",
      "UserRole": "InterService"
    },
    "Grpc": {
      "MaxMessageSize": 4194304,
      "EnableDetailedErrors": true
    }
  }
}
```

## Project Structure

```
BankSystem.Shared.WebApiDefaults/
├── Authentication/              # Authentication handlers and options
├── Configuration/              # Configuration classes and options
├── Constants/                  # Shared constants and policy definitions
├── Extensions/                 # Service registration extensions
├── Interceptors/               # gRPC interceptors
├── JsonConverters/            # Custom JSON converters
├── Middlewares/               # HTTP middlewares
├── Services/                  # Shared services
└── docs/                      # Documentation files
```

## Dependencies

- **ASP.NET Core 9.0**: Web API and hosting
- **Grpc.AspNetCore**: gRPC server support
- **Scalar.AspNetCore**: API documentation
- **Asp.Versioning**: API versioning
- **Microsoft.Extensions.Diagnostics.HealthChecks**: Health monitoring

## Usage in Microservices

This library is used by all microservices in the Bank System:

- **Security.Api**: User authentication and authorization
- **Account.Api**: Account management operations
- **Movement.Api**: Transaction and movement processing
- **Transaction.Api**: Transaction history and reporting
- **Notification.Api**: Event notifications and alerts
- **Reporting.Api**: Data reporting and analytics

## Contributing

When contributing to this shared library, remember that changes affect all microservices. Please:

1. Update documentation when adding new features
2. Maintain backward compatibility when possible
3. Add appropriate unit tests
4. Update configuration examples
5. Test with all dependent microservices

## See Also

- [BankSystem.Shared.Domain](../BankSystem.Shared.Domain/README.md)
- [BankSystem.Shared.Infrastructure](../BankSystem.Shared.Infrastructure/README.md)
- [BankSystem Architecture Documentation](../../../../docs/README.md)
