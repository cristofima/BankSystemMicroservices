# Security Policies and Authorization Guide

## Overview

This library provides comprehensive authorization policies for the Bank System, implementing role-based access control (RBAC) with predefined policies for different user types and operations.

## Built-in Authorization Policies

### Policy Constants

The `PolicyConstants` class defines all available authorization policies:

```csharp
public static class PolicyConstants
{
    // User role-based policies
    public const string CustomerAccess = "CustomerAccess";
    public const string AdminAccess = "AdminAccess";
    public const string ManagerAccess = "ManagerAccess";
    public const string TellerAccess = "TellerAccess";

    // Inter-service policies
    public const string InterServiceApiKey = "InterServiceApiKey";
    public const string InterServiceMTls = "InterServiceMTls";

    // Operation-specific policies
    public const string HighValueTransaction = "HighValueTransaction";
    public const string AccountManagement = "AccountManagement";
    public const string ReportingAccess = "ReportingAccess";
}
```

### Policy Definitions

#### 1. Customer Access Policy

```csharp
// Automatically configured
options.AddPolicy(PolicyConstants.CustomerAccess, policy =>
    policy.RequireAuthenticatedUser()
          .RequireRole("Customer")
          .RequireClaim("CustomerStatus", "Active"));

// Usage in controllers
[Authorize(Policy = PolicyConstants.CustomerAccess)]
[HttpGet("my-accounts")]
public async Task<ActionResult<IEnumerable<AccountDto>>> GetMyAccounts()
{
    var customerId = User.FindFirst("CustomerId")?.Value;
    var accounts = await _accountService.GetAccountsByCustomerIdAsync(Guid.Parse(customerId));
    return Ok(accounts);
}
```

#### 2. Admin Access Policy

```csharp
// Automatically configured
options.AddPolicy(PolicyConstants.AdminAccess, policy =>
    policy.RequireAuthenticatedUser()
          .RequireRole("Admin", "SuperAdmin")
          .RequireClaim("AdminLevel", "Full", "Limited"));

// Usage in controllers
[Authorize(Policy = PolicyConstants.AdminAccess)]
[HttpDelete("accounts/{id}")]
public async Task<ActionResult> DeleteAccount(Guid id)
{
    await _accountService.DeleteAccountAsync(id);
    return NoContent();
}

[Authorize(Policy = PolicyConstants.AdminAccess)]
[HttpPost("users/{userId}/roles")]
public async Task<ActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request)
{
    await _userService.AssignRoleAsync(userId, request.Role);
    return Ok();
}
```

#### 3. Manager Access Policy

```csharp
// Automatically configured
options.AddPolicy(PolicyConstants.ManagerAccess, policy =>
    policy.RequireAuthenticatedUser()
          .RequireRole("Manager", "BranchManager", "Admin")
          .RequireClaim("ManagerScope", "Branch", "Region", "National"));

// Usage in controllers
[Authorize(Policy = PolicyConstants.ManagerAccess)]
[HttpPost("accounts/{id}/freeze")]
public async Task<ActionResult> FreezeAccount(Guid id, [FromBody] FreezeAccountRequest request)
{
    await _accountService.FreezeAccountAsync(id, request.Reason);
    return Ok();
}

[Authorize(Policy = PolicyConstants.ManagerAccess)]
[HttpGet("reports/transactions")]
public async Task<ActionResult<TransactionReportDto>> GetTransactionReport(
    [FromQuery] DateTime fromDate,
    [FromQuery] DateTime toDate)
{
    var report = await _reportingService.GenerateTransactionReportAsync(fromDate, toDate);
    return Ok(report);
}
```

#### 4. Teller Access Policy

```csharp
// Automatically configured
options.AddPolicy(PolicyConstants.TellerAccess, policy =>
    policy.RequireAuthenticatedUser()
          .RequireRole("Teller", "SeniorTeller", "Manager")
          .RequireClaim("BranchId"));

// Usage in controllers
[Authorize(Policy = PolicyConstants.TellerAccess)]
[HttpPost("transactions/deposit")]
public async Task<ActionResult<TransactionDto>> ProcessDeposit(
    [FromBody] ProcessDepositRequest request)
{
    var branchId = User.FindFirst("BranchId")?.Value;
    var transaction = await _transactionService.ProcessDepositAsync(request, branchId);
    return Ok(transaction);
}
```

## Inter-Service Security Policies

### API Key Authentication Policy

```csharp
// Automatically configured
options.AddPolicy(PolicyConstants.InterServiceApiKey, policy =>
    policy.RequireAuthenticatedUser()
          .RequireClaim("AuthenticationType", "ApiKey")
          .RequireClaim("ServiceName")
          .RequireRole("InterService"));

// Usage in gRPC services
[Authorize(Policy = PolicyConstants.InterServiceApiKey)]
public class UserContactGrpcService : UserContactGrpc.UserContactGrpcBase
{
    public override async Task<GetUserContactInfoResponse> GetUserContactInfo(
        GetUserContactInfoRequest request,
        ServerCallContext context)
    {
        // Service implementation - only accessible by other services
        var serviceName = context.GetHttpContext().User.FindFirst("ServiceName")?.Value;
        _logger.LogInformation("Request from service: {ServiceName}", serviceName);

        // Process request...
    }
}
```

### Mutual TLS (mTLS) Policy

```csharp
// Automatically configured for high-security environments
options.AddPolicy(PolicyConstants.InterServiceMTls, policy =>
    policy.RequireAuthenticatedUser()
          .RequireClaim("AuthenticationType", "Certificate")
          .RequireClaim("CertificateThumbprint")
          .RequireRole("InterService"));

// Usage for high-security operations
[Authorize(Policy = PolicyConstants.InterServiceMTls)]
[HttpPost("internal/audit-log")]
public async Task<ActionResult> CreateAuditLog([FromBody] AuditLogEntry entry)
{
    await _auditService.LogAsync(entry);
    return Ok();
}
```

## Custom Authorization Policies

### Resource-Based Authorization

```csharp
// Custom resource-based policy
public class AccountOwnershipRequirement : IAuthorizationRequirement
{
}

public class AccountOwnershipHandler : AuthorizationHandler<AccountOwnershipRequirement, Account>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccountOwnershipRequirement requirement,
        Account resource)
    {
        var customerId = context.User.FindFirst("CustomerId")?.Value;

        if (customerId != null && resource.CustomerId.ToString() == customerId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Register the handler
services.AddScoped<IAuthorizationHandler, AccountOwnershipHandler>();

// Add the policy
services.AddAuthorization(options =>
{
    options.AddPolicy("AccountOwnership", policy =>
        policy.Requirements.Add(new AccountOwnershipRequirement()));
});

// Usage in controllers
[Authorize(Policy = "AccountOwnership")]
[HttpGet("accounts/{id}")]
public async Task<ActionResult<AccountDto>> GetAccount(Guid id)
{
    var account = await _accountService.GetAccountAsync(id);

    // This will only succeed if the user owns the account
    var authResult = await _authorizationService.AuthorizeAsync(User, account, "AccountOwnership");

    if (!authResult.Succeeded)
    {
        return Forbid();
    }

    return Ok(_mapper.Map<AccountDto>(account));
}
```

### Transaction Amount-Based Authorization

```csharp
// Custom policy for transaction amounts
public class TransactionAmountRequirement : IAuthorizationRequirement
{
    public decimal MaxAmount { get; }

    public TransactionAmountRequirement(decimal maxAmount)
    {
        MaxAmount = maxAmount;
    }
}

public class TransactionAmountHandler : AuthorizationHandler<TransactionAmountRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TransactionAmountHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TransactionAmountRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // Get transaction amount from request body or route
        if (httpContext?.Items["TransactionAmount"] is decimal amount)
        {
            var userRole = context.User.FindFirst("role")?.Value;

            var allowedAmount = userRole switch
            {
                "Teller" => 10000m,
                "Manager" => 100000m,
                "Admin" => decimal.MaxValue,
                _ => 1000m
            };

            if (amount <= allowedAmount)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

// Register and use
services.AddScoped<IAuthorizationHandler, TransactionAmountHandler>();

services.AddAuthorization(options =>
{
    options.AddPolicy("HighValueTransaction", policy =>
        policy.Requirements.Add(new TransactionAmountRequirement(50000m)));
});
```

## Role-Based Access Control (RBAC)

### User Roles Hierarchy

```csharp
// Role hierarchy (lowest to highest privilege)
public static class Roles
{
    public const string Customer = "Customer";
    public const string Teller = "Teller";
    public const string SeniorTeller = "SeniorTeller";
    public const string BranchManager = "BranchManager";
    public const string Manager = "Manager";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";

    // Service roles
    public const string InterService = "InterService";
    public const string SystemService = "SystemService";
}

// Role permissions matrix
public static class RolePermissions
{
    public static readonly Dictionary<string, string[]> Permissions = new()
    {
        [Roles.Customer] = new[]
        {
            "account:read:own",
            "transaction:create:own",
            "transaction:read:own"
        },

        [Roles.Teller] = new[]
        {
            "account:read",
            "transaction:create",
            "transaction:read",
            "customer:read"
        },

        [Roles.Manager] = new[]
        {
            "account:*",
            "transaction:*",
            "customer:*",
            "report:read"
        },

        [Roles.Admin] = new[]
        {
            "*"
        }
    };
}
```

### Claims-Based Authorization

```csharp
// Current User Service Implementation
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User
        .FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public Guid? CustomerId => _httpContextAccessor.HttpContext?.User
        .FindFirst("CustomerId")?.Value is string customerIdStr &&
        Guid.TryParse(customerIdStr, out var customerId) ? customerId : null;

    public string? UserName => _httpContextAccessor.HttpContext?.User
        .FindFirst(ClaimTypes.Name)?.Value;

    public IEnumerable<string> Roles => _httpContextAccessor.HttpContext?.User
        .FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? Enumerable.Empty<string>();

    public bool IsInRole(string role) => _httpContextAccessor.HttpContext?.User
        .IsInRole(role) ?? false;

    public bool HasClaim(string claimType, string claimValue) => _httpContextAccessor.HttpContext?.User
        .HasClaim(claimType, claimValue) ?? false;
}

// Usage in services
public class AccountService
{
    private readonly ICurrentUser _currentUser;

    public async Task<Result<AccountDto>> GetAccountAsync(Guid accountId)
    {
        var account = await _repository.GetByIdAsync(accountId);

        // Check ownership for customer role
        if (_currentUser.IsInRole(Roles.Customer))
        {
            if (account.CustomerId != _currentUser.CustomerId)
            {
                return Result<AccountDto>.Failure("Access denied");
            }
        }

        return Result<AccountDto>.Success(_mapper.Map<AccountDto>(account));
    }
}
```

## Security Best Practices

### 1. Principle of Least Privilege

```csharp
// Apply most restrictive policy by default
[Authorize(Policy = PolicyConstants.CustomerAccess)] // Restrictive default
public class AccountController : ControllerBase
{
    [Authorize(Policy = PolicyConstants.TellerAccess)] // More permissive for specific action
    [HttpPost("{id}/freeze")]
    public async Task<ActionResult> FreezeAccount(Guid id)
    {
        // Implementation
    }

    [Authorize(Policy = PolicyConstants.AdminAccess)] // Most permissive for admin-only action
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAccount(Guid id)
    {
        // Implementation
    }
}
```

### 2. Resource-Based Security

```csharp
// Always verify resource ownership
[HttpPut("accounts/{id}")]
public async Task<ActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequest request)
{
    var account = await _accountService.GetAccountAsync(id);

    // Verify ownership for non-admin users
    if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Manager))
    {
        if (account.CustomerId != _currentUser.CustomerId)
        {
            return Forbid("You can only update your own accounts");
        }
    }

    // Process update
    var result = await _accountService.UpdateAccountAsync(id, request);
    return Ok(result);
}
```

### 3. Audit Logging

```csharp
// Automatic audit logging for sensitive operations
[Authorize(Policy = PolicyConstants.ManagerAccess)]
[HttpPost("accounts/{id}/freeze")]
public async Task<ActionResult> FreezeAccount(Guid id, [FromBody] FreezeAccountRequest request)
{
    // Log the action
    await _auditService.LogAsync(new AuditLogEntry
    {
        UserId = _currentUser.UserId,
        Action = "AccountFreeze",
        ResourceId = id.ToString(),
        Timestamp = DateTime.UtcNow,
        Details = new { Reason = request.Reason }
    });

    await _accountService.FreezeAccountAsync(id, request.Reason);
    return Ok();
}
```

## Testing Authorization

### Unit Testing Policies

```csharp
public class AuthorizationPolicyTests
{
    [Fact]
    public async Task CustomerAccess_WithCustomerRole_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuthorization();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user123"),
            new(ClaimTypes.Role, "Customer"),
            new("CustomerStatus", "Active")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        // Act
        var result = await authorizationService.AuthorizeAsync(user, PolicyConstants.CustomerAccess);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AdminAccess_WithCustomerRole_ShouldFail()
    {
        // Arrange - user with customer role trying to access admin policy
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user123"),
            new(ClaimTypes.Role, "Customer")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        // Act
        var result = await authorizationService.AuthorizeAsync(user, PolicyConstants.AdminAccess);

        // Assert
        Assert.False(result.Succeeded);
    }
}
```

### Integration Testing with Authorization

```csharp
public class AccountControllerAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AccountControllerAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyAccounts_WithoutAuth_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/accounts/my-accounts");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithCustomerRole_Returns403()
    {
        // Arrange
        var client = _factory.CreateClient();
        var customerToken = GenerateJwtToken("Customer");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", customerToken);

        // Act
        var response = await client.DeleteAsync("/api/accounts/123e4567-e89b-12d3-a456-426614174000");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithAdminRole_Returns204()
    {
        // Arrange
        var client = _factory.CreateClient();
        var adminToken = GenerateJwtToken("Admin");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await client.DeleteAsync("/api/accounts/123e4567-e89b-12d3-a456-426614174000");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
```

## Troubleshooting Authorization

### Common Issues

1. **403 Forbidden**: User authenticated but lacks required role/claims
2. **401 Unauthorized**: Authentication failed or missing
3. **Policy Not Found**: Policy name misspelled or not registered

### Debug Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authorization": "Information",
      "Microsoft.AspNetCore.Authentication": "Information",
      "BankSystem.Shared.WebApiDefaults.Security": "Debug"
    }
  }
}
```

### Authorization Failure Debugging

```csharp
// Custom authorization handler for debugging
public class DebugAuthorizationHandler : IAuthorizationHandler
{
    private readonly ILogger<DebugAuthorizationHandler> _logger;

    public DebugAuthorizationHandler(ILogger<DebugAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.HasFailed)
        {
            _logger.LogWarning("Authorization failed for user {UserId} on resource {Resource}. Requirements: {Requirements}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                context.Resource?.GetType().Name,
                string.Join(", ", context.Requirements.Select(r => r.GetType().Name)));
        }

        return Task.CompletedTask;
    }
}
```
