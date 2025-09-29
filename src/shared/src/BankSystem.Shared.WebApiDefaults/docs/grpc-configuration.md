# gRPC Configuration Guide

## Overview

This library provides complete gRPC setup with authentication, interceptors, and service registration for inter-service communication in the Bank System.

## Basic Setup

### Service Registration

```csharp
// Program.cs
builder.Services.AddInterServiceGrpc(builder.Configuration);

var app = builder.Build();

// Register your gRPC services
app.MapGrpcService<UserContactGrpcService>();
app.MapGrpcService<AccountGrpcService>();

// Optional: Enable gRPC reflection for development
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.Run();
```

### gRPC Service Implementation

```csharp
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

[Authorize(Policy = "InterServiceApiKey")]
public class UserContactGrpcService : UserContactGrpc.UserContactGrpcBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserContactGrpcService> _logger;

    public UserContactGrpcService(
        IUserService userService,
        ILogger<UserContactGrpcService> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public override async Task<GetUserContactInfoResponse> GetUserContactInfo(
        GetUserContactInfoRequest request,
        ServerCallContext context)
    {
        try
        {
            var user = await _userService.GetByIdAsync(Guid.Parse(request.UserId));

            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
            }

            return new GetUserContactInfoResponse
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user contact info for user {UserId}", request.UserId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
```

## Configuration Options

### Complete Configuration

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
      "AllowedServices": ["Movement.Api", "Account.Api", "Transaction.Api"]
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

### Configuration Properties

| Property               | Description                        | Default       | Environment             |
| ---------------------- | ---------------------------------- | ------------- | ----------------------- |
| `MaxMessageSize`       | Maximum gRPC message size in bytes | 4194304 (4MB) | All                     |
| `EnableDetailedErrors` | Show detailed error messages       | `false`       | Development only        |
| `Reflection.Enabled`   | Enable gRPC reflection service     | `false`       | Development recommended |

## Client Configuration

### .NET gRPC Client

```csharp
// Client service registration
services.AddGrpcClient<UserContactGrpc.UserContactGrpcClient>(options =>
{
    options.Address = new Uri("https://localhost:5153");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();

    // For development with self-signed certificates
    if (Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    return handler;
});

// Usage in service
public class MovementService
{
    private readonly UserContactGrpc.UserContactGrpcClient _userClient;

    public MovementService(UserContactGrpc.UserContactGrpcClient userClient)
    {
        _userClient = userClient;
    }

    public async Task<UserContactInfo> GetUserContactAsync(Guid userId)
    {
        var headers = new Metadata
        {
            { "X-Service-ApiKey", "dev-security-service-key-2024-banking-microservices" },
            { "X-Service-Name", "Movement.Api" }
        };

        try
        {
            var response = await _userClient.GetUserContactInfoAsync(
                new GetUserContactInfoRequest { UserId = userId.ToString() },
                headers: headers
            );

            return new UserContactInfo
            {
                UserId = Guid.Parse(response.UserId),
                Email = response.Email,
                FirstName = response.FirstName,
                LastName = response.LastName,
                PhoneNumber = response.PhoneNumber
            };
        }
        catch (RpcException ex)
        {
            // Handle gRPC specific errors
            throw new ServiceException($"Failed to get user contact: {ex.Status.Detail}", ex);
        }
    }
}
```

### External Client (Node.js, Python, etc.)

```javascript
// Node.js example using grpc-js
const grpc = require("@grpc/grpc-js");
const protoLoader = require("@grpc/proto-loader");

// Load proto file
const packageDefinition = protoLoader.loadSync("user_contact.proto");
const userContactProto =
  grpc.loadPackageDefinition(packageDefinition).BankSystem.Security.Grpc;

// Create client
const client = new userContactProto.UserContactGrpc(
  "localhost:5153",
  grpc.credentials.createInsecure()
);

// Create metadata with authentication
const metadata = new grpc.Metadata();
metadata.add(
  "X-Service-ApiKey",
  "dev-security-service-key-2024-banking-microservices"
);
metadata.add("X-Service-Name", "external-service");

// Make call
client.getUserContactInfo(
  { userId: "12345678-1234-1234-1234-123456789012" },
  metadata,
  (error, response) => {
    if (error) {
      console.error("Error:", error);
    } else {
      console.log("Response:", response);
    }
  }
);
```

## Authentication with gRPC

### Server-Side Authentication

The gRPC services are protected by the `InterServiceAuthenticationInterceptor`:

```csharp
// Automatic registration in AddInterServiceGrpc()
services.AddSingleton<InterServiceAuthenticationInterceptor>();
```

### Authentication Flow

1. **Request Received**: gRPC interceptor validates incoming requests
2. **API Key Validation**: Checks `X-Service-ApiKey` header
3. **Service Identification**: Extracts service name from headers or metadata
4. **Authorization Check**: Validates service is in allowed list
5. **Claims Creation**: Creates authentication claims for the request
6. **Request Processing**: Forwards to service method if authenticated

### Metadata vs Headers

| Type          | Usage              | Example                                     |
| ------------- | ------------------ | ------------------------------------------- |
| HTTP Headers  | REST API calls     | `X-Service-ApiKey: value`                   |
| gRPC Metadata | gRPC service calls | `metadata.Add("X-Service-ApiKey", "value")` |

## Error Handling

### gRPC Status Codes

```csharp
public override async Task<GetUserResponse> GetUser(GetUserRequest request, ServerCallContext context)
{
    try
    {
        // Your logic
    }
    catch (NotFoundException)
    {
        throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
    }
    catch (ValidationException ex)
    {
        throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
    }
    catch (UnauthorizedException)
    {
        throw new RpcException(new Status(StatusCode.PermissionDenied, "Access denied"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error");
        throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
    }
}
```

### Client Error Handling

```csharp
try
{
    var response = await client.GetUserAsync(request, headers: headers);
    return response;
}
catch (RpcException ex)
{
    return ex.StatusCode switch
    {
        StatusCode.NotFound => Result<UserResponse>.Failure("User not found"),
        StatusCode.InvalidArgument => Result<UserResponse>.Failure("Invalid request"),
        StatusCode.Unauthenticated => Result<UserResponse>.Failure("Authentication failed"),
        StatusCode.PermissionDenied => Result<UserResponse>.Failure("Access denied"),
        _ => Result<UserResponse>.Failure("Service unavailable")
    };
}
```

## Testing gRPC Services

### Using Postman

1. **Enable gRPC**: Postman supports gRPC requests
2. **Import Proto**: Import your `.proto` files
3. **Set Headers**: Add authentication metadata
4. **Test Methods**: Call service methods directly

### Using grpcui

```bash
# Install grpcui
go install github.com/fullstorydev/grpcui/cmd/grpcui@latest

# Connect to service (with reflection enabled)
grpcui -plaintext localhost:5153
```

### Using ApiDog

1. **Create gRPC Request**
2. **Set Server URL**: `localhost:5153`
3. **Add Metadata**:
   - `X-Service-ApiKey`: `dev-security-service-key-2024-banking-microservices`
   - `X-Service-Name`: `test-client`
4. **Call Methods**: Test your gRPC services

## Performance Considerations

### Message Size Limits

```csharp
// Increase message size for large payloads
services.Configure<GrpcServiceOptions>(options =>
{
    options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16MB
    options.MaxSendMessageSize = 16 * 1024 * 1024;    // 16MB
});
```

### Connection Pooling

```csharp
// Configure HTTP/2 connection pooling
services.AddGrpcClient<UserContactGrpc.UserContactGrpcClient>(options =>
{
    options.Address = new Uri("https://localhost:5153");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new SocketsHttpHandler
    {
        PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
        EnableMultipleHttp2Connections = true
    };
});
```

### Streaming

```csharp
// Server streaming example
public override async Task GetUserStream(
    GetUserStreamRequest request,
    IServerStreamWriter<UserResponse> responseStream,
    ServerCallContext context)
{
    var users = await _userService.GetUsersAsync(request.Filter);

    foreach (var user in users)
    {
        await responseStream.WriteAsync(new UserResponse
        {
            UserId = user.Id.ToString(),
            // ... other properties
        });
    }
}
```

## Troubleshooting

### Common Issues

1. **Connection Refused**: Check if service is running and port is correct
2. **Authentication Failed**: Verify API key and service name headers
3. **Message Too Large**: Increase `MaxMessageSize` configuration
4. **Certificate Errors**: Configure certificate validation for development

### Debug Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Grpc": "Debug",
      "Grpc.AspNetCore.Server": "Information",
      "BankSystem.Shared.WebApiDefaults.Interceptors": "Debug"
    }
  }
}
```

### Health Checks

```csharp
// Add gRPC health checks
services.AddGrpcHealthChecks()
    .AddCheck("grpc_health", () => HealthCheckResult.Healthy());

// Use in client
var healthClient = new Health.HealthClient(channel);
var healthResponse = await healthClient.CheckAsync(new HealthCheckRequest());
```
