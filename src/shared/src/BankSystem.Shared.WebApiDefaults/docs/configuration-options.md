# Configuration Options Reference

## Overview

This document provides a complete reference for all configuration options available in BankSystem.Shared.WebApiDefaults, including examples for different environments and deployment scenarios.

## Core Configuration Sections

### InterServiceSecurity Configuration

Complete configuration for inter-service authentication and authorization:

```json
{
  "InterServiceSecurity": {
    "Grpc": {
      "MaxMessageSize": 4194304,
      "EnableDetailedErrors": true,
      "Reflection": {
        "Enabled": true
      }
    },
    "Authentication": {
      "Method": "ApiKey",
      "RequiredScope": "inter-service",
      "AllowedServices": [
        "Security.Api",
        "Account.Api",
        "Movement.Api",
        "Transaction.Api",
        "Notification.Api",
        "Reporting.Api"
      ]
    },
    "ApiKey": {
      "HeaderName": "X-Service-ApiKey",
      "Value": "dev-security-service-key-2024-banking-microservices",
      "UserName": "InterServiceApiKey",
      "UserRole": "InterService"
    },
    "MTls": {
      "Enabled": false,
      "RequireClientCertificate": true,
      "CertificateValidation": {
        "ValidateChain": true,
        "ValidateThumbprint": true,
        "AllowedThumbprints": ["thumbprint1", "thumbprint2"]
      }
    }
  }
}
```

#### InterServiceSecurity Properties

| Property                                        | Type       | Default                | Description                                        |
| ----------------------------------------------- | ---------- | ---------------------- | -------------------------------------------------- |
| `Grpc.MaxMessageSize`                           | `int`      | `4194304`              | Maximum gRPC message size in bytes (4MB)           |
| `Grpc.EnableDetailedErrors`                     | `bool`     | `false`                | Enable detailed gRPC error messages (dev only)     |
| `Grpc.Reflection.Enabled`                       | `bool`     | `false`                | Enable gRPC reflection service                     |
| `Authentication.Method`                         | `string`   | `"ApiKey"`             | Authentication method: "ApiKey", "MTls", or "Both" |
| `Authentication.RequiredScope`                  | `string`   | `"inter-service"`      | Required authentication scope                      |
| `Authentication.AllowedServices`                | `string[]` | `[]`                   | List of allowed service names                      |
| `ApiKey.HeaderName`                             | `string`   | `"X-Service-ApiKey"`   | HTTP header name for API key                       |
| `ApiKey.Value`                                  | `string`   | Required               | API key value                                      |
| `ApiKey.UserName`                               | `string`   | `"InterServiceApiKey"` | Username for API key authentication                |
| `ApiKey.UserRole`                               | `string`   | `"InterService"`       | Role for API key authentication                    |
| `MTls.Enabled`                                  | `bool`     | `false`                | Enable mutual TLS authentication                   |
| `MTls.RequireClientCertificate`                 | `bool`     | `true`                 | Require client certificate                         |
| `MTls.CertificateValidation.ValidateChain`      | `bool`     | `true`                 | Validate certificate chain                         |
| `MTls.CertificateValidation.ValidateThumbprint` | `bool`     | `true`                 | Validate certificate thumbprint                    |
| `MTls.CertificateValidation.AllowedThumbprints` | `string[]` | `[]`                   | List of allowed certificate thumbprints            |

### Web API Defaults Configuration

Configuration for REST API features:

```json
{
  "WebApiDefaults": {
    "EnableSwagger": true,
    "EnableCors": true,
    "EnableRateLimiting": true,
    "EnableCompression": true,
    "EnableHealthChecks": true,
    "Cors": {
      "PolicyName": "DefaultCorsPolicy",
      "AllowedOrigins": [
        "http://localhost:3000",
        "https://localhost:3000",
        "https://bankapp.azurewebsites.net"
      ],
      "AllowedHeaders": ["*"],
      "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"],
      "AllowCredentials": true,
      "MaxAge": 300
    },
    "RateLimit": {
      "GlobalLimiter": {
        "PermitLimit": 100,
        "Window": "00:01:00",
        "QueueLimit": 2,
        "QueueProcessingOrder": "OldestFirst"
      },
      "Policies": [
        {
          "Name": "AuthenticatedUsers",
          "PermitLimit": 1000,
          "Window": "00:01:00",
          "QueueLimit": 10
        },
        {
          "Name": "PublicApi",
          "PermitLimit": 50,
          "Window": "00:01:00",
          "QueueLimit": 5
        }
      ]
    },
    "Swagger": {
      "Title": "Bank System API",
      "Version": "v1",
      "Description": "RESTful API for banking operations",
      "ContactName": "Development Team",
      "ContactEmail": "dev@banksystem.com",
      "LicenseName": "MIT",
      "LicenseUrl": "https://opensource.org/licenses/MIT"
    },
    "HealthChecks": {
      "Path": "/health",
      "DetailedPath": "/health/detailed",
      "ReadinessPath": "/health/ready",
      "LivenessPath": "/health/live"
    },
    "ApiVersioning": {
      "DefaultVersion": "1.0",
      "AssumeDefaultVersionWhenUnspecified": true,
      "ReadVersionFromUrl": true,
      "ReadVersionFromQuery": true,
      "ReadVersionFromHeader": true,
      "QueryParameterName": "version",
      "HeaderName": "X-Version"
    }
  }
}
```

#### WebApiDefaults Properties

| Property                              | Type       | Default               | Description                             |
| ------------------------------------- | ---------- | --------------------- | --------------------------------------- |
| `EnableSwagger`                       | `bool`     | `true`                | Enable Swagger/OpenAPI documentation    |
| `EnableCors`                          | `bool`     | `true`                | Enable Cross-Origin Resource Sharing    |
| `EnableRateLimiting`                  | `bool`     | `true`                | Enable rate limiting middleware         |
| `EnableCompression`                   | `bool`     | `true`                | Enable response compression             |
| `EnableHealthChecks`                  | `bool`     | `true`                | Enable health check endpoints           |
| `Cors.PolicyName`                     | `string`   | `"DefaultCorsPolicy"` | CORS policy name                        |
| `Cors.AllowedOrigins`                 | `string[]` | `[]`                  | Allowed CORS origins                    |
| `Cors.AllowedHeaders`                 | `string[]` | `["*"]`               | Allowed CORS headers                    |
| `Cors.AllowedMethods`                 | `string[]` | `["*"]`               | Allowed CORS methods                    |
| `Cors.AllowCredentials`               | `bool`     | `false`               | Allow credentials in CORS requests      |
| `Cors.MaxAge`                         | `int`      | `300`                 | CORS preflight cache duration (seconds) |
| `RateLimit.GlobalLimiter.PermitLimit` | `int`      | `100`                 | Global rate limit permits               |
| `RateLimit.GlobalLimiter.Window`      | `TimeSpan` | `"00:01:00"`          | Global rate limit window                |
| `RateLimit.GlobalLimiter.QueueLimit`  | `int`      | `2`                   | Global rate limit queue size            |
| `Swagger.Title`                       | `string`   | `"API"`               | Swagger document title                  |
| `Swagger.Version`                     | `string`   | `"v1"`                | API version                             |
| `Swagger.Description`                 | `string`   | `""`                  | API description                         |
| `HealthChecks.Path`                   | `string`   | `"/health"`           | Basic health check endpoint             |
| `HealthChecks.DetailedPath`           | `string`   | `"/health/detailed"`  | Detailed health check endpoint          |

## Environment-Specific Configurations

### Development Environment

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BankSystem_Dev;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "InterServiceSecurity": {
    "Grpc": {
      "EnableDetailedErrors": true,
      "Reflection": {
        "Enabled": true
      }
    },
    "Authentication": {
      "AllowedServices": [
        "Security.Api",
        "Account.Api",
        "Movement.Api",
        "Transaction.Api",
        "test-client",
        "grpc-node-js",
        "development-client"
      ]
    },
    "ApiKey": {
      "Value": "dev-security-service-key-2024-banking-microservices"
    }
  },
  "WebApiDefaults": {
    "EnableSwagger": true,
    "Cors": {
      "AllowedOrigins": [
        "http://localhost:3000",
        "http://localhost:3001",
        "http://127.0.0.1:3000",
        "https://localhost:3000",
        "https://localhost:3001"
      ]
    },
    "RateLimit": {
      "GlobalLimiter": {
        "PermitLimit": 1000,
        "Window": "00:01:00"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "BankSystem": "Debug"
    }
  }
}
```

### Staging Environment

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=staging-db.database.windows.net;Database=BankSystem_Staging;User Id=staging_user;Password={password};Encrypt=true;"
  },
  "InterServiceSecurity": {
    "Grpc": {
      "EnableDetailedErrors": false,
      "Reflection": {
        "Enabled": false
      }
    },
    "Authentication": {
      "AllowedServices": [
        "Security.Api",
        "Account.Api",
        "Movement.Api",
        "Transaction.Api",
        "Notification.Api",
        "Reporting.Api"
      ]
    },
    "ApiKey": {
      "Value": "{STAGING_API_KEY}"
    },
    "MTls": {
      "Enabled": true,
      "RequireClientCertificate": true,
      "CertificateValidation": {
        "ValidateChain": true,
        "ValidateThumbprint": true,
        "AllowedThumbprints": ["{STAGING_CERT_THUMBPRINT}"]
      }
    }
  },
  "WebApiDefaults": {
    "EnableSwagger": true,
    "Cors": {
      "AllowedOrigins": [
        "https://staging.bankapp.com",
        "https://staging-admin.bankapp.com"
      ]
    },
    "RateLimit": {
      "GlobalLimiter": {
        "PermitLimit": 500,
        "Window": "00:01:00"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "BankSystem": "Information"
    }
  }
}
```

### Production Environment

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-db.database.windows.net;Database=BankSystem_Prod;User Id={user};Password={password};Encrypt=true;Connection Timeout=30;"
  },
  "InterServiceSecurity": {
    "Grpc": {
      "MaxMessageSize": 4194304,
      "EnableDetailedErrors": false,
      "Reflection": {
        "Enabled": false
      }
    },
    "Authentication": {
      "Method": "Both",
      "AllowedServices": [
        "Security.Api",
        "Account.Api",
        "Movement.Api",
        "Transaction.Api",
        "Notification.Api",
        "Reporting.Api"
      ]
    },
    "ApiKey": {
      "HeaderName": "X-Service-ApiKey",
      "Value": "{PROD_API_KEY}",
      "UserName": "InterServiceApiKey",
      "UserRole": "InterService"
    },
    "MTls": {
      "Enabled": true,
      "RequireClientCertificate": true,
      "CertificateValidation": {
        "ValidateChain": true,
        "ValidateThumbprint": true,
        "AllowedThumbprints": [
          "{PROD_CERT_THUMBPRINT_1}",
          "{PROD_CERT_THUMBPRINT_2}"
        ]
      }
    }
  },
  "WebApiDefaults": {
    "EnableSwagger": false,
    "EnableCors": true,
    "EnableRateLimiting": true,
    "EnableCompression": true,
    "Cors": {
      "AllowedOrigins": [
        "https://bankapp.com",
        "https://admin.bankapp.com",
        "https://mobile.bankapp.com"
      ],
      "AllowCredentials": true,
      "MaxAge": 86400
    },
    "RateLimit": {
      "GlobalLimiter": {
        "PermitLimit": 100,
        "Window": "00:01:00",
        "QueueLimit": 2
      },
      "Policies": [
        {
          "Name": "PremiumUsers",
          "PermitLimit": 2000,
          "Window": "00:01:00"
        },
        {
          "Name": "StandardUsers",
          "PermitLimit": 500,
          "Window": "00:01:00"
        },
        {
          "Name": "PublicApi",
          "PermitLimit": 50,
          "Window": "00:01:00"
        }
      ]
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "BankSystem": "Information",
      "System": "Error"
    }
  }
}
```

## Azure-Specific Configuration

### Azure App Service Configuration

```json
{
  "InterServiceSecurity": {
    "ApiKey": {
      "Value": "@Microsoft.KeyVault(SecretUri=https://banksystem-kv.vault.azure.net/secrets/inter-service-api-key/)"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "@Microsoft.KeyVault(SecretUri=https://banksystem-kv.vault.azure.net/secrets/database-connection-string/)"
  },
  "WebApiDefaults": {
    "HealthChecks": {
      "Path": "/health",
      "DetailedPath": "/health/detailed"
    }
  },
  "ApplicationInsights": {
    "InstrumentationKey": "@Microsoft.KeyVault(SecretUri=https://banksystem-kv.vault.azure.net/secrets/appinsights-key/)"
  }
}
```

### Azure Container Apps Configuration

```json
{
  "InterServiceSecurity": {
    "Grpc": {
      "MaxMessageSize": 4194304
    },
    "Authentication": {
      "AllowedServices": ["security-api", "account-api", "movement-api"]
    }
  },
  "WebApiDefaults": {
    "Cors": {
      "AllowedOrigins": [
        "https://bankapp-frontend.proudocean-12345.eastus.azurecontainerapps.io"
      ]
    }
  }
}
```

## Docker Configuration

### Docker Compose Environment Variables

```yaml
# docker-compose.yml
services:
  security-api:
    environment:
      - InterServiceSecurity__ApiKey__Value=docker-dev-key
      - InterServiceSecurity__Authentication__AllowedServices__0=account-api
      - InterServiceSecurity__Authentication__AllowedServices__1=movement-api
      - WebApiDefaults__EnableSwagger=true
      - WebApiDefaults__Cors__AllowedOrigins__0=http://localhost:3000
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=BankSystem;User Id=sa;Password=YourStrong@Passw0rd
```

### Environment Variable Mapping

| Configuration Path                     | Environment Variable                      |
| -------------------------------------- | ----------------------------------------- |
| `InterServiceSecurity:ApiKey:Value`    | `InterServiceSecurity__ApiKey__Value`     |
| `WebApiDefaults:EnableSwagger`         | `WebApiDefaults__EnableSwagger`           |
| `ConnectionStrings:DefaultConnection`  | `ConnectionStrings__DefaultConnection`    |
| `WebApiDefaults:Cors:AllowedOrigins:0` | `WebApiDefaults__Cors__AllowedOrigins__0` |

## Validation Rules

### Configuration Validation

The library includes built-in validation for configuration values:

```csharp
// InterServiceSecurityOptions validation
public class InterServiceSecurityOptionsValidator : IValidateOptions<InterServiceSecurityOptions>
{
    public ValidateOptionsResult Validate(string name, InterServiceSecurityOptions options)
    {
        var failures = new List<string>();

        // Validate API Key
        if (string.IsNullOrEmpty(options.ApiKey?.Value))
        {
            failures.Add("InterServiceSecurity:ApiKey:Value is required");
        }
        else if (options.ApiKey.Value.Length < 32)
        {
            failures.Add("InterServiceSecurity:ApiKey:Value must be at least 32 characters");
        }

        // Validate allowed services
        if (!options.Authentication?.AllowedServices?.Any() == true)
        {
            failures.Add("InterServiceSecurity:Authentication:AllowedServices must contain at least one service");
        }

        // Validate gRPC message size
        if (options.Grpc?.MaxMessageSize <= 0)
        {
            failures.Add("InterServiceSecurity:Grpc:MaxMessageSize must be greater than 0");
        }

        return failures.Any()
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
```

### Required vs Optional Settings

#### Required Settings

- `InterServiceSecurity:ApiKey:Value`
- `InterServiceSecurity:Authentication:AllowedServices`
- `ConnectionStrings:DefaultConnection`

#### Optional Settings (with defaults)

- `InterServiceSecurity:Grpc:MaxMessageSize` (default: 4194304)
- `InterServiceSecurity:ApiKey:HeaderName` (default: "X-Service-ApiKey")
- `WebApiDefaults:EnableSwagger` (default: true in dev, false in prod)
- `WebApiDefaults:RateLimit:GlobalLimiter:PermitLimit` (default: 100)

## Configuration Best Practices

### 1. Secret Management

```csharp
// Use Azure Key Vault for production secrets
"InterServiceSecurity": {
  "ApiKey": {
    "Value": "@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/api-key/)"
  }
}

// Use user secrets for development
// dotnet user-secrets set "InterServiceSecurity:ApiKey:Value" "dev-key-value"
```

### 2. Environment-Specific Overrides

```json
// appsettings.json (base configuration)
{
  "WebApiDefaults": {
    "EnableSwagger": false,
    "RateLimit": {
      "GlobalLimiter": {
        "PermitLimit": 100
      }
    }
  }
}

// appsettings.Development.json (overrides for development)
{
  "WebApiDefaults": {
    "EnableSwagger": true,
    "RateLimit": {
      "GlobalLimiter": {
        "PermitLimit": 1000
      }
    }
  }
}
```

### 3. Configuration Validation

```csharp
// Validate configuration at startup
builder.Services.AddOptions<InterServiceSecurityOptions>()
    .Bind(builder.Configuration.GetSection("InterServiceSecurity"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<WebApiDefaultsOptions>()
    .Bind(builder.Configuration.GetSection("WebApiDefaults"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 4. Monitoring Configuration Changes

```csharp
// Use IOptionsMonitor for runtime configuration changes
public class ConfigurableService
{
    private readonly IOptionsMonitor<InterServiceSecurityOptions> _options;

    public ConfigurableService(IOptionsMonitor<InterServiceSecurityOptions> options)
    {
        _options = options;
        _options.OnChange(OnConfigurationChanged);
    }

    private void OnConfigurationChanged(InterServiceSecurityOptions newOptions)
    {
        // Handle configuration changes
        _logger.LogInformation("Configuration updated");
    }
}
```

## Troubleshooting Configuration

### Common Configuration Errors

1. **Missing Required Values**

   - Error: `InterServiceSecurity:ApiKey:Value is required`
   - Solution: Set the API key value in configuration

2. **Invalid Time Spans**

   - Error: `Unable to parse TimeSpan value`
   - Solution: Use format like `"00:01:00"` for 1 minute

3. **Array Configuration**
   - Error: `AllowedServices is null`
   - Solution: Use array syntax in JSON or environment variables

### Debug Configuration

```csharp
// Log configuration values (excluding secrets)
public void LogConfiguration(IConfiguration configuration)
{
    _logger.LogInformation("Current configuration:");
    _logger.LogInformation("Swagger enabled: {SwaggerEnabled}",
        configuration["WebApiDefaults:EnableSwagger"]);
    _logger.LogInformation("CORS enabled: {CorsEnabled}",
        configuration["WebApiDefaults:EnableCors"]);
    _logger.LogInformation("Allowed services count: {ServiceCount}",
        configuration.GetSection("InterServiceSecurity:Authentication:AllowedServices").GetChildren().Count());
}
```

### Configuration Binding Issues

```csharp
// Verify configuration binding
var options = new InterServiceSecurityOptions();
configuration.GetSection("InterServiceSecurity").Bind(options);

// Check if values are properly bound
if (string.IsNullOrEmpty(options.ApiKey?.Value))
{
    throw new InvalidOperationException("API Key not configured properly");
}
```
