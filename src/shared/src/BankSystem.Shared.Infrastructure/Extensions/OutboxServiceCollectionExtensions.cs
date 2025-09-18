using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.Infrastructure.DomainEvents;
using BankSystem.Shared.Kernel.Common;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankSystem.Shared.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for configuring the Outbox pattern with MassTransit and Azure Service Bus
/// following industry best practices for reliable distributed messaging in microservices architectures.
/// </summary>
/// <remarks>
/// This class implements the Outbox pattern to ensure transactional integrity between database operations
/// and message publishing. It integrates MassTransit with Entity Framework Core to provide guaranteed
/// message delivery with proper retry policies, circuit breaker patterns, and fault tolerance mechanisms.
/// The implementation supports both PostgreSQL and SQL Server databases with optimized configuration
/// for each database engine.
/// </remarks>
public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Configures comprehensive Outbox pattern support with Azure Service Bus integration,
    /// Entity Framework Core outbox implementation, and production-ready fault tolerance mechanisms.
    /// </summary>
    /// <typeparam name="TDbContext">The Entity Framework DbContext type for the microservice that will store outbox messages.</typeparam>
    /// <param name="services">The service collection to configure with outbox pattern dependencies.</param>
    /// <param name="configuration">The configuration instance containing connection strings and service settings.</param>
    /// <param name="databaseEngine">The database engine type (PostgreSQL or SqlServer) for optimal outbox configuration.</param>
    /// <param name="serviceName">The name of the microservice used for endpoint naming and message routing.</param>
    /// <param name="configureMessageTypes">Action delegate to configure message types and topology specific to the service.</param>
    /// <param name="configureAdditionalConsumers">Optional action delegate to configure additional message consumers for the service.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the following components:
    /// <list type="bullet">
    /// <item>Entity Framework Core outbox with database-specific optimizations</item>
    /// <item>MassTransit bus configuration with Azure Service Bus transport</item>
    /// <item>Enhanced retry policies with exponential backoff (3 retries, 1s to 1min intervals)</item>
    /// <item>Circuit breaker pattern for fault tolerance (15 trip threshold, 5min reset interval)</item>
    /// <item>Domain event dispatcher for automatic event publishing</item>
    /// <item>Message delivery limits and timeout configuration</item>
    /// <item>Duplicate detection with 30-minute window</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when serviceName is null, empty, or consists only of whitespace characters.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when configureMessageTypes is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Azure Service Bus connection string is not configured or is empty.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when an unsupported database engine is specified.
    /// </exception>
    public static IServiceCollection AddMassTransitOutboxSupport<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        DatabaseEngine databaseEngine,
        string serviceName,
        Action<IServiceBusBusFactoryConfigurator> configureMessageTypes,
        Action<IBusRegistrationConfigurator>? configureAdditionalConsumers = null
    )
        where TDbContext : DbContext
    {
        Guard.AgainstNullOrEmpty(serviceName);
        Guard.AgainstNull(configureMessageTypes);

        // Add the enhanced domain event dispatcher
        services.AddScoped<IDbContextDomainEventDispatcher, DbContextDomainEventDispatcher>();

        // Add MassTransit with enhanced Outbox configuration
        services.AddMassTransit(config =>
        {
            // Set endpoint name formatter for service-specific naming
            config.SetEndpointNameFormatter(
                new DefaultEndpointNameFormatter(serviceName.ToLowerInvariant(), false)
            );

            // Configure additional consumers if provided
            configureAdditionalConsumers?.Invoke(config);

            // Configure Entity Framework Outbox with specified database engine
            config.AddEntityFrameworkOutbox<TDbContext>(outboxConfig =>
            {
                // Configure database engine based on parameter
                switch (databaseEngine)
                {
                    case DatabaseEngine.PostgreSql:
                        outboxConfig.UsePostgres();
                        break;
                    case DatabaseEngine.SqlServer:
                        outboxConfig.UseSqlServer();
                        break;
                    default:
                        throw new ArgumentException(
                            $"Unsupported database engine: {databaseEngine}"
                        );
                }

                // Configure specific bus outbox with delivery service settings
                outboxConfig.UseBusOutbox(busOutboxConfig =>
                {
                    // Delivery service configuration
                    busOutboxConfig.MessageDeliveryLimit = 10;
                    busOutboxConfig.MessageDeliveryTimeout = TimeSpan.FromMinutes(5);
                });

                // Configure delivery service with retry limits and timeout settings
                outboxConfig.QueryDelay = TimeSpan.FromSeconds(1);
                outboxConfig.QueryMessageLimit = 100;
                outboxConfig.QueryTimeout = TimeSpan.FromSeconds(30);

                // Configure duplicate detection with time-based deduplication window
                outboxConfig.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);

                outboxConfig.DisableInboxCleanupService();
            });

            // Configure Azure Service Bus with enhanced patterns
            config.UsingAzureServiceBus(
                (context, cfg) =>
                {
                    var connectionString = configuration.GetConnectionString("AzureServiceBus");
                    Guard.AgainstNullOrEmpty(connectionString);

                    cfg.Host(connectionString);

                    // Delegate message type configuration to the microservice
                    // This allows each service to explicitly control its message topology
                    configureMessageTypes(cfg);

                    // Enhanced retry policy configuration
                    cfg.UseMessageRetry(retry =>
                    {
                        retry.Exponential(
                            3,
                            TimeSpan.FromSeconds(1),
                            TimeSpan.FromMinutes(1),
                            TimeSpan.FromSeconds(5)
                        );
                    });

                    // Circuit breaker configuration for fault tolerance
                    cfg.UseCircuitBreaker(cb =>
                    {
                        cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                        cb.TripThreshold = 15;
                        cb.ActiveThreshold = 10;
                        cb.ResetInterval = TimeSpan.FromMinutes(5);
                    });

                    // Configure automatic endpoints for all registered consumers
                    cfg.ConfigureEndpoints(context);
                }
            );
        });

        return services;
    }
}
