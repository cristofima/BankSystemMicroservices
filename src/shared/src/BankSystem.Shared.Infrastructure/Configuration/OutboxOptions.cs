using System.ComponentModel.DataAnnotations;

namespace BankSystem.Shared.Infrastructure.Configuration;

public class OutboxOptions
{
    public const string SectionName = "MassTransit.Outbox";

    /// <summary>
    /// How often to check for pending outbox messages.
    /// Lower values provide faster delivery but increase database load.
    /// </summary>
    [Range(1, 10)]
    public int QueryDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Time window for detecting duplicate messages.
    /// Messages within this window are considered duplicates and ignored.
    /// </summary>
    [Range(5, 10)]
    public int DuplicateDetectionWindowMinutes { get; set; } = 5;

    /// <summary>
    /// Disable inbox cleanup service if not needed.
    /// The cleanup service removes old processed messages from the inbox.
    /// </summary>
    public bool DisableInboxCleanupService { get; set; } = false;
}
