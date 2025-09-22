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
}
