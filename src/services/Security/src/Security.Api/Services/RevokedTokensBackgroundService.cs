using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Security.Infrastructure.Data;

namespace Security.Api.Services;

/// <summary>
/// Background service to load revoked tokens into memory cache on startup
/// Based on the pattern from the article for persisting revocation across restarts
/// </summary>
public class RevokedTokensBackgroundService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RevokedTokensBackgroundService> _logger;

    public RevokedTokensBackgroundService(
        IServiceProvider serviceProvider,
        IMemoryCache memoryCache,
        ILogger<RevokedTokensBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading revoked tokens into memory cache...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

            // Load all currently revoked tokens that haven't expired
            var revokedTokens = await dbContext.RefreshTokens
                .Where(rt => rt.IsRevoked && rt.ExpiryDate > DateTime.UtcNow)
                .Select(rt => new { rt.JwtId, rt.ExpiryDate })
                .ToListAsync(cancellationToken);

            foreach (var token in revokedTokens)
            {
                var cacheKey = $"revoked_token_{token.JwtId}";
                var remainingTime = token.ExpiryDate.Subtract(DateTime.UtcNow);
                
                if (remainingTime > TimeSpan.Zero)
                {
                    _memoryCache.Set(cacheKey, DateTime.UtcNow, remainingTime);
                }
            }

            _logger.LogInformation("Loaded {Count} revoked tokens into memory cache", revokedTokens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading revoked tokens into memory cache");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Revoked tokens background service stopping");
        return Task.CompletedTask;
    }
}
