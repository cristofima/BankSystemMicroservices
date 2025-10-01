using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BankSystem.Shared.Infrastructure.Services;

/// <summary>
/// Hosted service that automatically applies database migrations on application startup
/// </summary>
/// <typeparam name="TContext">The DbContext type to migrate</typeparam>
[ExcludeFromCodeCoverage]
public class DatabaseMigrationHostedService<TContext> : IHostedService
    where TContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationHostedService<TContext>> _logger;

    /// <summary>
    /// Initializes a new instance of the DatabaseMigrationHostedService class
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency resolution</param>
    /// <param name="logger">The logger instance</param>
    public DatabaseMigrationHostedService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationHostedService<TContext>> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Starts the hosted service and applies database migrations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting automatic database migration for {ContextType}",
            typeof(TContext).Name
        );

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(
                cancellationToken
            );
            var migrations = pendingMigrations as string[] ?? pendingMigrations.ToArray();
            if (migrations.Length > 0)
            {
                _logger.LogInformation(
                    "Applying {Count} pending migrations for {ContextType}",
                    migrations.Length,
                    typeof(TContext).Name
                );

                await context.Database.MigrateAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully applied automatic migrations for {ContextType}",
                    typeof(TContext).Name
                );
            }
            else
            {
                _logger.LogInformation(
                    "No pending migrations found for {ContextType}",
                    typeof(TContext).Name
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to apply automatic migrations for {ContextType}",
                typeof(TContext).Name
            );
            throw;
        }
    }

    /// <summary>
    /// Stops the hosted service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
