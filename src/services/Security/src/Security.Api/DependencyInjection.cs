using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using BankSystem.Shared.WebApiDefaults.Configuration;
using BankSystem.Shared.WebApiDefaults.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Security.Api.Services;
using Security.Infrastructure.Data;

namespace Security.Api;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Configure common services with Security-specific controller configuration
        services.AddWebApiDefaults(configuration, "Security API");

        // Add Security-specific health checks
        services.AddDbContextHealthCheck<SecurityDbContext>();

        // Configure Inter-Service Security Options
        services.Configure<InterServiceSecurityOptions>(
            configuration.GetSection("InterServiceSecurity")
        );

        // Add inter-service security validation
        services.PostConfigure<InterServiceSecurityOptions>(options =>
        {
            // Validate authentication method configuration
            if (options.Authentication.Method == AuthenticationMethod.ApiKey)
            {
                if (string.IsNullOrWhiteSpace(options.ApiKey.Value))
                {
                    throw new InvalidOperationException(
                        "API Key is required when using ApiKey authentication method. "
                            + "Please configure InterServiceSecurity:ApiKey:Value in your settings."
                    );
                }

                if (options.ApiKey.Value.Length < 16)
                {
                    throw new InvalidOperationException(
                        "API Key must be at least 16 characters long for security reasons."
                    );
                }
            }

            if (options.Authentication.AllowedServices?.Count < 1)
            {
                throw new InvalidOperationException(
                    "At least one allowed service must be configured in InterServiceSecurity:Authentication:AllowedServices."
                );
            }
        });

        // Add memory cache for token revocation
        services.AddMemoryCache();

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
            options.AddPolicy(
                "DefaultPolicy",
                builder =>
                {
                    builder
                        .WithOrigins(
                            configuration.GetSection("AllowedOrigins").Get<string[]>() ?? []
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                }
            );
        });

        // Add AutoMapper with gRPC mapping profile
        services.AddAutoMapper(typeof(Mapping.GrpcMappingProfile));

        // Add rate limiting with Security-specific policies (in addition to default)
        services.Configure<RateLimiterOptions>(options =>
        {
            // Security-specific authentication rate limiting
            options.AddFixedWindowLimiter(
                "AuthPolicy",
                limiterOptions =>
                {
                    limiterOptions.PermitLimit = 5;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 0;
                }
            );

            // Security-specific refresh token rate limiting
            options.AddFixedWindowLimiter(
                "RefreshPolicy",
                limiterOptions =>
                {
                    limiterOptions.PermitLimit = 10;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 2;
                }
            );
        });

        return services;
    }
}
