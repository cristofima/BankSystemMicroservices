using BankSystem.Shared.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankSystem.Shared.Infrastructure.Extensions;

/// <summary>
/// Shared database migration extensions for all microservices
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Configures automatic database migration to be applied when the application starts
    /// </summary>
    /// <typeparam name="TContext">The DbContext type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddAutomaticMigrations<TContext>(
        this IServiceCollection services
    )
        where TContext : DbContext
    {
        // Validate that TContext is registered
        if (services.All(sd => sd.ServiceType != typeof(TContext)))
        {
            throw new InvalidOperationException(
                $"DbContext {typeof(TContext).Name} must be registered before calling AddAutomaticMigrations"
            );
        }

        services.AddHostedService<DatabaseMigrationHostedService<TContext>>();
        return services;
    }
}
