using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace BankSystem.Shared.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for configuring JWT authentication and authorization services
/// in the dependency injection container. This class contains methods to set up secure
/// JWT-based authentication with proper token validation and security event handling.
/// </summary>
/// <remarks>
/// This static class extends IServiceCollection to provide fluent configuration methods
/// for JWT authentication. The methods configure authentication schemes, token validation
/// parameters, and security event handlers to ensure secure API access across the banking
/// system microservices.
/// </remarks>
[ExcludeFromCodeCoverage]
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Configures JWT Bearer authentication with comprehensive security validation parameters
    /// and event handlers for the banking system. This method sets up token validation,
    /// issuer and audience verification, and security event logging.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the authentication services to</param>
    /// <param name="configuration">The IConfiguration instance containing JWT settings from appsettings</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    /// <remarks>
    /// This method configures JWT authentication with the following security features:
    /// - Issuer and audience validation for token integrity
    /// - Signing key validation using symmetric encryption
    /// - Zero clock skew tolerance for precise timing validation
    /// - Mandatory token expiration and signature requirements
    /// - Comprehensive authentication failure logging
    /// - Claims identity validation for authorized access
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the required JWT Key configuration is missing</exception>
    /// <exception cref="ArgumentNullException">Thrown when services or configuration parameters are null</exception>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Get JWT options for authentication configuration
        var jwtSection = configuration.GetSection("Jwt");
        var jwtKey =
            jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key not configured");

        // Configure JWT Authentication with proper validation
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = jwtSection.GetValue("ValidateIssuer", true),
                    ValidateAudience = jwtSection.GetValue("ValidateAudience", true),
                    ValidateLifetime = jwtSection.GetValue("ValidateLifetime", true),
                    ValidateIssuerSigningKey = jwtSection.GetValue(
                        "ValidateIssuerSigningKey",
                        true
                    ),
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero, // No tolerance for clock skew
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                };

                // Add event handlers for enhanced security and proper error handling
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Log authentication failures
                        var logger = context.HttpContext.RequestServices.GetRequiredService<
                            ILogger<JwtBearerEvents>
                        >();
                        logger.LogWarning(
                            "JWT authentication failed: {Error}",
                            context.Exception.Message
                        );
                        
                        // Don't handle the response here - let it be handled by ExceptionHandlingMiddleware
                        // Just mark the authentication as failed
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        // Skip the default challenge response
                        context.HandleResponse();
                        
                        // Throw an exception that will be caught by ExceptionHandlingMiddleware
                        throw new UnauthorizedAccessException(
                            string.IsNullOrEmpty(context.ErrorDescription) 
                                ? "Authentication is required to access this resource"
                                : context.ErrorDescription
                        );
                    },
                    OnTokenValidated = context =>
                    {
                        var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                        if (claimsIdentity?.Claims == null)
                        {
                            context.Fail("Token has no claims identity or claims collection");
                        }
                        return Task.CompletedTask;
                    },
                };
            });

        return services;
    }
}
