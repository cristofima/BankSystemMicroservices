using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using Asp.Versioning;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.Infrastructure.Extensions;
using BankSystem.Shared.WebApiDefaults.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace BankSystem.Shared.WebApiDefaults.Extensions;

[ExcludeFromCodeCoverage]
public static class WebApiExtensions
{
    private const string RateLimitingSection = "RateLimiting:DefaultApi";

    /// <summary>
    /// Adds common Web API defaults for all microservices in the bank system.
    /// This includes controllers, authentication, authorization, API documentation, versioning, CORS, and rate limiting.
    /// Note: This should be used together with Aspire ServiceDefaults for complete configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="apiTitle">The title for the API documentation (default: "Bank System API")</param>
    /// <param name="configureControllers">Optional action to configure controller options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddWebApiDefaults(
        this IServiceCollection services,
        IConfiguration configuration,
        string apiTitle = "Bank System API",
        Action<MvcOptions>? configureControllers = null
    )
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
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new QueryStringApiVersionReader("version"),
                    new HeaderApiVersionReader("X-Version")
                );
            })
            .AddApiExplorer(setup =>
            {
                setup.GroupNameFormat = "'v'VVV";
                setup.SubstituteApiVersionInUrl = true;
            });

        // Add JWT Authentication from shared infrastructure
        services.AddJwtAuthentication(configuration);

        // Add HTTP context accessor for services that need it
        services.AddHttpContextAccessor();
        // Add custom current user service
        services.AddScoped<ICurrentUser, CurrentUser>();

        // Add authorization
        services.AddAuthorization();

        // Add OpenAPI/Swagger
        services.AddOpenApi();

        // Add CORS with default policy
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var allowedOrigins =
                    configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                if (allowedOrigins.Length == 0 || allowedOrigins.Contains("*"))
                {
                    throw new InvalidOperationException(
                        "CORS policy is too permissive. Please configure allowed origins explicitly."
                    );
                }

                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // Add rate limiting with default policies
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Default API rate limit
            options.AddFixedWindowLimiter(
                "DefaultApi",
                limiterOptions =>
                {
                    var permitLimit = configuration.GetValue(
                        $"{RateLimitingSection}:PermitLimit",
                        100
                    );
                    Guard.AgainstZeroOrNegative(permitLimit, "permitLimit");

                    var windowSize = configuration.GetValue(
                        $"{RateLimitingSection}:WindowMinutes",
                        1
                    );
                    Guard.AgainstZeroOrNegative(windowSize, "windowSize");

                    var queueLimit = configuration.GetValue(
                        $"{RateLimitingSection}:QueueLimit",
                        10
                    );
                    Guard.AgainstNegative(queueLimit, "queueLimit");

                    limiterOptions.PermitLimit = permitLimit;
                    limiterOptions.Window = TimeSpan.FromMinutes(windowSize);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = queueLimit;
                }
            );
        });

        return services;
    }

    /// <summary>
    /// Adds a DbContext health check to the service collection.
    /// This complements the basic health checks from Aspire ServiceDefaults.
    /// </summary>
    /// <typeparam name="TDbContext">The type of DbContext to check</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="name">The name of the health check (default: "database")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDbContextHealthCheck<TDbContext>(
        this IServiceCollection services,
        string name = "database"
    )
        where TDbContext : DbContext
    {
        services.AddHealthChecks().AddDbContextCheck<TDbContext>(name);

        return services;
    }

    /// <summary>
    /// Configures the common Web API middleware pipeline for all microservices.
    /// This includes API-specific middleware like authentication, CORS, and documentation.
    /// Note: Use this AFTER configuring Aspire ServiceDefaults middleware.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="apiTitle">The title for the API documentation</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseWebApiDefaults(
        this WebApplication app,
        string apiTitle = "Bank System API"
    )
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

        // Core middleware pipeline (order matters!)
        app.UseHttpsRedirection();

        // Rate limiting
        app.UseRateLimiter();

        // CORS
        app.UseCors();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Default redirect to API documentation
        app.Map(
            "/",
            () =>
            {
                var redirectTarget = app.Environment.IsDevelopment() ? "/scalar" : "/health";
                return Results.Redirect(redirectTarget);
            }
        );

        return app;
    }
}
