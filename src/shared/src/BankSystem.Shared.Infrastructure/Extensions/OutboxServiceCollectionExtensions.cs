using System.Diagnostics.CodeAnalysis;
using BankSystem.Shared.Infrastructure.Configuration;
using BankSystem.Shared.Kernel.Common;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BankSystem.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring MassTransit Entity Framework Outbox pattern.
/// The Outbox pattern ensures reliable message publishing by storing outgoing messages
/// in the same database transaction as business data changes.
/// </summary>
/// <remarks>
/// The Entity Framework Outbox provides:
/// <list type="bullet">
/// <item>
/// <description>Transactional consistency between database changes and message publishing</description>
/// </item>
/// <item>
/// <description>Automatic retry and delivery guarantees</description>
/// </item>
/// <item>
/// <description>Duplicate detection to prevent message replay</description>
/// </item>
/// <item>
/// <description>Background processing to deliver messages outside of the request context</description>
/// </item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Configures MassTransit Entity Framework Outbox for databases.
    /// This method adds outbox middleware that captures published messages and stores them
    /// in database tables within the same transaction as business data.
    /// </summary>
    /// <typeparam name="TDbContext">The Entity Framework DbContext type that will contain the outbox tables</typeparam>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configuration">Configuration containing outbox settings</param>
    /// <param name="databaseEngine">Database engine</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddEntityFrameworkOutbox<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        DatabaseEngine databaseEngine
    )
        where TDbContext : DbContext
    {
        services
            .Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName))
            .AddOptionsWithValidateOnStart<OutboxOptions>();

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<OutboxOptions>>().Value;

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddEntityFrameworkOutbox<TDbContext>(outboxConfigurator =>
            {
                switch (databaseEngine)
                {
                    case DatabaseEngine.SqlServer:
                        outboxConfigurator.UseSqlServer();
                        break;

                    case DatabaseEngine.PostgreSql:
                        outboxConfigurator.UsePostgres();
                        break;
                }

                // Query Configuration
                outboxConfigurator.QueryDelay = TimeSpan.FromSeconds(options.QueryDelaySeconds);
                outboxConfigurator.DuplicateDetectionWindow = TimeSpan.FromMinutes(
                    options.DuplicateDetectionWindowMinutes
                );

                // Cleanup Configuration
                if (options.DisableInboxCleanupService)
                {
                    outboxConfigurator.DisableInboxCleanupService();
                }

                // Use Bus Outbox for better performance
                outboxConfigurator.UseBusOutbox();
            });
        });

        return services;
    }
}
