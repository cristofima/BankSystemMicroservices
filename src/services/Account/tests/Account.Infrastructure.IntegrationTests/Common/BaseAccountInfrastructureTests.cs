using BankSystem.Account.Infrastructure.Data;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace BankSystem.Account.Infrastructure.IntegrationTests.Common;

/// <summary>
/// Base class for Account infrastructure integration tests using Testcontainers with PostgreSQL.
/// Provides isolated database instances per test class for reliable and independent testing.
/// </summary>
public abstract class BaseAccountInfrastructureTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _sqlContainer;
    private ServiceProvider? _serviceProvider;

    protected BaseAccountInfrastructureTests()
    {
        // Create PostgreSQL container with specific configuration
        _sqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithUsername("testuser")
            .WithPassword("Test123!")
            .WithPortBinding(0, 5432) // Random host port
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
    }

    /// <summary>
    /// Gets the configured service provider for dependency resolution
    /// </summary>
    protected ServiceProvider ServiceProvider => _serviceProvider
                                                 ?? throw new InvalidOperationException("Service provider not initialized. Ensure InitializeAsync has been called.");

    /// <summary>
    /// Gets a service of type T from the service provider
    /// </summary>
    protected T GetService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

    /// <summary>
    /// Gets the AccountDbContext instance
    /// </summary>
    protected AccountDbContext GetDbContext() => GetService<AccountDbContext>();

    /// <summary>
    /// Initializes the test environment - starts PostgreSQL container and sets up services
    /// </summary>
    public async Task InitializeAsync()
    {
        // Start the PostgreSQL container
        await _sqlContainer.StartAsync();

        // Setup service collection with infrastructure dependencies
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Create configuration
        var configuration = CreateConfiguration();
        services.AddSingleton(configuration);

        // Configure DbContext with container connection string
        services.AddDbContext<AccountDbContext>(options =>
        {
            options.UseNpgsql(_sqlContainer.GetConnectionString(), sqlOptions =>
            {
                sqlOptions.CommandTimeout(30);
                sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
            });

            // Enable detailed errors and sensitive data logging for testing
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        });

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
    /// Creates test configuration with required settings
    /// </summary>
    private IConfiguration CreateConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();

        // Add in-memory configuration for testing
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            // Database Configuration
            ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString(),
        }!);

        return configurationBuilder.Build();
    }

    /// <summary>
    /// Initializes the database schema and applies migrations
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

        // Ensure database is created and migrations are applied
        await dbContext.Database.EnsureCreatedAsync();
    }
}