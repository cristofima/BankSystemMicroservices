using System.Diagnostics.CodeAnalysis;
using BankSystem.Shared.WebApiDefaults.Authentication;
using BankSystem.Shared.WebApiDefaults.Configuration;
using BankSystem.Shared.WebApiDefaults.Constants;
using BankSystem.Shared.WebApiDefaults.Interceptors;
using Microsoft.Extensions.Options;

namespace BankSystem.Shared.WebApiDefaults.Extensions;

/// <summary>
/// Extensions for configuring gRPC services with common defaults for the Bank System.
/// Provides authentication, authorization, and configuration for gRPC services.
/// </summary>
[ExcludeFromCodeCoverage]
public static class GrpcExtensions
{
    /// <summary>
    /// Adds gRPC services with Bank System defaults including enhanced inter-service security.
    /// Supports both API Key (development) and mTLS (production) authentication methods.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="environment">The application environment</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGrpcDefaults(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        var isDevelopment = environment.IsDevelopment();

        // Configure inter-service security options
        var interServiceOptions = new InterServiceSecurityOptions();
        configuration.GetSection(InterServiceSecurityOptions.SectionName).Bind(interServiceOptions);
        services.Configure<InterServiceSecurityOptions>(
            configuration.GetSection(InterServiceSecurityOptions.SectionName)
        );
        services.PostConfigure<InterServiceSecurityOptions>(opts =>
        {
            // Set authentication method based on environment if not explicitly configured
            if (
                opts.Authentication.Method == AuthenticationMethod.ApiKey
                && !isDevelopment
                && opts.MTls.IsValid()
            )
            {
                opts.Authentication.Method = AuthenticationMethod.MTls;
            }
        });

        // Add gRPC services with enhanced configuration
        services.AddGrpc(options =>
        {
            // Configure message size limits from options
            options.MaxReceiveMessageSize = interServiceOptions.Grpc.MaxMessageSize;
            options.MaxSendMessageSize = interServiceOptions.Grpc.MaxMessageSize;

            // Enable detailed errors based on environment and configuration
            options.EnableDetailedErrors =
                isDevelopment && interServiceOptions.Grpc.EnableDetailedErrors;

            // Add inter-service authentication interceptor
            options.Interceptors.Add<InterServiceAuthenticationInterceptor>();
        });

        // Register the interceptor
        services.AddTransient<InterServiceAuthenticationInterceptor>();

        // Add gRPC reflection based on configuration
        if (isDevelopment && interServiceOptions.Grpc.Reflection.Enabled)
        {
            services.AddGrpcReflection();
        }

        // Configure authentication schemes
        ConfigureAuthentication(services, interServiceOptions);

        // Configure authorization policies
        ConfigureAuthorization(services, interServiceOptions);

        return services;
    }

    /// <summary>
    /// Configures gRPC middleware and endpoints for the Web API.
    /// This should be called after UseWebApiDefaults.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseGrpcDefaults(this WebApplication app)
    {
        // Get inter-service security options to check if reflection is enabled
        var interServiceOptions = app
            .Services.GetRequiredService<IOptions<InterServiceSecurityOptions>>()
            .Value;

        // Add gRPC reflection only if it was registered and enabled
        if (app.Environment.IsDevelopment() && interServiceOptions.Grpc.Reflection.Enabled)
        {
            app.MapGrpcReflectionService();
        }

        return app;
    }

    /// <summary>
    /// Maps a gRPC service with proper authentication based on environment.
    /// In development, uses API Key authentication for testing.
    /// Maps a gRPC service with enhanced authentication based on configuration.
    /// Supports both API Key (development) and mTLS (production) authentication methods.
    /// </summary>
    /// <typeparam name="TService">The gRPC service type</typeparam>
    /// <param name="app">The web application</param>
    /// <param name="requireAuth">Whether to require authentication (default: true)</param>
    /// <returns>The gRPC service endpoint convention builder</returns>
    public static GrpcServiceEndpointConventionBuilder MapGrpcServiceWithAuth<TService>(
        this WebApplication app,
        bool requireAuth = true
    )
        where TService : class
    {
        var endpointBuilder = app.MapGrpcService<TService>();

        if (!requireAuth)
            return endpointBuilder;

        // Get inter-service security options to determine authentication method
        var interServiceOptions = app
            .Services.GetRequiredService<IOptions<InterServiceSecurityOptions>>()
            .Value;
        var isDevelopment = app.Environment.IsDevelopment();

        if (
            isDevelopment
            || interServiceOptions.Authentication.Method == AuthenticationMethod.ApiKey
        )
        {
            // Use API Key authentication with policy-based authorization
            endpointBuilder.RequireAuthorization(InterServiceConstants.ApiKeyScheme);
        }
        else
        {
            // Use mTLS authentication with certificate validation
            endpointBuilder.RequireAuthorization(InterServiceConstants.MTlsScheme);
        }

        return endpointBuilder;
    }

    #region Private Helper Methods

    /// <summary>
    /// Configures authentication schemes based on the security options.
    /// </summary>
    private static void ConfigureAuthentication(
        IServiceCollection services,
        InterServiceSecurityOptions options
    )
    {
        var authBuilder = services.AddAuthentication();

        // Configure API Key authentication if enabled
        if (options.Authentication.Method == AuthenticationMethod.ApiKey)
        {
            authBuilder.AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                InterServiceConstants.ApiKeyScheme,
                configureOptions =>
                {
                    configureOptions.ApiKeyHeaderName = options.ApiKey.HeaderName;
                    configureOptions.ApiKeyValue = options.ApiKey.Value;
                    configureOptions.UserName = options.ApiKey.UserName;
                    configureOptions.UserRole = options.ApiKey.UserRole;
                    configureOptions.ValidServices = options.Authentication.AllowedServices;
                }
            );
        }

        // Configure certificate authentication for mTLS if enabled
        // TODO: Implement mTLS authentication when Microsoft.AspNetCore.Authentication.Certificate package is added
        if (options.Authentication.Method == AuthenticationMethod.MTls)
        {
            // For now, mTLS authentication requires additional package:
            // Microsoft.AspNetCore.Authentication.Certificate
            throw new NotImplementedException(InterServiceConstants.MTlsPackageRequiredError);
        }
    }

    /// <summary>
    /// Configures authorization policies for inter-service communication.
    /// </summary>
    private static void ConfigureAuthorization(
        IServiceCollection services,
        InterServiceSecurityOptions options
    )
    {
        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                InterServiceConstants.ApiKeyScheme,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddAuthenticationSchemes(InterServiceConstants.ApiKeyScheme);
                    policy.RequireClaim(
                        InterServiceConstants.ScopeClaim,
                        options.Authentication.RequiredScope
                    );
                }
            )
            .AddPolicy(
                InterServiceConstants.MTlsScheme,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddAuthenticationSchemes(InterServiceConstants.MTlsScheme);
                    policy.RequireClaim(
                        InterServiceConstants.ScopeClaim,
                        options.Authentication.RequiredScope
                    );
                }
            );
    }

    #endregion Private Helper Methods
}
