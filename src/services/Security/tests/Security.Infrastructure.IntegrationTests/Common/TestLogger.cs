using Microsoft.Extensions.Logging;

namespace Security.Infrastructure.IntegrationTests.Common;

/// <summary>
/// Test logger implementation for capturing log entries during integration tests
/// </summary>
/// <typeparam name="T">The category type for the logger</typeparam>
public class TestLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _logs = new();

    public IReadOnlyList<LogEntry> Logs => _logs.AsReadOnly();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return new TestScope();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _logs.Add(new LogEntry(logLevel, eventId, message, exception, typeof(T).Name));
    }

    public void Clear()
    {
        _logs.Clear();
    }

    public LogEntry? GetLatestLog()
    {
        return _logs.Count > 0 ? _logs[^1] : null;
    }

    public IEnumerable<LogEntry> GetLogsOfLevel(LogLevel level)
    {
        return _logs.Where(log => log.LogLevel == level);
    }

    public bool HasLogWithMessage(string message)
    {
        return _logs.Any(log => log.Message.Contains(message, StringComparison.OrdinalIgnoreCase));
    }

    private class TestScope : IDisposable
    {
        public void Dispose()
        {
            // No cleanup needed for test scope
        }
    }
}

/// <summary>
/// Represents a log entry captured during testing
/// </summary>
public record LogEntry(
    LogLevel LogLevel,
    EventId EventId,
    string Message,
    Exception? Exception,
    string CategoryName)
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
