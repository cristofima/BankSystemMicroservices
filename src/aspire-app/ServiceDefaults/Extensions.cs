using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace BankSystem.ServiceDefaults;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and
// OpenTelemetry. This project should be referenced by each service project in your solution. To
// learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
[ExcludeFromCodeCoverage]
public static partial class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    private static readonly string[] ExcludedPaths =
    [
        "/health", // Health checks
        "/alive", // Liveness checks
        "/ready", // Readiness checks
        "/live", // Alternative liveness
        "/healthz", // Kubernetes health checks
        "/livez", // Kubernetes liveness
        "/readyz", // Kubernetes readiness
        "/swagger", // Swagger UI
        "/swagger-ui", // Alternative Swagger UI
        "/scalar", // Scalar API documentation
        "/openapi", // OpenAPI specification
        "/api-docs", // API documentation
        "/favicon.ico", // Favicon requests
        "/robots.txt", // Robots file
        "/sitemap.xml", // Sitemap
        "/.well-known", // Well-known URIs
        "/metrics", // Prometheus metrics
        "/ping", // Simple ping endpoint
        "/version", // Version endpoint
        "/status" // Status endpoint
        ,
    ];

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Restrict the allowed schemes for service discovery.
        builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        {
            if (builder.Environment.IsDevelopment())
            {
                options.AllowedSchemes = ["http", "https"];
            }
            else
            {
                options.AllowedSchemes = ["https"];
            }
        });

        // Add Serilog configuration
        builder.AddSerilogLogging();

        return builder;
    }

    /// <summary>
    /// Configures Serilog as the primary logging provider.
    /// </summary>
    public static TBuilder AddSerilogLogging<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSerilog(
            (services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithEnvironmentName()
                    .Enrich.WithProcessId()
                    .Enrich.WithThreadId()
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
                    )
                    .WriteTo.File(
                        path: "logs/banksystem-.txt",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
                    );

                // Set minimum log level based on environment
                if (builder.Environment.IsDevelopment())
                {
                    configuration.MinimumLevel.Debug();
                }
                else
                {
                    configuration.MinimumLevel.Information();
                }

                // Override specific namespaces to reduce noise
                configuration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
                configuration.MinimumLevel.Override(
                    "Microsoft.Hosting.Lifetime",
                    LogEventLevel.Information
                );
                configuration.MinimumLevel.Override("System", LogEventLevel.Warning);
            }
        );

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                        // Configure trace filtering to reduce noise and focus on business endpoints
                        options.Filter = context =>
                        {
                            var path = context.Request.Path.Value;

                            // Priority 1: Include all versioned API endpoints (/api/v1, /api/v2, etc.)
                            if (
                                Regex.IsMatch(
                                    path ?? string.Empty,
                                    @"^/api/v\d+/?",
                                    RegexOptions.IgnoreCase,
                                    TimeSpan.FromMilliseconds(500)
                                )
                            )
                                return true;

                            // Priority 2: Exclude infrastructure/documentation endpoints to reduce
                            // telemetry noise
                            return !IsExcludedPath(path);
                        }
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
        );

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Enable the Azure Monitor exporter
        if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder
            .Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // All health checks must pass for app to be considered ready to accept traffic after starting
        var healthEndpoint = app.MapHealthChecks(
            HealthEndpointPath,
            new HealthCheckOptions { ResponseWriter = WriteHealthCheckResponse }
        );

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        var alivenessEndpoint = app.MapHealthChecks(
            AlivenessEndpointPath,
            new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") }
        );

        // Only require authorization in non-development environments
        if (!app.Environment.IsDevelopment())
        {
            healthEndpoint.RequireAuthorization();
            alivenessEndpoint.RequireAuthorization();
        }

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
                exception = context
                    .RequestServices.GetRequiredService<IHostEnvironment>()
                    .IsDevelopment()
                    ? x.Value.Exception?.Message
                : x.Value.Exception != null ? "An error occurred"
                : null,
                duration = x.Value.Duration.ToString(),
            }),
            duration = report.TotalDuration.ToString(),
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    /// <summary>
    /// Determines if a path should be excluded from tracing based on common non-business endpoints.
    /// </summary>
    /// <param name="path">The request path to evaluate</param>
    /// <returns>True if the path should be excluded from tracing, false otherwise</returns>
    private static bool IsExcludedPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        var pathLower = path.ToLowerInvariant();

        return ExcludedPaths.Any(excluded => pathLower.StartsWith(excluded));
    }
}
