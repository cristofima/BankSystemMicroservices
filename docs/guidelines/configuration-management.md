# Configuration Management Guidelines

## Overview

This document provides comprehensive guidelines for configuration management in the Bank System Microservices project, following .NET best practices for handling settings using IOptions pattern instead of direct IConfiguration usage.

## IOptions Pattern Implementation

### Configuration Models per Section

Instead of using `IConfiguration` directly, create strongly-typed configuration models:

```csharp
// ❌ Bad: Direct IConfiguration usage
public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpServer = _configuration["Email:SmtpServer"];
        var port = int.Parse(_configuration["Email:Port"]);
        // ... implementation
    }
}

// ✅ Good: Using IOptions with configuration models
public class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

public class EmailService
{
    private readonly EmailOptions _emailOptions;

    public EmailService(IOptions<EmailOptions> emailOptions)
    {
        _emailOptions = emailOptions.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpServer = _emailOptions.SmtpServer;
        var port = _emailOptions.Port;
        // ... implementation
    }
}
```

### Configuration Registration

Register configuration models in `Program.cs` or extension methods:

```csharp
// Program.cs
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));

// Or using extension method for better organization
builder.Services.AddApplicationOptions(builder.Configuration);

// Extension method
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailOptions>(
            configuration.GetSection(EmailOptions.SectionName));

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        services.Configure<AzureServiceBusOptions>(
            configuration.GetSection(AzureServiceBusOptions.SectionName));

        return services;
    }
}
```

## Comprehensive Configuration Models

### JWT Configuration

```csharp
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)] // 1 minute to 24 hours
    public int ExpiryInMinutes { get; set; } = 60;

    [Range(1, 30)] // 1 to 30 days
    public int RefreshTokenExpiryInDays { get; set; } = 7;

    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
}
```

### Database Configuration

```csharp
public class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    public int CommandTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public int MaxRetryDelay { get; set; } = 30;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;

    // Connection pooling settings
    public int MaxPoolSize { get; set; } = 100;
    public int MinPoolSize { get; set; } = 5;
    public int ConnectionLifetime { get; set; } = 0;
}

// Usage in service registration
public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        services.AddDbContext<BankDbContext>((serviceProvider, options) =>
        {
            var dbOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseSqlServer(dbOptions.ConnectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(dbOptions.CommandTimeout);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: dbOptions.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(dbOptions.MaxRetryDelay),
                    errorNumbersToAdd: null);
            });

            if (dbOptions.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();

            if (dbOptions.EnableDetailedErrors)
                options.EnableDetailedErrors();
        });

        return services;
    }
}
```

### Azure Service Bus Configuration

```csharp
public class AzureServiceBusOptions
{
    public const string SectionName = "AzureServiceBus";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    public TopicConfiguration Topics { get; set; } = new();
    public SubscriptionConfiguration Subscriptions { get; set; } = new();

    public int MaxConcurrentCalls { get; set; } = 1;
    public int MaxDeliveryCount { get; set; } = 3;
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan DefaultMessageTimeToLive { get; set; } = TimeSpan.FromDays(14);

    public class TopicConfiguration
    {
        public string TransactionEvents { get; set; } = "transaction-events";
        public string AccountEvents { get; set; } = "account-events";
        public string SecurityEvents { get; set; } = "security-events";
    }

    public class SubscriptionConfiguration
    {
        public string MovementService { get; set; } = "movement-service";
        public string AccountService { get; set; } = "account-service";
        public string NotificationService { get; set; } = "notification-service";
    }
}
```

### Logging Configuration

```csharp
public class LoggingOptions
{
    public const string SectionName = "Logging";

    public string ApplicationName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;

    public SerilogOptions Serilog { get; set; } = new();
    public ApplicationInsightsOptions ApplicationInsights { get; set; } = new();

    public class SerilogOptions
    {
        public string MinimumLevel { get; set; } = "Information";
        public string OutputTemplate { get; set; } =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}";

        public FileOptions File { get; set; } = new();
        public ConsoleOptions Console { get; set; } = new();

        public class FileOptions
        {
            public bool Enabled { get; set; } = true;
            public string Path { get; set; } = "logs/app-.txt";
            public string RollingInterval { get; set; } = "Day";
            public int RetainedFileCountLimit { get; set; } = 31;
        }

        public class ConsoleOptions
        {
            public bool Enabled { get; set; } = true;
            public string Theme { get; set; } = "Colored";
        }
    }

    public class ApplicationInsightsOptions
    {
        public bool Enabled { get; set; } = true;
        public string InstrumentationKey { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
    }
}
```

### Business Rule Configuration

```csharp
public class BusinessRulesOptions
{
    public const string SectionName = "BusinessRules";

    public TransactionLimits TransactionLimits { get; set; } = new();
    public AccountLimits AccountLimits { get; set; } = new();
    public SecurityPolicies SecurityPolicies { get; set; } = new();

    public class TransactionLimits
    {
        public decimal DailyWithdrawalLimit { get; set; } = 5000m;
        public decimal SingleTransactionLimit { get; set; } = 10000m;
        public decimal MonthlyTransferLimit { get; set; } = 50000m;
        public int MaxTransactionsPerDay { get; set; } = 50;
    }

    public class AccountLimits
    {
        public decimal MinimumBalance { get; set; } = 0m;
        public decimal OverdraftLimit { get; set; } = 1000m;
        public int MaxAccountsPerCustomer { get; set; } = 5;
    }

    public class SecurityPolicies
    {
        public int MaxFailedLoginAttempts { get; set; } = 5;
        public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
        public int PasswordMinLength { get; set; } = 8;
        public bool RequireSpecialCharacters { get; set; } = true;
        public bool RequireNumbers { get; set; } = true;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
    }
}
```

## Configuration Validation

### Validation Attributes

```csharp
public class EmailOptions : IValidatableObject
{
    public const string SectionName = "Email";

    [Required(ErrorMessage = "SMTP server is required")]
    public string SmtpServer { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int Port { get; set; }

    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string FromAddress { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Port == 587 && !EnableSsl)
        {
            yield return new ValidationResult(
                "SSL should be enabled when using port 587",
                new[] { nameof(EnableSsl) });
        }

        if (string.IsNullOrEmpty(FromAddress))
        {
            yield return new ValidationResult(
                "From address is required for email configuration",
                new[] { nameof(FromAddress) });
        }
    }
}
```

### Options Validation Registration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure with validation
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName))
            .AddOptionsWithValidateOnStart<EmailOptions>()
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrEmpty(options.SmtpServer),
                "SMTP server cannot be empty");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName))
            .AddOptionsWithValidateOnStart<JwtOptions>()
            .ValidateDataAnnotations()
            .Validate(options => options.Key.Length >= 32,
                "JWT key must be at least 32 characters long");

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName))
            .AddOptionsWithValidateOnStart<DatabaseOptions>()
            .ValidateDataAnnotations();

        return services;
    }
}
```

## Environment-Specific Configuration

### appsettings Structure

```json
// appsettings.json (base configuration)
{
  "Jwt": {
    "Issuer": "https://localhost:5001",
    "Audience": "bank-system-api",
    "ExpiryInMinutes": 60,
    "RefreshTokenExpiryInDays": 7
  },
  "Database": {
    "CommandTimeout": 30,
    "MaxRetryCount": 3,
    "MaxRetryDelay": 30,
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false
  },
  "BusinessRules": {
    "TransactionLimits": {
      "DailyWithdrawalLimit": 5000.0,
      "SingleTransactionLimit": 10000.0,
      "MonthlyTransferLimit": 50000.0,
      "MaxTransactionsPerDay": 50
    }
  }
}
```

```json
// appsettings.Development.json
{
  "Jwt": {
    "Key": "your-development-secret-key-here"
  },
  "Database": {
    "ConnectionString": "Server=localhost;Database=BankSystem_Dev;Trusted_Connection=true;",
    "EnableSensitiveDataLogging": true,
    "EnableDetailedErrors": true
  },
  "Logging": {
    "Serilog": {
      "MinimumLevel": "Debug",
      "Console": {
        "Enabled": true
      },
      "File": {
        "Enabled": true,
        "Path": "logs/dev-app-.txt"
      }
    }
  }
}
```

```json
// appsettings.Production.json
{
  "Database": {
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false,
    "MaxRetryCount": 5,
    "MaxRetryDelay": 60
  },
  "Logging": {
    "Serilog": {
      "MinimumLevel": "Warning",
      "Console": {
        "Enabled": false
      },
      "File": {
        "Enabled": true,
        "Path": "/app/logs/prod-app-.txt"
      }
    },
    "ApplicationInsights": {
      "Enabled": true
    }
  }
}
```

## Secrets Management

### Azure Key Vault Integration

```csharp
public class AzureKeyVaultOptions
{
    public const string SectionName = "AzureKeyVault";

    public bool Enabled { get; set; } = false;
    public string VaultUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

// Program.cs
if (builder.Environment.IsProduction())
{
    var keyVaultOptions = builder.Configuration
        .GetSection(AzureKeyVaultOptions.SectionName)
        .Get<AzureKeyVaultOptions>();

    if (keyVaultOptions?.Enabled == true)
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultOptions.VaultUrl),
            new ClientSecretCredential(
                keyVaultOptions.TenantId,
                keyVaultOptions.ClientId,
                keyVaultOptions.ClientSecret));
    }
}
```

### User Secrets for Development

```csharp
// For development, use user secrets
// dotnet user-secrets set "Jwt:Key" "your-super-secret-jwt-key-for-development"
// dotnet user-secrets set "Database:ConnectionString" "Server=localhost;Database=BankSystem_Dev;Trusted_Connection=true;"

// Program.cs
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}
```

## Configuration Usage Patterns

### Service Implementation with IOptions

```csharp
public class TransactionService
{
    private readonly BusinessRulesOptions _businessRules;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IOptions<BusinessRulesOptions> businessRules,
        ILogger<TransactionService> logger)
    {
        _businessRules = businessRules.Value;
        _logger = logger;
    }

    public async Task<Result> ValidateTransactionAsync(CreateTransactionCommand command)
    {
        if (command.Amount > _businessRules.TransactionLimits.SingleTransactionLimit)
        {
            _logger.LogWarning("Transaction amount {Amount} exceeds single transaction limit {Limit}",
                command.Amount, _businessRules.TransactionLimits.SingleTransactionLimit);
            return Result.Failure("Transaction amount exceeds single transaction limit");
        }

        // Additional validation logic
        return Result.Success();
    }
}
```

### IOptionsMonitor for Hot Reload

```csharp
// For configuration that might change at runtime
public class NotificationService
{
    private readonly IOptionsMonitor<NotificationOptions> _optionsMonitor;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IOptionsMonitor<NotificationOptions> optionsMonitor,
        ILogger<NotificationService> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;

        // Subscribe to configuration changes
        _optionsMonitor.OnChange(OnConfigurationChanged);
    }

    private void OnConfigurationChanged(NotificationOptions newOptions)
    {
        _logger.LogInformation("Notification configuration updated");
        // Handle configuration change
    }

    public async Task SendNotificationAsync(string message)
    {
        var currentOptions = _optionsMonitor.CurrentValue;
        // Use current configuration
    }
}
```

### IOptionsSnapshot for Scoped Configuration

```csharp
// For configuration that might vary per request/scope
public class TenantService
{
    private readonly IOptionsSnapshot<TenantOptions> _tenantOptions;

    public TenantService(IOptionsSnapshot<TenantOptions> tenantOptions)
    {
        _tenantOptions = tenantOptions;
    }

    public async Task ProcessTenantRequestAsync()
    {
        // Gets configuration specific to current scope/request
        var options = _tenantOptions.Value;
        // Process with tenant-specific configuration
    }
}
```

## Configuration Testing

### Unit Testing with Configuration

```csharp
[TestFixture]
public class TransactionServiceTests
{
    private IOptions<BusinessRulesOptions> _businessRulesOptions;
    private TransactionService _transactionService;

    [SetUp]
    public void Setup()
    {
        var businessRules = new BusinessRulesOptions
        {
            TransactionLimits = new BusinessRulesOptions.TransactionLimits
            {
                SingleTransactionLimit = 10000m,
                DailyWithdrawalLimit = 5000m
            }
        };

        _businessRulesOptions = Options.Create(businessRules);
        _transactionService = new TransactionService(_businessRulesOptions, Mock.Of<ILogger<TransactionService>>());
    }

    [Test]
    public async Task ValidateTransaction_AmountExceedsLimit_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateTransactionCommand { Amount = 15000m };

        // Act
        var result = await _transactionService.ValidateTransactionAsync(command);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Contains.Substring("exceeds single transaction limit"));
    }
}
```

## Summary

1. **Always use IOptions pattern** instead of direct IConfiguration access
2. **Create strongly-typed configuration models** for each configuration section
3. **Implement validation** using data annotations and custom validation
4. **Organize configuration** logically by feature/service area
5. **Use environment-specific** configuration files appropriately
6. **Secure sensitive data** using Azure Key Vault or user secrets
7. **Test configuration** in unit tests using Options.Create()
8. **Monitor configuration changes** when needed using IOptionsMonitor
9. **Validate configuration at startup** to catch issues early
10. **Document configuration options** with clear descriptions and examples
