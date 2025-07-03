using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Security.Api.Filters;
using Security.Api.Middleware;
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
        services.AddControllers(options =>
        {
            // Global model validation
            options.ModelValidatorProviders.Clear();
            
            // Add custom filters
            options.Filters.Add<GlobalExceptionFilter>();
        });

        // Configure API versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader("version"),
                new HeaderApiVersionReader("X-Version"));
        }).AddApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
        });

        // Configure OpenAPI/Swagger
        services.AddOpenApi();

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

        // Add CORS
        services.AddCors(options =>
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

        // Add rate limiting
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("AuthPolicy", limiterOptions =>
            {
                limiterOptions.PermitLimit = 5;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("RefreshPolicy", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 2;
            });
        });

        // Add health checks
        services.AddHealthChecks()
            .AddDbContextCheck<SecurityDbContext>("database");

        return services;
    }
}