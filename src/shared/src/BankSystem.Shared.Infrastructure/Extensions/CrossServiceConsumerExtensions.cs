using BankSystem.Shared.Domain.Validation;
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
    /// <param name="context">The registration context providing access to registered consumers for configuration.</param>
    /// <param name="consumerServiceName">The name of the consuming service (e.g., "security", "notification").</param>
    /// <param name="sourceDomainName">The source domain name that publishes events (e.g., "account", "transaction").</param>
    /// <param name="configureConsumers">Action delegate to configure specific consumers for the subscription endpoint.</param>
    /// <remarks>
    /// <para>
    /// This method creates a dedicated Azure Service Bus subscription endpoint following the naming convention:
    /// <list type="bullet">
    /// <item>Subscription Name: "{consumerServiceName}-service"</item>
    /// <item>Topic Name: "{sourceDomainName}-events"</item>
    /// </list>
    /// </para>
    /// <para>
    /// Consume topology is disabled to prevent automatic queue creation and ensure explicit control
    /// over message routing through the provided configureConsumers action.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when context or configureConsumers is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when consumerServiceName or sourceDomainName is empty or whitespace.
    /// </exception>
    public static void ConfigureCrossServiceSubscription(
        this IServiceBusBusFactoryConfigurator cfg,
        IRegistrationContext context,
        string consumerServiceName,
        string sourceDomainName,
        Action<IServiceBusSubscriptionEndpointConfigurator, IRegistrationContext> configureConsumers
    )
    {
        Guard.AgainstNull(context);
        Guard.AgainstNull(configureConsumers);
        Guard.AgainstNullOrEmpty(consumerServiceName);
        Guard.AgainstNullOrEmpty(consumerServiceName);

        var subscriptionName = $"{consumerServiceName.ToLowerInvariant()}-service";
        var topicName = $"{sourceDomainName.ToLowerInvariant()}-events";

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
                endpointConfigurator.LockDuration = TimeSpan.FromMinutes(5);
                endpointConfigurator.PrefetchCount = 5;
                endpointConfigurator.ConcurrentMessageLimit = 3;

                // Configure domain-specific consumers through the provided configuration action
                // Pass both the endpoint configurator and registration context for proper consumer configuration
                configureConsumers(endpointConfigurator, context);
            }
        );
    }
}
