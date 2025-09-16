using BankSystem.Shared.Kernel.Common;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Security.Application.Configuration;
using Security.Infrastructure.Data;
using Testcontainers.MsSql;

namespace Security.Infrastructure.IntegrationTests.Common;

/// <summary>
/// Base class for Security infrastructure integration tests using Testcontainers with SQL Server.
/// Provides isolated database instances per test class for reliable and independent testing.
/// </summary>
public abstract class BaseSecurityInfrastructureTest : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private ServiceProvider? _serviceProvider;

    protected BaseSecurityInfrastructureTest()
    {
        // Create SQL Server container with specific configuration
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Test123!")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SA_PASSWORD", "Test123!")
            .WithPortBinding(0, 1433) // Random host port
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();
    }

    /// <summary>
    /// Gets the configured service provider for dependency resolution
    /// </summary>
    protected ServiceProvider ServiceProvider =>
        _serviceProvider
        ?? throw new InvalidOperationException(
            "Service provider not initialized. Ensure InitializeAsync has been called."
        );

    /// <summary>
    /// Gets a service of type T from the service provider
    /// </summary>
    protected T GetService<T>()
        where T : notnull => ServiceProvider.GetRequiredService<T>();

    /// <summary>
    /// Gets the SecurityDbContext instance
    /// </summary>
    protected SecurityDbContext GetDbContext() => GetService<SecurityDbContext>();

    /// <summary>
    /// Initializes the test environment - starts SQL Server container and sets up services
    /// </summary>
    public async Task InitializeAsync()
    {
        // Start the SQL Server container
        await _sqlContainer.StartAsync();

        // Setup service collection with infrastructure dependencies
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Create configuration
        var configuration = CreateConfiguration();
        services.AddSingleton<IConfiguration>(configuration);

        // Configure options
        ConfigureOptions(services, configuration);

        // Configure DbContext with container connection string
        services.AddDbContext<SecurityDbContext>(options =>
        {
            options.UseSqlServer(
                _sqlContainer.GetConnectionString(),
                sqlOptions =>
                {
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null
                    );
                }
            );

            // Enable detailed errors and sensitive data logging for testing
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        });

        services.AddScoped<ICurrentUser, TestCurrentUser>();

        // Register infrastructure services
        services.AddInfrastructureServices(configuration);

        // Build service provider
        _serviceProvider = services.BuildServiceProvider();

        // Initialize database
        await InitializeDatabaseAsync();
    }

    /// <summary>
    /// Cleans up resources - stops SQL Server container and disposes service provider
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
        {
            await _serviceProvider.DisposeAsync();
        }

        await _sqlContainer.DisposeAsync();
    }

    /// <summary>
    /// Creates a fresh scope for isolated test operations
    /// </summary>
    protected IServiceScope CreateScope() => ServiceProvider.CreateScope();

    /// <summary>
    /// Executes an operation within a new service scope
    /// </summary>
    protected async Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> operation)
    {
        using var scope = CreateScope();
        return await operation(scope.ServiceProvider);
    }

    /// <summary>
    /// Executes an operation within a new service scope
    /// </summary>
    protected async Task ExecuteInScopeAsync(Func<IServiceProvider, Task> operation)
    {
        using var scope = CreateScope();
        await operation(scope.ServiceProvider);
    }

    /// <summary>
    /// Creates test configuration with required settings
    /// </summary>
    private IConfiguration CreateConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();

        // Add in-memory configuration for testing
        configurationBuilder.AddInMemoryCollection(
            new Dictionary<string, string>
            {
                // Database Configuration
                ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString(),

                // JWT Configuration
                ["Jwt:Key"] = "ThisIsASecretKeyForTestingPurposesOnly123456789",
                ["Jwt:Issuer"] = "https://localhost:5001",
                ["Jwt:Audience"] = "bank-system-test",
                ["Jwt:ExpiryInMinutes"] = "60",
                ["Jwt:RefreshTokenExpiryInDays"] = "7",

                // Security Configuration
                ["Security:MaxFailedLoginAttempts"] = "5",
                ["Security:LockoutDuration"] = "00:15:00",
                ["Security:PasswordPolicy:MinLength"] = "8",
                ["Security:PasswordPolicy:RequireSpecialCharacters"] = "true",
                ["Security:PasswordPolicy:RequireNumbers"] = "true",
                ["Security:PasswordPolicy:RequireUppercase"] = "true",
                ["Security:PasswordPolicy:RequireLowercase"] = "true",
                ["Security:TokenSecurity:EnableTokenRotation"] = "true",
                ["Security:TokenSecurity:EnableRevocationCheck"] = "true",
                ["Security:TokenSecurity:MaxConcurrentSessions"] = "5",
                ["Security:TokenSecurity:CleanupExpiredTokensAfterDays"] = "30",
                ["Security:Audit:EnableAuditLogging"] = "true",
                ["Security:Audit:LogSuccessfulAuthentication"] = "true",
                ["Security:Audit:LogFailedAuthentication"] = "true",
                ["Security:Audit:LogTokenOperations"] = "true",
                ["Security:Audit:LogUserOperations"] = "true",
            }!
        );

        return configurationBuilder.Build();
    }

    /// <summary>
    /// Configures options from configuration
    /// </summary>
    private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
    }

    /// <summary>
    /// Initializes the database schema and applies migrations
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        // Ensure database is created and migrations are applied
        await dbContext.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Cleans up the database after each test method to ensure isolation
    /// </summary>
    protected async Task CleanupDatabaseAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        // Remove all data but keep schema
        await dbContext.RefreshTokens.ExecuteDeleteAsync();
        await dbContext.Users.ExecuteDeleteAsync();

        await dbContext.SaveChangesAsync();
    }
}
