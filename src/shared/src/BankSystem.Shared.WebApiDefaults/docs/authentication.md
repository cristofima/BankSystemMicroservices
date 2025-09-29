# Authentication Guide

## Overview

The authentication system in `BankSystem.Shared.WebApiDefaults` provides secure inter-service communication using two methods:

- **API Key Authentication** (Development/Testing)
- **mTLS Authentication** (Production)

## API Key Authentication

### Configuration

```json
{
  "InterServiceSecurity": {
    "Authentication": {
      "Method": "ApiKey",
      "RequiredScope": "inter-service",
      "AllowedServices": [
        "Movement.Api",
        "Account.Api",
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
    }
  }
}
```

### Service Registration

```csharp
// Program.cs
builder.Services.AddInterServiceGrpc(builder.Configuration);

// For Web API controllers
builder.Services.AddWebApiDefaults(builder.Configuration);
```

### Usage in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "InterServiceApiKey")]
public class UserController : ControllerBase
{
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid userId)
    {
        // Your implementation
    }
}
```

### Usage in gRPC Services

```csharp
[Authorize(Policy = "InterServiceApiKey")]
public class UserContactGrpcService : UserContactGrpc.UserContactGrpcBase
{
    public override async Task<GetUserContactInfoResponse> GetUserContactInfo(
        GetUserContactInfoRequest request,
        ServerCallContext context)
    {
        // Your implementation
    }
}
```

## Service Name Resolution

The authentication system identifies calling services using multiple strategies:

### 1. X-Service-Name Header (Recommended)

```http
X-Service-ApiKey: dev-security-service-key-2024-banking-microservices
X-Service-Name: Movement.Api
```

### 2. User-Agent Header

```http
X-Service-ApiKey: dev-security-service-key-2024-banking-microservices
User-Agent: Movement.Api/1.0.0
```

### 3. gRPC Metadata (for gRPC clients)

```csharp
var headers = new Metadata
{
    { "X-Service-ApiKey", "dev-security-service-key-2024-banking-microservices" },
    { "X-Service-Name", "Movement.Api" }
};
```

## Testing Authentication

### Using HTTP Client

```http
POST /grpc/BankSystem.Security.Grpc.UserContactGrpcService/GetUserContactInfo
Host: localhost:5153
Content-Type: application/grpc-web+proto
X-Service-ApiKey: dev-security-service-key-2024-banking-microservices
X-Service-Name: Movement.Api

{
  "userId": "12345678-1234-1234-1234-123456789012"
}
```

### Using .NET HttpClient

```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-Service-ApiKey", "dev-security-service-key-2024-banking-microservices");
client.DefaultRequestHeaders.Add("X-Service-Name", "Movement.Api");

var response = await client.GetAsync("https://localhost:5153/api/users/123");
```

### Using gRPC Client

```csharp
var channel = GrpcChannel.ForAddress("https://localhost:5153");
var client = new UserContactGrpc.UserContactGrpcClient(channel);

var headers = new Metadata
{
    { "X-Service-ApiKey", "dev-security-service-key-2024-banking-microservices" },
    { "X-Service-Name", "Movement.Api" }
};

var response = await client.GetUserContactInfoAsync(
    new GetUserContactInfoRequest { UserId = "123" },
    headers: headers
);
```

## mTLS Authentication (Production)

### Configuration

```json
{
  "InterServiceSecurity": {
    "Authentication": {
      "Method": "MTls"
    },
    "MTls": {
      "Enabled": true,
      "ServerCertificatePath": "/certificates/server.pfx",
      "ClientCertificatePath": "/certificates/client.pfx",
      "ValidateClientCertificate": true
    }
  }
}
```

### Azure Key Vault Integration

```json
{
  "InterServiceSecurity": {
    "MTls": {
      "AzureKeyVault": {
        "Enabled": true,
        "VaultUrl": "https://your-keyvault.vault.azure.net/",
        "ServerCertificateName": "server-certificate",
        "ClientCertificateName": "client-certificate"
      }
    }
  }
}
```

## Security Considerations

### Development Environment

- Use API Key authentication for simplicity
- Store API keys in user secrets or environment variables
- Never commit API keys to source control

### Production Environment

- Use mTLS for maximum security
- Store certificates in Azure Key Vault
- Implement certificate rotation
- Monitor certificate expiration

### Allowed Services

- Maintain a strict list of allowed services
- Remove unused services from the allowed list
- Use specific service names, avoid wildcards
- Review the list regularly

## Troubleshooting

### Common Issues

1. **401 Unauthorized**: Check API key value and header name
2. **Service 'Unknown' not allowed**: Verify service name headers
3. **Policy not found**: Ensure authorization policies are registered
4. **Certificate errors**: Validate certificate paths and permissions

### Debugging Tips

```csharp
// Enable detailed logging
"Logging": {
  "LogLevel": {
    "BankSystem.Shared.WebApiDefaults.Authentication": "Debug",
    "BankSystem.Shared.WebApiDefaults.Interceptors": "Debug"
  }
}
```

### Log Examples

```
[DBG] API key authentication successful for service 'Movement.Api'
[WRN] Service 'UnknownService' is not in the allowed services list
[INF] InterServiceApiKey was not authenticated. Failure message: Invalid API key
```

## API Reference

### ApiKeyAuthenticationHandler

- Validates API keys using constant-time comparison
- Extracts service names from multiple sources
- Creates claims for authenticated requests

### InterServiceAuthenticationInterceptor

- gRPC interceptor for authentication validation
- Supports both API Key and mTLS methods
- Adds authentication context to gRPC calls

### ApiKeyAuthenticationSchemeOptions

- Configuration options for API key authentication
- Includes validation methods
- Supports custom header names and values
