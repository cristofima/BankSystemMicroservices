using Microsoft.EntityFrameworkCore;
using Security.Infrastructure.Data;

namespace Security.Api.Services;

/// <summary>
/// Background service to periodically clean up expired tokens from the database
/// </summary>
public class TokenCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6); // Run every 6 hours

    public TokenCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TokenCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is being stopped
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // Wait 30 minutes before retry
            }
        }
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting token cleanup process");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

            // Remove tokens that expired more than 24 hours ago
            var cutoffDate = DateTime.UtcNow.AddDays(-1);

            var expiredTokens = await dbContext.RefreshTokens
                .Where(rt => rt.ExpiryDate < cutoffDate)
                .ToListAsync(cancellationToken);

            if (expiredTokens.Any())
            {
                dbContext.RefreshTokens.RemoveRange(expiredTokens);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {Count} expired tokens", expiredTokens.Count);
            }
            else
            {
                _logger.LogDebug("No expired tokens found for cleanup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token cleanup process");
            throw;
        }
    }
}
