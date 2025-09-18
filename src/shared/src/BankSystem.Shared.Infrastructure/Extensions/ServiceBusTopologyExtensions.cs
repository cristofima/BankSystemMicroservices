using System.Reflection;
using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.Kernel.Events;
using MassTransit;

namespace BankSystem.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring Azure Service Bus topology with MassTransit.
/// Implements best practices with topic-per-domain pattern and cross-service subscriptions.
/// </summary>
/// <remarks>
/// <para>
/// Architecture Pattern:
/// <list type="bullet">
/// <item>Each microservice publishes to its own topic (account-events, security-events, etc.)</item>
/// <item>Consumer services create subscriptions to topics they need to consume from</item>
/// <item>Complete isolation between domains with clear responsibility boundaries</item>
/// </list>
/// </para>
/// </remarks>
public static class ServiceBusTopologyExtensions
{
    /// <summary>
    /// Configures domain publishing - each service publishes to its own topic.
    /// Example: Account service publishes AccountCreatedEvent to "account-events" topic.
    /// This is the recommended approach for publisher-only services.
    /// </summary>
    /// <param name="cfg">Service Bus configurator</param>
    /// <param name="domainName">Name of the domain (e.g., "account", "security")</param>
    /// <param name="eventTypes">Types of domain events this service publishes</param>
    public static void ConfigureDomainPublishing(
        this IServiceBusBusFactoryConfigurator cfg,
        string domainName,
        params Type[] eventTypes
    )
    {
        Guard.AgainstNullOrEmpty(domainName);
        Guard.AgainstNull(eventTypes);

        if (eventTypes.Length == 0)
            throw new ArgumentException(
                "At least one event type must be specified",
                nameof(eventTypes)
            );

        var topicName = $"{domainName.ToLowerInvariant()}-events";

        foreach (var eventType in eventTypes)
        {
            ValidateDomainEventType(eventType);
            ConfigureMessageForEventType(cfg, eventType, topicName);
        }
    }

    /// <summary>
    /// Configures cross-service consumption - allows a service to consume events from another domain.
    /// Example: Security service consuming AccountCreatedEvent from "account-events" topic.
    /// Creates a subscription named "{consumerService}-service" on "{sourceDomain}-events" topic.
    /// </summary>
    /// <param name="cfg">Service Bus configurator</param>
    /// <param name="consumerServiceName">Name of the consuming service (e.g., "security")</param>
    /// <param name="sourceDomainName">Name of the source domain (e.g., "account")</param>
    /// <param name="configureConsumers">Action to configure consumers for this subscription</param>
    public static void ConfigureCrossServiceConsumption(
        this IServiceBusBusFactoryConfigurator cfg,
        string consumerServiceName,
        string sourceDomainName,
        Action<IReceiveEndpointConfigurator> configureConsumers
    )
    {
        Guard.AgainstNullOrEmpty(consumerServiceName);
        Guard.AgainstNullOrEmpty(sourceDomainName);
        Guard.AgainstNull(configureConsumers);

        var subscriptionName = $"{consumerServiceName.ToLowerInvariant()}-service";
        var topicName = $"{sourceDomainName.ToLowerInvariant()}-events";

        cfg.SubscriptionEndpoint(
            subscriptionName,
            topicName,
            e =>
            {
                // Configure subscription properties for optimal performance and reliability
                e.ConfigureConsumeTopology = true;
                e.DefaultMessageTimeToLive = TimeSpan.FromDays(14);
                e.MaxDeliveryCount = 3;
                e.EnableBatchedOperations = true;

                // Dead letter queue configuration
                e.EnableDeadLetteringOnMessageExpiration = true;

                // Lock duration for message processing
                e.LockDuration = TimeSpan.FromMinutes(5);

                // Configure the consumers for this subscription
                configureConsumers(e);
            }
        );
    }

    /// <summary>
    /// Validates that the provided type is a domain event
    /// </summary>
    private static void ValidateDomainEventType(Type eventType)
    {
        if (!typeof(IDomainEvent).IsAssignableFrom(eventType))
        {
            throw new ArgumentException(
                $"Event type {eventType.Name} must implement IDomainEvent interface",
                nameof(eventType)
            );
        }
    }

    /// <summary>
    /// Configures a specific event type using reflection to call the generic method.
    /// All events in the domain use the same topic name for unified messaging.
    /// </summary>
    private static void ConfigureMessageForEventType(
        IServiceBusBusFactoryConfigurator cfg,
        Type eventType,
        string topicName
    )
    {
        // Get the generic method and make it specific to this event type
        var method = typeof(ServiceBusTopologyExtensions)
            .GetMethod(nameof(ConfigureMessage), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(eventType);

        // Call the generic method with the topic name
        method.Invoke(null, [cfg, topicName]);
    }

    /// <summary>
    /// Generic method that configures an event type to use the domain topic.
    /// All events in the domain are published to the same topic (e.g., "account-events").
    /// </summary>
    private static void ConfigureMessage<T>(IServiceBusBusFactoryConfigurator cfg, string topicName)
        where T : class, IDomainEvent
    {
        cfg.Message<T>(config =>
        {
            // Use the domain topic name (e.g., "account-events") instead of individual entity names
            config.SetEntityName(topicName);
        });
    }

    /// <summary>
    /// Gets all domain event types from the specified assembly for automatic discovery
    /// </summary>
    public static Type[] GetDomainEventTypes(Assembly assembly)
    {
        return assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IDomainEvent).IsAssignableFrom(t))
            .ToArray();
    }
}
