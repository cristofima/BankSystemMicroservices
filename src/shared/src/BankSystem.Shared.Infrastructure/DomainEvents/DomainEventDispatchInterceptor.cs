using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BankSystem.Shared.Infrastructure.DomainEvents;

/// <summary>
/// EF Core interceptor that automatically dispatches domain events after successful SaveChanges
/// This makes domain event publishing completely transparent to repositories
/// </summary>
public class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatchInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatchInterceptor"/> class.
    /// </summary>
    public DomainEventDispatchInterceptor(
        IServiceProvider serviceProvider,
        ILogger<DomainEventDispatchInterceptor> logger
    )
    {
        Guard.AgainstNull(serviceProvider, nameof(logger));
        Guard.AgainstNull(logger, nameof(logger));

        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        // Log the saving attempt
        _logger.LogDebug("SaveChanges intercepted - preparing to dispatch domain events");

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default
    )
    {
        var context = eventData.Context;
        if (context == null)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        try
        {
            // Find all aggregate roots with domain events
            var aggregatesWithEvents = GetAggregatesWithEvents(context);

            if (aggregatesWithEvents.Count > 0)
            {
                _logger.LogDebug(
                    "Found {AggregateCount} aggregates with domain events after SaveChanges",
                    aggregatesWithEvents.Count
                );

                // Use scoped service to dispatch events
                using var scope = _serviceProvider.CreateScope();
                var eventDispatcher =
                    scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

                await eventDispatcher.DispatchEventsAsync(aggregatesWithEvents, cancellationToken);

                _logger.LogInformation(
                    "Successfully dispatched domain events from {AggregateCount} aggregates",
                    aggregatesWithEvents.Count
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch domain events after SaveChanges");

            // Important: We don't rethrow here because the database operation already succeeded
            // Domain event dispatch failures should be handled by retry mechanisms or dead letter queues
            // Throwing here would make the entire operation appear failed when the data was actually saved
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        _logger.LogDebug(
            "SaveChanges intercepted (synchronous) - preparing to dispatch domain events"
        );
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var context = eventData.Context;
        if (context == null)
        {
            return base.SavedChanges(eventData, result);
        }

        try
        {
            // Find all aggregate roots with domain events
            var aggregatesWithEvents = GetAggregatesWithEvents(context);

            if (aggregatesWithEvents.Count > 0)
            {
                _logger.LogDebug(
                    "Found {AggregateCount} aggregates with domain events after SaveChanges (sync)",
                    aggregatesWithEvents.Count
                );

                // Use scoped service to dispatch events synchronously
                using var scope = _serviceProvider.CreateScope();
                var eventDispatcher =
                    scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

                // Note: This is synchronous dispatch - should be avoided in production
                // The async version above is preferred
                eventDispatcher
                    .DispatchEventsAsync(aggregatesWithEvents, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                _logger.LogInformation(
                    "Successfully dispatched domain events from {AggregateCount} aggregates (sync)",
                    aggregatesWithEvents.Count
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch domain events after SaveChanges (sync)");
            // Same as async - don't rethrow as database operation succeeded
        }

        return base.SavedChanges(eventData, result);
    }

    private static List<IAggregateRoot> GetAggregatesWithEvents(DbContext context)
    {
        return context
            .ChangeTracker.Entries<IAggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents?.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();
    }
}
