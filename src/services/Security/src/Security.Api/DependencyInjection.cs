using BankSystem.Shared.WebApiDefaults.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Security.Api.Filters;
using Security.Api.Services;
using Security.Infrastructure.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;

namespace Security.Api;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddWebApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure common services with Security-specific controller configuration
        services.AddWebApiDefaults(
            configuration,
            "Security API",
            options =>
            {
                // Add global exception filter specific to Security service
                options.Filters.Add<GlobalExceptionFilter>();
            });

        // Add Security-specific health checks
        services.AddDbContextHealthCheck<SecurityDbContext>("database");

        // Add memory cache for token revocation
        services.AddMemoryCache();

        // Add HTTP context accessor for services that need it
        services.AddHttpContextAccessor();

        // Register API helper services
        services.AddScoped<IHttpContextInfoService, HttpContextInfoService>();
        services.AddScoped<IApiResponseService, ApiResponseService>();

        // Register middleware services
        services.AddScoped<ITokenRevocationService, TokenRevocationService>();

        // Register background services
        services.AddHostedService<RevokedTokensBackgroundService>();
        services.AddHostedService<TokenCleanupBackgroundService>();

        // Override default CORS with Security-specific configuration
        services.Configure<CorsOptions>(options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                builder
                    .WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        // Add rate limiting with Security-specific policies (in addition to default)
        services.Configure<RateLimiterOptions>(options =>
        {
            // Security-specific authentication rate limiting
            options.AddFixedWindowLimiter("AuthPolicy", limiterOptions =>
            {
                limiterOptions.PermitLimit = 5;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });

            // Security-specific refresh token rate limiting
            options.AddFixedWindowLimiter("RefreshPolicy", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 2;
            });
        });

        return services;
    }
}