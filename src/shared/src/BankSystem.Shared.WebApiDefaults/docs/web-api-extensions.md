# Web API Extensions Guide

## Overview

The `AddWebApiDefaults` extension method provides a comprehensive setup for REST API services with authentication, validation, versioning, CORS, rate limiting, and more.

## Basic Usage

### Simple Setup

```csharp
// Program.cs - Minimal setup
var builder = WebApplication.CreateBuilder(args);

// Add all web API defaults with configuration
builder.Services.AddWebApiDefaults(builder.Configuration);

var app = builder.Build();

// Configure pipeline
app.UseWebApiDefaults();

await app.RunAsync();
```

### With Custom Configuration

```csharp
// Program.cs - Custom configuration
var builder = WebApplication.CreateBuilder(args);

// Add web API defaults with options
builder.Services.AddWebApiDefaults(builder.Configuration, options =>
{
    options.EnableSwagger = true;
    options.EnableCors = true;
    options.EnableRateLimiting = true;
    options.EnableCompression = true;
});

// Additional service registrations
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

var app = builder.Build();

// Use defaults pipeline
app.UseWebApiDefaults();

await app.RunAsync();
```

## Included Features

### 1. Controllers & Model Binding

Automatic configuration for:

- **Controller registration** with dependency injection
- **Model binding** with validation
- **JSON serialization** with optimized settings
- **Custom JSON converters** (Guid, DateTime)

```csharp
// JSON configuration applied automatically
services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new GuidJsonConverter());
    options.SerializerOptions.WriteIndented = !isProduction;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
```

### 2. API Versioning

Automatic API versioning setup:

```csharp
// Automatic configuration
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("version"),
        new HeaderApiVersionReader("X-Version")
    );
});

// Usage in controllers
[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/accounts")]
public class AccountController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<AccountDto>> GetAccountV1(Guid id)
    {
        // Version 1 implementation
    }

    [HttpGet]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<AccountDtoV2>> GetAccountV2(Guid id)
    {
        // Version 2 implementation
    }
}
```

### 3. CORS Configuration

Flexible CORS setup:

```csharp
// Configuration
{
  "WebApiDefaults": {
    "Cors": {
      "PolicyName": "DefaultCorsPolicy",
      "AllowedOrigins": [
        "http://localhost:3000",
        "https://localhost:3000",
        "https://bankapp.azurewebsites.net"
      ],
      "AllowedHeaders": ["*"],
      "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
      "AllowCredentials": true,
      "MaxAge": 300
    }
  }
}

// Usage
[EnableCors("DefaultCorsPolicy")]
[ApiController]
public class AccountController : ControllerBase
{
    // Controller methods
}
```

### 4. Rate Limiting

Built-in rate limiting with multiple algorithms:

```csharp
// Configuration options
{
  "WebApiDefaults": {
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
          "Window": "00:01:00"
        },
        {
          "Name": "PublicApi",
          "PermitLimit": 50,
          "Window": "00:01:00"
        }
      ]
    }
  }
}

// Usage in controllers
[EnableRateLimiting("AuthenticatedUsers")]
[HttpGet]
public async Task<ActionResult<AccountDto>> GetAccount(Guid id)
{
    // Method implementation
}

[EnableRateLimiting("PublicApi")]
[HttpGet("public/rates")]
public async Task<ActionResult<ExchangeRatesDto>> GetExchangeRates()
{
    // Public method with stricter limits
}
```

### 5. Response Compression

Automatic response compression:

```csharp
// Enabled automatically with optimal settings
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Compression levels optimized for production
services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
```

### 6. Health Checks

Comprehensive health check setup:

```csharp
// Automatic registration
services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddDbContext<AppDbContext>()
    .AddUrlGroup(new Uri("https://api.example.com/health"), "external-api");

// Endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### 7. API Documentation (Swagger/Scalar)

Modern API documentation:

```csharp
// Scalar UI (modern alternative to Swagger UI)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference(); // Modern API explorer
}

// XML documentation integration
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bank System API",
        Version = "v1"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});
```

## Configuration Options

### Complete Configuration Example

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
      "AllowedOrigins": ["http://localhost:3000", "https://localhost:3000"],
      "AllowedHeaders": ["*"],
      "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
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
          "Window": "00:01:00"
        }
      ]
    },
    "Swagger": {
      "Title": "Bank System API",
      "Version": "v1",
      "Description": "RESTful API for banking operations",
      "ContactName": "Development Team",
      "ContactEmail": "dev@bankystem.com"
    },
    "HealthChecks": {
      "Path": "/health",
      "DetailedPath": "/health/detailed",
      "ReadinessPath": "/health/ready"
    }
  }
}
```

### Environment-Specific Settings

```json
// appsettings.Development.json
{
  "WebApiDefaults": {
    "EnableSwagger": true,
    "Cors": {
      "AllowedOrigins": [
        "http://localhost:3000",
        "http://localhost:3001",
        "http://127.0.0.1:3000"
      ]
    },
    "RateLimit": {
      "GlobalLimiter": {
        "PermitLimit": 1000,
        "Window": "00:01:00"
      }
    }
  }
}

// appsettings.Production.json
{
  "WebApiDefaults": {
    "EnableSwagger": false,
    "Cors": {
      "AllowedOrigins": [
        "https://bankapp.azurewebsites.net",
        "https://mobile.bankapp.com"
      ]
    },
    "RateLimit": {
      "GlobalLimiter": {
        "PermitLimit": 100,
        "Window": "00:01:00"
      }
    }
  }
}
```

## Advanced Usage

### Custom Exception Handling

```csharp
// Automatic registration of global exception handler
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        var response = exception switch
        {
            ValidationException ex => new {
                type = "validation_error",
                title = "Validation Failed",
                status = 400,
                errors = ex.Errors
            },
            NotFoundException => new {
                type = "not_found",
                title = "Resource Not Found",
                status = 404
            },
            _ => new {
                type = "server_error",
                title = "Server Error",
                status = 500
            }
        };

        httpContext.Response.StatusCode = response.status;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response), cancellationToken);

        return true;
    }
}
```

### Custom Middleware Integration

```csharp
// Add custom middleware with defaults
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebApiDefaults(builder.Configuration);

// Add custom middleware
builder.Services.AddScoped<RequestLoggingMiddleware>();
builder.Services.AddScoped<PerformanceMiddleware>();

var app = builder.Build();

// Custom middleware before defaults
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<PerformanceMiddleware>();

// Use defaults (includes auth, CORS, etc.)
app.UseWebApiDefaults();

// Custom middleware after defaults
app.UseMiddleware<BusinessRuleMiddleware>();

app.MapControllers();
app.Run();
```

### Custom Authorization Policies

```csharp
// Register custom authorization policies
builder.Services.AddAuthorization(options =>
{
    // Use built-in policies from PolicyConstants
    options.AddPolicy(PolicyConstants.CustomerAccess, policy =>
        policy.RequireRole("Customer")
              .RequireAuthenticatedUser());

    options.AddPolicy(PolicyConstants.AdminAccess, policy =>
        policy.RequireRole("Admin", "SuperAdmin")
              .RequireAuthenticatedUser());

    // Custom policies
    options.AddPolicy("HighValueTransaction", policy =>
        policy.RequireRole("Manager", "Admin")
              .RequireClaim("TransactionLimit", "High"));

    options.AddPolicy("InternalApi", policy =>
        policy.RequireClaim("ApiType", "Internal"));
});

// Usage in controllers
[Authorize(Policy = PolicyConstants.AdminAccess)]
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteAccount(Guid id)
{
    // Admin-only operation
}

[Authorize(Policy = "HighValueTransaction")]
[HttpPost("transfer/high-value")]
public async Task<ActionResult> ProcessHighValueTransfer(TransferRequest request)
{
    // High-value transfer operation
}
```

## Integration with Other Services

### Service Registration Pattern

```csharp
// AccountService/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add shared defaults
builder.Services.AddWebApiDefaults(builder.Configuration);

// Add service-specific registrations
builder.Services.AddAccountServices(builder.Configuration);

var app = builder.Build();

// Use shared defaults
app.UseWebApiDefaults();

// Service-specific middleware
app.UseAccountMiddleware();

// Map controllers
app.MapControllers();

app.Run();

// Service-specific extensions
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccountServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAccountRepository, AccountRepository>();

        services.AddDbContext<AccountDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
```

### gRPC Integration

```csharp
// Combined REST API + gRPC
var builder = WebApplication.CreateBuilder(args);

// Add web API defaults
builder.Services.AddWebApiDefaults(builder.Configuration);

// Add gRPC services
builder.Services.AddInterServiceGrpc(builder.Configuration);

var app = builder.Build();

// Use web API defaults
app.UseWebApiDefaults();

// Map REST controllers
app.MapControllers();

// Map gRPC services
app.MapGrpcService<AccountGrpcService>();
app.MapGrpcService<TransactionGrpcService>();

app.Run();
```

## Testing

### Integration Testing Setup

```csharp
public class WebApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WebApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAccounts_ReturnsOkResult()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer valid-jwt-token");

        // Act
        var response = await _client.GetAsync("/api/v1/accounts");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8",
            response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task GetAccounts_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

### Load Testing

```csharp
// Example for testing rate limiting
[Fact]
public async Task RateLimiting_ExceedsLimit_Returns429()
{
    // Arrange
    var tasks = new List<Task<HttpResponseMessage>>();

    // Act - send requests beyond rate limit
    for (int i = 0; i < 101; i++)
    {
        tasks.Add(_client.GetAsync("/api/v1/accounts"));
    }

    var responses = await Task.WhenAll(tasks);

    // Assert
    var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
    Assert.True(rateLimitedResponses > 0);
}
```

## Troubleshooting

### Common Issues

1. **CORS Errors**: Check allowed origins configuration
2. **Rate Limiting**: Verify policy configuration and limits
3. **Authentication**: Ensure proper JWT configuration
4. **Versioning**: Check API version headers and routing

### Debug Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Cors": "Information",
      "Microsoft.AspNetCore.RateLimiting": "Information",
      "Microsoft.AspNetCore.Authentication": "Information",
      "Microsoft.AspNetCore.Authorization": "Information",
      "BankSystem.Shared.WebApiDefaults": "Debug"
    }
  }
}
```

### Health Check Debugging

```bash
# Check basic health
curl http://localhost:5000/health

# Check detailed health
curl http://localhost:5000/health/detailed

# Check readiness
curl http://localhost:5000/health/ready
```
