using Asp.Versioning;
using BankSystem.Shared.Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace BankSystem.Shared.ServiceDefaults.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Adds common service defaults for all microservices in the bank system.
    /// This includes controllers, authentication, authorization, health checks, API documentation, versioning, and CORS.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="apiTitle">The title for the API documentation (default: "Bank System API")</param>
    /// <param name="configureControllers">Optional action to configure controller options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddServiceDefaults(
        this IServiceCollection services,
        IConfiguration configuration,
        string apiTitle = "Bank System API",
        Action<MvcOptions>? configureControllers = null)
    {
        // Add Controllers with common configuration
        services.AddControllers(options =>
        {
            // Global model validation
            options.ModelValidatorProviders.Clear();

            // Allow services to add additional configuration
            configureControllers?.Invoke(options);
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

        // Add JWT Authentication from shared infrastructure
        services.AddJwtAuthentication(configuration);

        // Add authorization
        services.AddAuthorization();

        // Add health checks
        services.AddHealthChecks();

        // Add OpenAPI/Swagger
        services.AddOpenApi();

        // Add CORS with default policy
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                if (allowedOrigins.Length == 0 || allowedOrigins.Contains("*"))
                {
                    var serviceProvider = services.BuildServiceProvider();
                    var env = serviceProvider.GetRequiredService<IHostEnvironment>();
                    if (!env.IsDevelopment())
                    {
                        throw new InvalidOperationException("CORS policy is too permissive. Please configure allowed origins explicitly in production.");
                    }

                    policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                }
                else
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                }
            });
        });

        // Add rate limiting with default policies
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Default API rate limit
            options.AddFixedWindowLimiter("DefaultApi", limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 10;
            });
        });

        return services;
    }

    /// <summary>
    /// Adds a DbContext health check to the service collection.
    /// </summary>
    /// <typeparam name="TDbContext">The type of DbContext to check</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="name">The name of the health check (default: "database")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDbContextHealthCheck<TDbContext>(
        this IServiceCollection services,
        string name = "database") where TDbContext : DbContext
    {
        services.AddHealthChecks()
            .AddDbContextCheck<TDbContext>(name);

        return services;
    }

    /// <summary>
    /// Configures the common middleware pipeline for all microservices.
    /// This includes development-specific middleware, security headers, authentication, and health checks.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="apiTitle">The title for the API documentation</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseServiceDefaults(this WebApplication app, string apiTitle = "Bank System API")
    {
        // Development-specific middleware
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle(apiTitle);
            });
        }
        else
        {
            // Production-specific middleware
            app.UseHsts(); // HTTP Strict Transport Security
        }

        // Core middleware pipeline
        app.UseHttpsRedirection();

        // Rate limiting
        app.UseRateLimiter();

        // CORS
        app.UseCors();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Configure health checks with detailed JSON response
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        });

        // Default redirect to API documentation
        app.Map("/", () =>
        {
            var redirectTarget = app.Environment.IsDevelopment() ? "/scalar" : "/";
            return Results.Redirect(redirectTarget);
        });

        return app;
    }

    /// <summary>
    /// Writes a detailed JSON health check response including status, individual checks, and timing information.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="report">The health check report</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                exception = x.Value.Exception?.Message,
                duration = x.Value.Duration.ToString()
            }),
            duration = report.TotalDuration.ToString()
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}