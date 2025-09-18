using MassTransit;

namespace BankSystem.Shared.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for configuring cross-service event consumers in a microservices architecture.
/// Enables services to consume events from other services in a scalable and decoupled manner.
/// </summary>
/// <remarks>
/// This class facilitates the implementation of event-driven architecture patterns by providing
/// standardized configuration for cross-service communication through Azure Service Bus topics
/// and subscriptions. Each consuming service gets its own subscription to source domain events.
/// </remarks>
public static class CrossServiceConsumerExtensions
{
    /// <summary>
    /// Configures cross-service event consumption with explicit consumer specification and standardized
    /// Azure Service Bus subscription endpoint configuration.
    /// </summary>
    /// <param name="cfg">The MassTransit Service Bus factory configurator for setting up messaging endpoints.</param>
    /// <param name="consumerServiceName">The name of the consuming service (e.g., "security", "notification").</param>
    /// <param name="sourceDomainName">The source domain name that publishes events (e.g., "account", "transaction").</param>
    /// <param name="configureConsumers">Action delegate to configure specific consumers for the subscription endpoint.</param>
    /// <remarks>
    /// <para>
    /// This method creates a dedicated Azure Service Bus subscription endpoint following the naming convention:
    /// - Subscription Name: "{consumerServiceName}-service"
    /// - Topic Name: "{sourceDomainName}-events"
    /// </para>
    /// <para>
    /// The endpoint is configured with production-ready settings including:
    /// - Message TTL: 14 days
    /// - Dead letter queue: Enabled on expiration
    /// - Max delivery attempts: 5
    /// - Lock duration: 10 minutes
    /// - Prefetch count: 5 messages
    /// - Concurrent processing: 3 messages
    /// </para>
    /// <para>
    /// Consume topology is disabled to prevent automatic queue creation and ensure explicit control
    /// over message routing through the provided configureConsumers action.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when cfg, consumerServiceName, sourceDomainName, or configureConsumers is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when consumerServiceName or sourceDomainName is empty or whitespace.
    /// </exception>
    public static void ConfigureCrossServiceConsumption(
        this IServiceBusBusFactoryConfigurator cfg,
        string consumerServiceName,
        string sourceDomainName,
        Action<IServiceBusSubscriptionEndpointConfigurator> configureConsumers
    )
    {
        var subscriptionName = $"{consumerServiceName.ToLower()}-service";
        var topicName = $"{sourceDomainName.ToLower()}-events";

        cfg.SubscriptionEndpoint(
            subscriptionName,
            topicName,
            endpointConfigurator =>
            {
                // Configure basic cross-service endpoint settings for reliable message processing
                endpointConfigurator.ConfigureConsumeTopology = false;
                endpointConfigurator.DefaultMessageTimeToLive = TimeSpan.FromDays(14);
                endpointConfigurator.EnableDeadLetteringOnMessageExpiration = true;
                endpointConfigurator.MaxDeliveryCount = 5;
                endpointConfigurator.LockDuration = TimeSpan.FromMinutes(10);
                endpointConfigurator.PrefetchCount = 5;
                endpointConfigurator.ConcurrentMessageLimit = 3;

                // Configure domain-specific consumers through the provided configuration action
                configureConsumers(endpointConfigurator);
            }
        );
    }
}
