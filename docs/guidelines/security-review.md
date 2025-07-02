# Security Review Guidelines

## Overview

This document provides comprehensive security review guidelines for the Bank System Microservices project, following OWASP best practices and Microsoft security recommendations for financial applications.

## Security Review Framework

### OWASP Top 10 for API Security

#### A01: Broken Object Level Authorization

```csharp
// ✅ Good: Proper authorization checks
[HttpGet("{accountId:guid}/transactions")]
[Authorize]
public async Task<ActionResult<IEnumerable<TransactionDto>>> GetAccountTransactions(Guid accountId)
{
    // Verify user can access this account
    var hasAccess = await _authorizationService.AuthorizeAsync(
        User, accountId, "CanViewAccount");

    if (!hasAccess.Succeeded)
    {
        _logger.LogWarning("User {UserId} attempted to access account {AccountId} without permission",
            User.GetUserId(), accountId);
        return Forbid();
    }

    var query = new GetAccountTransactionsQuery(accountId);
    var result = await _mediator.Send(query);

    return Ok(result);
}

// ❌ Bad: Missing authorization check
[HttpGet("{accountId:guid}/transactions")]
[Authorize] // Only checks if user is authenticated, not authorized for this account
public async Task<ActionResult<IEnumerable<TransactionDto>>> GetAccountTransactions(Guid accountId)
{
    // Any authenticated user can access any account's transactions
    var query = new GetAccountTransactionsQuery(accountId);
    var result = await _mediator.Send(query);
    return Ok(result);
}
```

#### A02: Broken Authentication

```csharp
// ✅ Good: Secure authentication implementation
public class AuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthenticationService> _logger;

    public async Task<Result<AuthenticationResponse>> AuthenticateAsync(LoginRequest request)
    {
        // Rate limiting check
        if (await IsRateLimitedAsync(request.Email))
        {
            _logger.LogWarning("Rate limit exceeded for email {Email}", request.Email);
            return Result<AuthenticationResponse>.Failure("Too many login attempts. Please try again later.");
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Log failed attempt but don't reveal user doesn't exist
            _logger.LogWarning("Login attempt for non-existent email {Email}", request.Email);
            await RecordFailedAttemptAsync(request.Email);
            return Result<AuthenticationResponse>.Failure("Invalid email or password");
        }

        // Check account lockout
        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login attempt for locked account {UserId}", user.Id);
            return Result<AuthenticationResponse>.Failure("Account is locked. Please try again later.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed login attempt for user {UserId}", user.Id);
            await RecordFailedAttemptAsync(request.Email);

            if (result.IsLockedOut)
                return Result<AuthenticationResponse>.Failure("Account has been locked due to multiple failed attempts");

            return Result<AuthenticationResponse>.Failure("Invalid email or password");
        }

        // Generate secure tokens
        var tokens = await _tokenService.GenerateTokensAsync(user);

        _logger.LogInformation("Successful login for user {UserId}", user.Id);
        await ClearFailedAttemptsAsync(request.Email);

        return Result<AuthenticationResponse>.Success(new AuthenticationResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresAt = tokens.ExpiresAt
        });
    }
}

// ❌ Bad: Insecure authentication
public class AuthenticationService
{
    public async Task<string> AuthenticateAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Reveals if user exists
        if (user == null)
            throw new Exception("User not found");

        // No rate limiting or lockout protection
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            throw new Exception("Invalid password");

        // Simple token without proper claims or expiration
        return GenerateSimpleToken(user.Id);
    }
}
```

#### A03: Broken Object Property Level Authorization

```csharp
// ✅ Good: Property-level authorization
public class AccountDto
{
    public Guid Id { get; init; }
    public string AccountNumber { get; init; } = string.Empty;
    public decimal Balance { get; init; }

    // Sensitive fields only for account owners
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FullAccountNumber { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RoutingNumber { get; init; }
}

public class AccountMappingProfile : Profile
{
    public AccountMappingProfile()
    {
        CreateMap<Account, AccountDto>()
            .ForMember(dest => dest.FullAccountNumber, opt => opt.MapFrom((src, dest, member, context) =>
            {
                // Only include full account number for account owner
                var currentUserId = context.Items.ContainsKey("CurrentUserId")
                    ? (Guid)context.Items["CurrentUserId"]
                    : Guid.Empty;

                return src.CustomerId == currentUserId ? src.AccountNumber : null;
            }))
            .ForMember(dest => dest.RoutingNumber, opt => opt.MapFrom((src, dest, member, context) =>
            {
                var currentUserId = context.Items.ContainsKey("CurrentUserId")
                    ? (Guid)context.Items["CurrentUserId"]
                    : Guid.Empty;

                return src.CustomerId == currentUserId ? src.RoutingNumber : null;
            }));
    }
}

// ❌ Bad: Exposing sensitive data
public class AccountDto
{
    public Guid Id { get; init; }
    public string AccountNumber { get; init; } = string.Empty; // Full number always exposed
    public string RoutingNumber { get; init; } = string.Empty; // Sensitive data exposed
    public string SocialSecurityNumber { get; init; } = string.Empty; // Highly sensitive
}
```

#### A04: Unrestricted Resource Consumption

```csharp
// ✅ Good: Resource limits and pagination
[HttpGet("transactions")]
[EnableRateLimiting("GetTransactionsPolicy")]
public async Task<ActionResult<PagedResult<TransactionDto>>> GetTransactions(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null)
{
    // Validate page size limits
    if (pageSize > 100)
    {
        _logger.LogWarning("Excessive page size requested: {PageSize}", pageSize);
        return BadRequest("Page size cannot exceed 100");
    }

    // Validate date range
    if (fromDate.HasValue && toDate.HasValue)
    {
        var dateRange = toDate.Value - fromDate.Value;
        if (dateRange.TotalDays > 365)
        {
            _logger.LogWarning("Excessive date range requested: {Days} days", dateRange.TotalDays);
            return BadRequest("Date range cannot exceed 365 days");
        }
    }

    var query = new GetTransactionsQuery(page, pageSize, fromDate, toDate);
    var result = await _mediator.Send(query);

    return Ok(result);
}

// Rate limiting configuration
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("GetTransactionsPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100; // 100 requests
        limiterOptions.Window = TimeSpan.FromMinutes(1); // per minute
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });
});

// ❌ Bad: No resource limits
[HttpGet("transactions")]
public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null)
{
    // No pagination - could return millions of records
    // No rate limiting - could be called repeatedly
    // No date range validation - could query years of data

    var transactions = await _context.Transactions
        .Where(t => !fromDate.HasValue || t.Timestamp >= fromDate)
        .Where(t => !toDate.HasValue || t.Timestamp <= toDate)
        .ToListAsync(); // Loads everything into memory

    return Ok(_mapper.Map<IEnumerable<TransactionDto>>(transactions));
}
```

#### A05: Broken Function Level Authorization

```csharp
// ✅ Good: Function-level authorization
[HttpPost("accounts/{accountId:guid}/freeze")]
[Authorize(Policy = "RequireManagerRole")]
public async Task<ActionResult> FreezeAccount(Guid accountId, [FromBody] FreezeAccountRequest request)
{
    // Additional authorization check
    var user = await _userManager.GetUserAsync(User);
    var canFreezeAccount = await _authorizationService.AuthorizeAsync(
        User, new AccountAuthorizationRequirement(accountId, "Freeze"));

    if (!canFreezeAccount.Succeeded)
    {
        _logger.LogWarning("User {UserId} attempted to freeze account {AccountId} without permission",
            user.Id, accountId);
        return Forbid();
    }

    var command = new FreezeAccountCommand(accountId, request.Reason, user.Id);
    var result = await _mediator.Send(command);

    if (result.IsSuccess)
    {
        _logger.LogInformation("Account {AccountId} frozen by user {UserId} for reason: {Reason}",
            accountId, user.Id, request.Reason);
    }

    return result.IsSuccess ? NoContent() : BadRequest(result.Error);
}

// Authorization policy configuration
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireManagerRole", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Manager", "Admin");
        policy.RequireClaim("department", "Banking");
    });
});

// ❌ Bad: Insufficient authorization
[HttpPost("accounts/{accountId:guid}/freeze")]
[Authorize] // Only checks authentication, not authorization
public async Task<ActionResult> FreezeAccount(Guid accountId)
{
    // Any authenticated user can freeze any account
    await _accountService.FreezeAccountAsync(accountId);
    return NoContent();
}
```

#### A06: Unrestricted Access to Sensitive Business Flows

```csharp
// ✅ Good: Protected sensitive operations
[HttpPost("transfer")]
[Authorize]
[EnableRateLimiting("TransferPolicy")]
public async Task<ActionResult<TransferDto>> CreateTransfer([FromBody] CreateTransferRequest request)
{
    // Additional verification for high-value transfers
    if (request.Amount > 10000)
    {
        var mfaVerified = await _mfaService.VerifyRecentMfaAsync(User.GetUserId());
        if (!mfaVerified)
        {
            _logger.LogWarning("High-value transfer attempt without MFA verification by user {UserId}",
                User.GetUserId());
            return BadRequest("Multi-factor authentication required for transfers over $10,000");
        }
    }

    // Check daily transfer limits
    var dailyTransferTotal = await _transferService.GetDailyTransferTotalAsync(
        User.GetUserId(), DateTime.UtcNow.Date);

    if (dailyTransferTotal + request.Amount > 50000)
    {
        _logger.LogWarning("Daily transfer limit exceeded for user {UserId}. Current: {Current}, Requested: {Requested}",
            User.GetUserId(), dailyTransferTotal, request.Amount);
        return BadRequest("Daily transfer limit exceeded");
    }

    // Fraud detection
    var fraudScore = await _fraudDetectionService.AnalyzeTransferAsync(request, User.GetUserId());
    if (fraudScore > 0.8)
    {
        _logger.LogWarning("High fraud score {Score} detected for transfer by user {UserId}",
            fraudScore, User.GetUserId());

        // Flag for manual review
        await _reviewService.FlagForReviewAsync(request, fraudScore);
        return Accepted("Transfer flagged for review due to security concerns");
    }

    var command = new CreateTransferCommand(
        request.FromAccountId,
        request.ToAccountId,
        request.Amount,
        request.Description,
        User.GetUserId());

    var result = await _mediator.Send(command);

    if (result.IsSuccess)
    {
        _logger.LogInformation("Transfer created: {TransferId} from {FromAccount} to {ToAccount} amount {Amount}",
            result.Value.Id, request.FromAccountId, request.ToAccountId, request.Amount);
    }

    return result.IsSuccess
        ? CreatedAtAction(nameof(GetTransfer), new { transferId = result.Value.Id }, result.Value)
        : BadRequest(result.Error);
}

// ❌ Bad: Unprotected sensitive operations
[HttpPost("transfer")]
[Authorize]
public async Task<ActionResult> CreateTransfer([FromBody] CreateTransferRequest request)
{
    // No amount limits
    // No MFA for high-value transfers
    // No fraud detection
    // No rate limiting

    await _transferService.CreateTransferAsync(request);
    return Ok();
}
```

### Input Validation Security

#### SQL Injection Prevention

```csharp
// ✅ Good: Parameterized queries with Entity Framework
public async Task<IEnumerable<Transaction>> GetTransactionsByDescriptionAsync(
    string description,
    CancellationToken cancellationToken = default)
{
    return await _context.Transactions
        .Where(t => EF.Functions.Like(t.Description, $"%{description}%")) // Parameterized
        .ToListAsync(cancellationToken);
}

// Using stored procedures
public async Task<Account> GetAccountByNumberAsync(string accountNumber)
{
    var parameter = new SqlParameter("@AccountNumber", SqlDbType.VarChar, 20)
    {
        Value = accountNumber
    };

    return await _context.Accounts
        .FromSqlRaw("EXEC GetAccountByNumber @AccountNumber", parameter)
        .FirstOrDefaultAsync();
}

// ❌ Bad: String concatenation (vulnerable to SQL injection)
public async Task<IEnumerable<Transaction>> GetTransactionsByDescription(string description)
{
    var sql = $"SELECT * FROM Transactions WHERE Description LIKE '%{description}%'";
    return await _context.Transactions.FromSqlRaw(sql).ToListAsync();
}
```

#### XSS Prevention

```csharp
// ✅ Good: Input sanitization and output encoding
public class CommentDto
{
    private string _content = string.Empty;

    public string Content
    {
        get => _content;
        init => _content = HtmlEncoder.Default.Encode(value ?? string.Empty); // Encode on input
    }
}

[HttpPost("comments")]
public async Task<ActionResult<CommentDto>> CreateComment([FromBody] CreateCommentRequest request)
{
    // Validate input length
    if (request.Content.Length > 1000)
        return BadRequest("Comment content too long");

    // Sanitize HTML
    var sanitizedContent = _htmlSanitizer.Sanitize(request.Content);

    var command = new CreateCommentCommand(sanitizedContent);
    var result = await _mediator.Send(command);

    return result.IsSuccess
        ? Ok(result.Value)
        : BadRequest(result.Error);
}

// ❌ Bad: No input sanitization
public class CommentDto
{
    public string Content { get; init; } = string.Empty; // Raw HTML stored
}
```

#### CSRF Protection

```csharp
// ✅ Good: CSRF protection enabled
[HttpPost("transfer")]
[Authorize]
[ValidateAntiForgeryToken] // CSRF protection
public async Task<ActionResult> CreateTransfer([FromBody] CreateTransferRequest request)
{
    // Implementation
}

// In Startup/Program.cs
services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ❌ Bad: No CSRF protection
[HttpPost("transfer")]
[Authorize]
public async Task<ActionResult> CreateTransfer([FromBody] CreateTransferRequest request)
{
    // Vulnerable to CSRF attacks
}
```

### Cryptography and Data Protection

#### Secure Password Handling

```csharp
// ✅ Good: Secure password configuration
services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 12; // Strong minimum length
    options.Password.RequiredUniqueChars = 4;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedPhoneNumber = false;
});

// Password hashing service
public class PasswordHashingService
{
    private const int WorkFactor = 12; // bcrypt work factor

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}

// ❌ Bad: Weak password requirements
services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 6; // Too short
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.MaxFailedAccessAttempts = 20; // Too permissive
});
```

#### Sensitive Data Encryption

```csharp
// ✅ Good: Data encryption at rest
public class EncryptedPersonalData : IPersonalData
{
    private readonly IDataProtector _protector;

    public EncryptedPersonalData(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("PersonalData.v1");
    }

    public string EncryptedSocialSecurityNumber { get; set; } = string.Empty;

    [NotMapped]
    public string SocialSecurityNumber
    {
        get => string.IsNullOrEmpty(EncryptedSocialSecurityNumber)
            ? string.Empty
            : _protector.Unprotect(EncryptedSocialSecurityNumber);
        set => EncryptedSocialSecurityNumber = string.IsNullOrEmpty(value)
            ? string.Empty
            : _protector.Protect(value);
    }
}

// Database column-level encryption
[Column(TypeName = "varbinary(max)")]
public byte[] EncryptedAccountNumber { get; set; } = Array.Empty<byte>();

// ❌ Bad: Storing sensitive data in plain text
public class Customer
{
    public string SocialSecurityNumber { get; set; } = string.Empty; // Plain text
    public string CreditCardNumber { get; set; } = string.Empty; // Plain text
}
```

### Secure Communication

#### HTTPS Configuration

```csharp
// ✅ Good: Secure HTTPS configuration
services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

// In Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");

    await next();
});
```

#### CORS Configuration

```csharp
// ✅ Good: Restrictive CORS policy
services.AddCors(options =>
{
    options.AddPolicy("BankingAppPolicy", builder =>
    {
        builder
            .WithOrigins("https://banking.example.com", "https://mobile.banking.example.com")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", "Authorization", "X-CSRF-TOKEN")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// ❌ Bad: Permissive CORS policy
services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin() // Security risk
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
```

### Logging and Monitoring

#### Security Event Logging

```csharp
// ✅ Good: Comprehensive security logging
public class SecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;

    public SecurityEventLogger(ILogger<SecurityEventLogger> logger)
    {
        _logger = logger;
    }

    public void LogSuccessfulLogin(string userId, string ipAddress)
    {
        _logger.LogInformation("SECURITY_EVENT: Successful login for user {UserId} from IP {IpAddress}",
            userId, ipAddress);
    }

    public void LogFailedLogin(string email, string ipAddress, string reason)
    {
        _logger.LogWarning("SECURITY_EVENT: Failed login attempt for email {Email} from IP {IpAddress}. Reason: {Reason}",
            email, ipAddress, reason);
    }

    public void LogAccountLockout(string userId, string ipAddress)
    {
        _logger.LogWarning("SECURITY_EVENT: Account locked for user {UserId} from IP {IpAddress}",
            userId, ipAddress);
    }

    public void LogUnauthorizedAccess(string userId, string resource, string action)
    {
        _logger.LogWarning("SECURITY_EVENT: Unauthorized access attempt by user {UserId} to resource {Resource} action {Action}",
            userId, resource, action);
    }

    public void LogHighValueTransaction(string userId, decimal amount, string accountId)
    {
        _logger.LogInformation("SECURITY_EVENT: High-value transaction {Amount:C} by user {UserId} on account {AccountId}",
            amount, userId, accountId);
    }
}

// Structured logging configuration
services.Configure<LoggerFilterOptions>(options =>
{
    // Ensure security events are always logged
    options.Rules.Add(new LoggerFilterRule(
        providerName: null,
        categoryName: "SecurityEventLogger",
        logLevel: LogLevel.Information,
        filter: null));
});
```

### Security Testing

#### Security Unit Tests

```csharp
// ✅ Good: Security-focused unit tests
[TestFixture]
public class AuthorizationTests
{
    private AuthorizationService _authorizationService;
    private Mock<IUserService> _mockUserService;

    [Test]
    public async Task AuthorizeAccountAccess_UserNotOwner_ShouldDeny()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var differentOwnerId = Guid.NewGuid();

        _mockUserService.Setup(x => x.GetAccountOwnerAsync(accountId))
            .ReturnsAsync(differentOwnerId);

        // Act
        var result = await _authorizationService.CanAccessAccountAsync(userId, accountId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateTransactionAmount_ExceedsLimit_ShouldFail()
    {
        // Arrange
        var transaction = new CreateTransactionCommand
        {
            Amount = 100000, // Exceeds daily limit
            AccountId = Guid.NewGuid()
        };

        // Act
        var result = await _transactionValidator.ValidateAsync(transaction);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.ErrorMessage.Contains("daily limit")), Is.True);
    }
}
```

## Security Review Checklist

### Authentication & Authorization

- [ ] Multi-factor authentication implemented for high-value operations
- [ ] Proper session management and timeout
- [ ] Account lockout protection
- [ ] Password complexity requirements enforced
- [ ] Role-based access control (RBAC) implemented
- [ ] Resource-level authorization checks
- [ ] JWT tokens properly validated and secured

### Input Validation & Data Protection

- [ ] All inputs validated and sanitized
- [ ] SQL injection prevention (parameterized queries)
- [ ] XSS prevention (output encoding)
- [ ] CSRF protection enabled
- [ ] File upload restrictions
- [ ] Sensitive data encrypted at rest
- [ ] PII data properly masked in logs and responses

### Communication Security

- [ ] HTTPS enforced with proper configuration
- [ ] Secure headers implemented
- [ ] CORS policy properly configured
- [ ] Certificate validation
- [ ] Secure communication between microservices

### Error Handling & Logging

- [ ] Security events properly logged
- [ ] Error messages don't leak sensitive information
- [ ] Audit trail maintained
- [ ] Log tampering protection
- [ ] Monitoring and alerting configured

### Infrastructure Security

- [ ] Secrets management (Azure Key Vault)
- [ ] Network security (VNet, NSG)
- [ ] Container security scanning
- [ ] Dependency vulnerability scanning
- [ ] Regular security updates

## Summary

1. **Follow OWASP guidelines** - Address top security risks systematically
2. **Implement defense in depth** - Multiple security layers
3. **Validate all inputs** - Never trust client data
4. **Encrypt sensitive data** - Both at rest and in transit
5. **Log security events** - Maintain audit trails
6. **Test security controls** - Include security in testing strategy
7. **Monitor continuously** - Detect and respond to threats
8. **Keep dependencies updated** - Regular security patches
9. **Follow least privilege** - Minimal necessary permissions
10. **Regular security reviews** - Continuous improvement
