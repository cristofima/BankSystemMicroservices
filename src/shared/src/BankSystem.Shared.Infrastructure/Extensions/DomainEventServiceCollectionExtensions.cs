using System.Diagnostics.CodeAnalysis;
using BankSystem.Shared.Infrastructure.DomainEvents;
using BankSystem.Shared.Kernel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BankSystem.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring domain event services in the dependency injection container.
/// Provides methods to register domain event emission, and dispatching services.
/// </summary>
[ExcludeFromCodeCoverage]
public static class DomainEventServiceCollectionExtensions
{
    /// <summary>
    /// Registers domain event emission services for automatic event publishing.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEventEmission(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventEmitter, DomainEventEmitter>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return services;
    }
}
