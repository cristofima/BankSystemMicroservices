using System.Threading.RateLimiting;
using BankSystem.ApiGateway.Configuration;
using BankSystem.ApiGateway.Middlewares;
using BankSystem.Shared.Infrastructure.Extensions;
using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.WebApiDefaults.Middlewares;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

namespace BankSystem.ApiGateway.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddEndpointsApiExplorer();

        // Add CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                var allowedOrigins =
                    configuration.GetSection("Gateway:Cors:AllowedOrigins").Get<string[]>() ?? [];
                if (allowedOrigins.Length > 0)
                {
                    builder
                        .WithOrigins(allowedOrigins)
                        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                        .WithHeaders(HttpHeaderConstants.CommonAllowedHeaders)
                        .WithExposedHeaders(HttpHeaderConstants.CommonExposedHeaders)
                        .AllowCredentials();
                }
                else
                {
                    throw new InvalidOperationException(
                        "CORS configuration is missing or invalid. Please specify allowed origins in the configuration."
                    );
                }
            });
        });

        // Add HTTP client factory
        services.AddHttpClient();

        // Add memory cache
        services.AddMemoryCache();

        // Add middlewares that implement IMiddleware (only these need DI registration)
        services.AddScoped<CorrelationIdMiddleware>();
        services.AddScoped<ExceptionHandlingMiddleware>();

        return services;
    }

    public static IServiceCollection AddAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var authSettings = configuration.GetSection("Authentication");
        services.AddJwtAuthentication(authSettings);

        services.ConfigureAuthorizationPolicies();

        return services;
    }

    public static IServiceCollection AddHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Configure health check options
        services.Configure<GatewayHealthCheckOptions>(
            configuration.GetSection(GatewayHealthCheckOptions.SectionName)
        );
        var healthCheckOptions =
            configuration
                .GetSection(GatewayHealthCheckOptions.SectionName)
                .Get<GatewayHealthCheckOptions>() ?? new GatewayHealthCheckOptions();

        var healthChecksBuilder = services.AddHealthChecks();

        // Add external service health checks
        foreach (var serviceCheck in healthCheckOptions.Services)
        {
            var timeout = TimeSpan.FromSeconds(
                serviceCheck.TimeoutSeconds ?? healthCheckOptions.TimeoutSeconds
            );
            var failureStatus = Enum.TryParse<HealthStatus>(
                serviceCheck.FailureStatus,
                out var status
            )
                ? status
                : HealthStatus.Degraded;

            var displayName = !string.IsNullOrEmpty(serviceCheck.DisplayName)
                ? serviceCheck.DisplayName
                : $"{serviceCheck.Name} Health Check";

            healthChecksBuilder.AddUrlGroup(
                uri: new Uri(serviceCheck.Uri),
                name: displayName,
                failureStatus: failureStatus,
                timeout: timeout,
                tags: serviceCheck.Tags
            );
        }

        return services;
    }

    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddRateLimiter(options =>
        {
            // Global rate limiting
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User?.Identity?.Name
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                    }
                )
            );

            // Auth endpoint rate limiting
            options.AddFixedWindowLimiter(
                "auth",
                options =>
                {
                    options.PermitLimit = 5;
                    options.Window = TimeSpan.FromMinutes(1);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 0;
                }
            );

            // Transaction endpoint rate limiting
            options.AddFixedWindowLimiter(
                "transactions",
                options =>
                {
                    options.PermitLimit = 20;
                    options.Window = TimeSpan.FromMinutes(1);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 5;
                }
            );

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsync(
                    "Rate limit exceeded. Please try again later.",
                    token
                );
            };
        });

        return services;
    }

    public static Task<WebApplication> ConfigurePipelineAsync(this WebApplication app)
    {
        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseHsts();
        }

        // Security headers - centralized for all microservices
        app.UseSecurityHeaders(app.Environment);

        // HTTPS redirection
        app.UseHttpsRedirection();

        // Custom middleware - Exception handling must come before authentication
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Request logging
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = (httpContext, _, ex) =>
            {
                if (ex != null)
                    return LogEventLevel.Error;

                var isServerError = httpContext.Response.StatusCode > 499;
                return isServerError ? LogEventLevel.Error : LogEventLevel.Information;
            };
        });

        // Rate limiting
        app.UseRateLimiter();

        // CORS
        app.UseCors();

        // Selective authentication middleware (must come BEFORE Auth)
        app.UseSelectiveAuthentication();

        // Authentication and Authorization - Only applied after selective check
        app.UseAuthentication();
        app.UseAuthorization();

        // Response transform middleware after auth
        app.UseMiddleware<ResponseTransformMiddleware>();

        // YARP reverse proxy
        app.MapReverseProxy();

        return Task.FromResult(app);
    }
}
