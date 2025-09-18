using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace BankSystem.Shared.Infrastructure.DomainEvents;

/// <summary>
/// Entity Framework Core interceptor that automatically dispatches domain events using the Outbox pattern.
/// This interceptor ensures domain events are captured and stored in the Outbox before the main SaveChanges operation,
/// making domain event publishing completely transparent to repositories while maintaining transactional integrity.
/// </summary>
public class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<DomainEventDispatchInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatchInterceptor"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for this interceptor</param>
    public DomainEventDispatchInterceptor(ILogger<DomainEventDispatchInterceptor> logger)
    {
        Guard.AgainstNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        var dbContext = eventData.Context;
        if (dbContext == null || result.HasResult)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        try
        {
            // CRITICAL: Dispatch events BEFORE SaveChanges for Outbox pattern
            // This ensures events are stored in Outbox tables within the same transaction
            _logger.LogDebug("Dispatching domain events BEFORE SaveChanges for Outbox pattern");

            // Find all aggregate roots with domain events
            var aggregatesWithEvents = GetAggregatesWithEvents(dbContext);

            if (aggregatesWithEvents.Count > 0)
            {
                _logger.LogDebug(
                    "Found {AggregateCount} aggregates with domain events BEFORE SaveChanges",
                    aggregatesWithEvents.Count
                );

                // Use the DbContext-aware dispatcher for proper Outbox integration
                var dbContextDispatcher = dbContext.GetService<IDbContextDomainEventDispatcher>();

                _logger.LogDebug(
                    "Using DbContextDomainEventDispatcher for Outbox pattern BEFORE SaveChanges"
                );

                await dbContextDispatcher.DispatchEventsWithDbContextAsync(
                    dbContext,
                    aggregatesWithEvents,
                    cancellationToken
                );

                _logger.LogInformation(
                    "Successfully dispatched domain events from {AggregateCount} aggregates BEFORE SaveChanges",
                    aggregatesWithEvents.Count
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch domain events BEFORE SaveChanges");

            // Rethrow because we're in the middle of the transaction
            throw;
        }

        // Log the saving attempt
        _logger.LogDebug(
            "SaveChanges intercepted - domain events dispatched to Outbox, proceeding with database save"
        );

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default
    )
    {
        // Now we need to clear events AFTER successful SaveChanges
        var context = eventData.Context;
        if (context != null && result > 0)
        {
            try
            {
                // Find aggregates that had events and clear them now
                var aggregatesWithEvents = context
                    .ChangeTracker.Entries<IAggregateRoot>()
                    .Where(entry => entry.Entity.DomainEvents?.Count > 0)
                    .Select(entry => entry.Entity)
                    .ToList();

                if (aggregatesWithEvents.Count > 0)
                {
                    _logger.LogDebug(
                        "Clearing domain events from {Count} aggregates after successful SaveChanges",
                        aggregatesWithEvents.Count
                    );

                    foreach (var aggregate in aggregatesWithEvents)
                    {
                        aggregate.ClearDomainEvents();
                    }

                    _logger.LogDebug(
                        "Events cleared from {Count} aggregates - Outbox pattern completed successfully",
                        aggregatesWithEvents.Count
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to clear domain events after SaveChanges - not critical"
                );
                // Don't throw - SaveChanges was successful
            }

            _logger.LogDebug(
                "SaveChanges completed successfully. Domain events were dispatched to Outbox BEFORE commit and cleared AFTER. Changes saved: {ChangeCount}",
                result
            );
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Intercepts the synchronous SavingChanges operation to provide appropriate warnings about Outbox pattern limitations.
    /// This method is called by Entity Framework Core for synchronous save operations.
    /// </summary>
    /// <param name="eventData">Contextual information about the DbContext and the database operation being performed</param>
    /// <param name="result">The current result of the save operation, which may be modified by this interceptor</param>
    /// <returns>The interception result for the save operation</returns>
    /// <remarks>
    /// The Outbox pattern requires asynchronous operations for proper domain event dispatching.
    /// This synchronous method logs warnings and delegates to the base implementation without
    /// processing domain events. Applications should use SaveChangesAsync for proper event handling.
    /// </remarks>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        _logger.LogDebug(
            "SaveChanges intercepted (synchronous) - Outbox pattern requires async operations"
        );

        // For synchronous SaveChanges, we cannot properly implement the Outbox pattern
        // Log a warning and proceed with the base implementation
        _logger.LogWarning(
            "Synchronous SaveChanges detected - domain events will NOT be processed via Outbox. Use SaveChangesAsync instead."
        );

        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Retrieves all aggregate root entities that have pending domain events from the EF Core ChangeTracker.
    /// This method filters tracked entities to identify those with domain events that need to be published.
    /// </summary>
    /// <param name="context">The EF Core DbContext containing the ChangeTracker with entity entries</param>
    /// <returns>A list of aggregate root entities that contain unpublished domain events</returns>
    /// <remarks>
    /// This method uses the EF Core ChangeTracker to identify IAggregateRoot entities that have
    /// domain events pending publication. Only entities with a non-empty DomainEvents collection
    /// are returned. This filtering ensures that only aggregates requiring event dispatch are
    /// processed by the Outbox pattern, optimizing performance by avoiding unnecessary operations
    /// on entities without events.
    /// </remarks>
    private static List<IAggregateRoot> GetAggregatesWithEvents(DbContext context)
    {
        return context
            .ChangeTracker.Entries<IAggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents?.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();
    }
}
