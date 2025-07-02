# API Design Guidelines

## Overview

This document provides comprehensive guidelines for designing RESTful APIs in the Bank System Microservices project, following Microsoft's API design best practices, ASP.NET Core 9.0 recommendations, and OWASP security guidelines.

## RESTful Design Principles

REST (Representational State Transfer) APIs should adhere to the following core principles:

- **Platform Independence**: Clients can call the API regardless of internal implementation
- **Loose Coupling**: Client and service can evolve independently
- **Stateless**: Each request contains all information needed to process it
- **Uniform Interface**: Use standard HTTP verbs and consistent resource representations
- **Resource-Based**: APIs are organized around resources (business entities)

## RESTful API Design Principles

### Resource Naming

- **Use nouns, not verbs** for resource names
- **Use plural nouns** for collections
- **Use hierarchical structure** for related resources
- **Use lowercase letters and hyphens** for multi-word names

```csharp
// ✅ Good: Proper resource naming
[Route("api/accounts")]                    // Collection
[Route("api/accounts/{accountId}")]        // Individual resource
[Route("api/accounts/{accountId}/transactions")]  // Sub-resource
[Route("api/customers/{customerId}/accounts")]     // Hierarchical

// ❌ Bad: Poor resource naming
[Route("api/getAccounts")]                 // Verb in URL
[Route("api/Account")]                     // Singular noun
[Route("api/accounts/{accountId}/getTransactions")] // Verb in URL
```

### HTTP Methods Usage

```csharp
[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    // GET - Retrieve resource(s)
    [HttpGet]
    public async Task<ActionResult<PagedResult<AccountDto>>> GetAccounts(
        [FromQuery] GetAccountsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{accountId:guid}")]
    public async Task<ActionResult<AccountDto>> GetAccount(Guid accountId)
    {
        var query = new GetAccountByIdQuery(accountId);
        var result = await _mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound();
    }

    // POST - Create new resource
    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateAccount(
        [FromBody] CreateAccountCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(
            nameof(GetAccount),
            new { accountId = result.Value.Id },
            result.Value);
    }

    // PUT - Update entire resource
    [HttpPut("{accountId:guid}")]
    public async Task<ActionResult<AccountDto>> UpdateAccount(
        Guid accountId,
        [FromBody] UpdateAccountCommand command)
    {
        if (accountId != command.AccountId)
            return BadRequest("Account ID mismatch");

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound();
    }

    // PATCH - Partial update
    [HttpPatch("{accountId:guid}/status")]
    public async Task<ActionResult> UpdateAccountStatus(
        Guid accountId,
        [FromBody] UpdateAccountStatusCommand command)
    {
        command = command with { AccountId = accountId };
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? NoContent()
            : NotFound();
    }

    // DELETE - Remove resource
    [HttpDelete("{accountId:guid}")]
    public async Task<ActionResult> DeleteAccount(Guid accountId)
    {
        var command = new DeleteAccountCommand(accountId);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? NoContent()
            : NotFound();
    }
}
```

## HTTP Status Codes

### Success Responses

```csharp
// 200 OK - Successful GET, PUT, PATCH
return Ok(result);

// 201 Created - Successful POST
return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);

// 202 Accepted - Asynchronous processing started
return Accepted();

// 204 No Content - Successful DELETE or update with no response body
return NoContent();
```

### Client Error Responses

```csharp
// 400 Bad Request - Invalid request data
if (!ModelState.IsValid)
    return BadRequest(ModelState);

// 401 Unauthorized - Authentication required
return Unauthorized();

// 403 Forbidden - Access denied
return Forbid();

// 404 Not Found - Resource doesn't exist
if (account == null)
    return NotFound();

// 409 Conflict - Resource conflict
if (accountExists)
    return Conflict("Account already exists");

// 422 Unprocessable Entity - Validation errors
return UnprocessableEntity(validationErrors);
```

### Server Error Responses

```csharp
// 500 Internal Server Error - Handled by global exception middleware
// Don't return these directly from controllers
```

## Input Validation

### Model Validation

```csharp
public record CreateAccountRequest
{
    [Required(ErrorMessage = "Customer ID is required")]
    public Guid CustomerId { get; init; }

    [Required(ErrorMessage = "Account type is required")]
    [EnumDataType(typeof(AccountType), ErrorMessage = "Invalid account type")]
    public AccountType AccountType { get; init; }

    [Range(0.01, 1000000, ErrorMessage = "Initial deposit must be between $0.01 and $1,000,000")]
    public decimal InitialDeposit { get; init; }

    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be 3 characters")]
    public string Currency { get; init; } = string.Empty;
}

[ApiController]
public class AccountController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateAccount(
        [FromBody] CreateAccountRequest request)
    {
        // Model validation is automatic with [ApiController]
        // Additional custom validation if needed
        if (!Currency.IsValidCode(request.Currency))
            return BadRequest("Invalid currency code");

        var command = new CreateAccountCommand(
            request.CustomerId,
            request.AccountType,
            request.InitialDeposit,
            request.Currency);

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAccount), new { accountId = result.Value.Id }, result.Value)
            : BadRequest(result.Error);
    }
}
```

### FluentValidation Integration

```csharp
public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be positive")
            .LessThanOrEqualTo(50000)
            .WithMessage("Amount cannot exceed $50,000");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Currency)
            .Must(BeValidCurrency)
            .WithMessage("Invalid currency code");
    }

    private bool BeValidCurrency(string currency)
    {
        return Currency.IsValidCode(currency);
    }
}
```

## Response Formatting

### Consistent Response Models

```csharp
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record PagedResult<T>
{
    public IEnumerable<T> Data { get; init; } = Enumerable.Empty<T>();
    public PaginationInfo Pagination { get; init; } = new();
}

public record PaginationInfo
{
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalRecords { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

// Usage in controller
[HttpGet]
public async Task<ActionResult<PagedResult<AccountDto>>> GetAccounts(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50)
{
    var query = new GetAccountsQuery(page, pageSize);
    var result = await _mediator.Send(query);

    return Ok(result);
}
```

### Error Response Format

```csharp
public record ErrorResponse
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string Instance { get; init; } = string.Empty;
    public Dictionary<string, object> Extensions { get; init; } = new();
}

// Global exception handling middleware
public class ExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Validation Error",
                Status = 400,
                Detail = validationEx.Message,
                Instance = context.Request.Path,
                Extensions = { ["errors"] = validationEx.Errors }
            },
            NotFoundException => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Resource Not Found",
                Status = 404,
                Detail = exception.Message,
                Instance = context.Request.Path
            },
            _ => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Status = 500,
                Detail = "An unexpected error occurred",
                Instance = context.Request.Path
            }
        };

        context.Response.StatusCode = response.Status;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
```

## Versioning

### URL Versioning

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/accounts")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public class AccountController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<IEnumerable<AccountDtoV1>>> GetAccountsV1()
    {
        // Version 1 implementation
    }

    [HttpGet]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<PagedResult<AccountDtoV2>>> GetAccountsV2(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // Version 2 implementation with pagination
    }
}

// Startup configuration
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("version"),
        new HeaderApiVersionReader("X-Version"));
});
```

## Content Negotiation

### Accept Headers

```csharp
[HttpGet("{accountId:guid}/statement")]
[Produces("application/json", "application/pdf")]
public async Task<ActionResult> GetAccountStatement(
    Guid accountId,
    [FromQuery] int year,
    [FromQuery] int month)
{
    var query = new GetAccountStatementQuery(accountId, year, month);
    var statement = await _mediator.Send(query);

    return Request.Headers.Accept.Any(h => h.MediaType == "application/pdf")
        ? File(statement.PdfData, "application/pdf", $"statement-{accountId}-{year}-{month}.pdf")
        : Ok(statement.Data);
}
```

## Caching

### Response Caching

```csharp
[HttpGet("{accountId:guid}")]
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "accountId" })]
public async Task<ActionResult<AccountDto>> GetAccount(Guid accountId)
{
    var query = new GetAccountByIdQuery(accountId);
    var result = await _mediator.Send(query);

    if (!result.IsSuccess)
        return NotFound();

    // Add cache headers
    Response.Headers.ETag = $"\"{result.Value.Version}\"";
    Response.Headers.LastModified = result.Value.UpdatedAt.ToString("R");

    return Ok(result.Value);
}
```

### ETags for Conditional Requests

```csharp
[HttpPut("{accountId:guid}")]
public async Task<ActionResult<AccountDto>> UpdateAccount(
    Guid accountId,
    [FromBody] UpdateAccountCommand command,
    [FromHeader(Name = "If-Match")] string? ifMatch = null)
{
    // Check ETag for optimistic concurrency
    if (!string.IsNullOrEmpty(ifMatch))
    {
        var currentAccount = await _mediator.Send(new GetAccountByIdQuery(accountId));
        if (currentAccount.IsSuccess && $"\"{currentAccount.Value.Version}\"" != ifMatch)
        {
            return StatusCode(412); // Precondition Failed
        }
    }

    var result = await _mediator.Send(command);

    return result.IsSuccess
        ? Ok(result.Value)
        : NotFound();
}
```

## Security Headers

### Controller Security Configuration

```csharp
[ApiController]
[Route("api/accounts")]
[Authorize] // Require authentication for all actions
[RequireHttps] // Enforce HTTPS
public class AccountController : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Manager,Admin")] // Role-based authorization
    public async Task<ActionResult<AccountDto>> CreateAccount(
        [FromBody] CreateAccountCommand command)
    {
        // Implementation
    }

    [HttpDelete("{accountId:guid}")]
    [Authorize(Policy = "CanDeleteAccounts")] // Policy-based authorization
    public async Task<ActionResult> DeleteAccount(Guid accountId)
    {
        // Implementation
    }

    [HttpGet]
    [AllowAnonymous] // Override controller-level authorization
    public async Task<ActionResult<IEnumerable<PublicAccountInfoDto>>> GetPublicAccountInfo()
    {
        // Public information that doesn't require authentication
    }
}
```

## Rate Limiting

### Action-Level Rate Limiting

```csharp
[HttpPost]
[EnableRateLimiting("TransactionPolicy")]
public async Task<ActionResult<TransactionDto>> CreateTransaction(
    [FromBody] CreateTransactionCommand command)
{
    // Implementation
}

// In Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("TransactionPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
});
```

## Documentation

### OpenAPI/Swagger Configuration

```csharp
[HttpPost]
[ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[SwaggerOperation(
    Summary = "Create a new account",
    Description = "Creates a new customer account with the specified details",
    OperationId = "CreateAccount",
    Tags = new[] { "Accounts" })]
public async Task<ActionResult<AccountDto>> CreateAccount(
    [FromBody, SwaggerRequestBody("Account creation details")] CreateAccountCommand command)
{
    // Implementation
}

// Model documentation
/// <summary>
/// Account creation request model
/// </summary>
public record CreateAccountRequest
{
    /// <summary>
    /// The unique identifier of the customer
    /// </summary>
    /// <example>123e4567-e89b-12d3-a456-426614174000</example>
    [Required]
    public Guid CustomerId { get; init; }

    /// <summary>
    /// The type of account to create
    /// </summary>
    /// <example>Checking</example>
    [Required]
    public AccountType AccountType { get; init; }

    /// <summary>
    /// Initial deposit amount (minimum $0.01)
    /// </summary>
    /// <example>100.00</example>
    [Range(0.01, double.MaxValue)]
    public decimal InitialDeposit { get; init; }
}
```

## Performance Optimization

### Async Best Practices in Controllers

```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<AccountDto>>> GetAccounts(
    [FromQuery] GetAccountsQuery query,
    CancellationToken cancellationToken)
{
    // Pass cancellation token through the chain
    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
}

[HttpPost("bulk")]
public async Task<ActionResult<BulkOperationResult>> ProcessBulkTransactions(
    [FromBody] IEnumerable<CreateTransactionCommand> commands,
    CancellationToken cancellationToken)
{
    // Process multiple operations concurrently
    var tasks = commands.Select(cmd => _mediator.Send(cmd, cancellationToken));
    var results = await Task.WhenAll(tasks);

    return Ok(new BulkOperationResult
    {
        TotalProcessed = results.Length,
        Successful = results.Count(r => r.IsSuccess),
        Failed = results.Count(r => !r.IsSuccess),
        Results = results
    });
}
```

## Security Best Practices (OWASP)

### HTTPS Only

All REST services must only provide HTTPS endpoints:

```csharp
// Program.cs - Enforce HTTPS
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
    app.UseHsts(); // HTTP Strict Transport Security
}

[ApiController]
[RequireHttps] // Enforce HTTPS at controller level
public class AccountController : ControllerBase
{
    // Controller actions
}
```

### Input Validation

Implement comprehensive input validation following OWASP guidelines:

```csharp
public record CreateTransactionRequest
{
    [Required(ErrorMessage = "Account ID is required")]
    public Guid AccountId { get; init; }

    [Range(0.01, 50000, ErrorMessage = "Amount must be between $0.01 and $50,000")]
    public decimal Amount { get; init; }

    [Required]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\.\-_]*$", ErrorMessage = "Description contains invalid characters")]
    public string Description { get; init; } = string.Empty;
}

[ApiController]
public class TransactionController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(
        [FromBody] CreateTransactionRequest request)
    {
        // Model validation is automatic with [ApiController]

        // Additional business validation
        if (await IsAccountSuspended(request.AccountId))
            return Forbid("Account is suspended");

        // Process request...
    }
}
```

### Security Headers

Implement security headers following OWASP recommendations:

```csharp
// Program.cs - Add security headers middleware
app.Use(async (context, next) =>
{
    // Cache control
    context.Response.Headers.Add("Cache-Control", "no-store");

    // Content security policy
    context.Response.Headers.Add("Content-Security-Policy", "frame-ancestors 'none'");

    // MIME type sniffing protection
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

    // Clickjacking protection
    context.Response.Headers.Add("X-Frame-Options", "DENY");

    // XSS protection
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

    await next();
});
```

### Content Type Validation

Validate request and response content types:

```csharp
[ApiController]
public class AccountController : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<AccountDto>> CreateAccount(
        [FromBody] CreateAccountCommand command)
    {
        // Automatically validates Content-Type is application/json
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

// Global content type validation
public class ContentTypeValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _allowedContentTypes = { "application/json" };

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.HasJsonContentType() ||
            context.Request.ContentLength == 0)
        {
            await _next(context);
        }
        else
        {
            context.Response.StatusCode = 415; // Unsupported Media Type
            await context.Response.WriteAsync("Unsupported content type");
        }
    }
}
```

### Rate Limiting

Implement rate limiting to prevent abuse:

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthenticationPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("TransactionPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
});

[EnableRateLimiting("TransactionPolicy")]
[HttpPost]
public async Task<ActionResult<TransactionDto>> CreateTransaction(
    [FromBody] CreateTransactionCommand command)
{
    // Implementation
}
```

### Error Handling Security

Implement secure error handling that doesn't expose sensitive information:

```csharp
public class SecureExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecureExceptionHandlingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log full exception details for debugging
        _logger.LogError(exception, "Unhandled exception occurred. Path: {Path}",
            context.Request.Path);

        var response = exception switch
        {
            ValidationException => new
            {
                Type = "validation_error",
                Title = "Validation Failed",
                Status = 400,
                Detail = "One or more validation errors occurred"
            },
            UnauthorizedAccessException => new
            {
                Type = "unauthorized",
                Title = "Unauthorized",
                Status = 401,
                Detail = "Authentication is required"
            },
            ArgumentException => new
            {
                Type = "bad_request",
                Title = "Bad Request",
                Status = 400,
                Detail = "The request was invalid"
            },
            _ => new
            {
                Type = "internal_error",
                Title = "Internal Server Error",
                Status = 500,
                Detail = "An unexpected error occurred"
            }
        };

        context.Response.StatusCode = response.Status;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
```

### Audit Logging

Implement comprehensive audit logging for security events:

```csharp
public class SecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;

    public void LogSuccessfulAuthentication(string userId, string ipAddress)
    {
        _logger.LogInformation("Successful authentication for user {UserId} from IP {IpAddress}",
            userId, ipAddress);
    }

    public void LogFailedAuthentication(string userIdentifier, string ipAddress, string reason)
    {
        _logger.LogWarning("Failed authentication attempt for {UserIdentifier} from IP {IpAddress}. Reason: {Reason}",
            userIdentifier, ipAddress, reason);
    }

    public void LogSensitiveDataAccess(string userId, string resourceType, string resourceId)
    {
        _logger.LogInformation("User {UserId} accessed {ResourceType} {ResourceId}",
            userId, resourceType, resourceId);
    }
}

[ApiController]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly SecurityAuditService _auditService;

    [HttpGet("{accountId:guid}")]
    public async Task<ActionResult<AccountDto>> GetAccount(Guid accountId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Audit sensitive data access
        _auditService.LogSensitiveDataAccess(userId, "Account", accountId.ToString());

        // Implementation...
    }
}
```

### JWT Security

Implement secure JWT handling:

```csharp
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32, ErrorMessage = "JWT key must be at least 32 characters")]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)] // 1 minute to 24 hours
    public int ExpiryInMinutes { get; set; } = 60;

    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
}

// Program.cs - JWT Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = jwtOptions.ValidateIssuer,
            ValidateAudience = jwtOptions.ValidateAudience,
            ValidateLifetime = jwtOptions.ValidateLifetime,
            ValidateIssuerSigningKey = jwtOptions.ValidateIssuerSigningKey,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.Zero // Remove default 5-minute tolerance
        };
    });
```

## Monitoring and Logging

### Controller Logging

```csharp
[ApiController]
public class AccountController : ControllerBase
{
    private readonly ILogger<AccountController> _logger;

    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateAccount(
        [FromBody] CreateAccountCommand command)
    {
        _logger.LogInformation("Creating account for customer {CustomerId}", command.CustomerId);

        try
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Account {AccountId} created successfully for customer {CustomerId}",
                    result.Value.Id, command.CustomerId);
                return CreatedAtAction(nameof(GetAccount), new { accountId = result.Value.Id }, result.Value);
            }

            _logger.LogWarning("Failed to create account for customer {CustomerId}: {Error}",
                command.CustomerId, result.Error);
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account for customer {CustomerId}", command.CustomerId);
            throw;
        }
    }
}
```

## Summary

1. **Follow RESTful conventions** for resource naming and HTTP methods
2. **Use appropriate HTTP status codes** for different scenarios
3. **Implement comprehensive input validation** at the API boundary
4. **Maintain consistent response formats** across all endpoints
5. **Version your APIs** to manage changes over time
6. **Secure your endpoints** with proper authentication and authorization
7. **Follow OWASP security guidelines** for input validation, error handling, and security headers
8. **Implement rate limiting** to prevent abuse and ensure fair usage
9. **Use HTTPS exclusively** for all endpoints in production
10. **Document your APIs** thoroughly with OpenAPI/Swagger
11. **Optimize for performance** with async operations and caching
12. **Monitor and log** API activity for troubleshooting and analytics
13. **Handle errors gracefully** with meaningful error responses
14. **Audit security events** for compliance and threat detection
