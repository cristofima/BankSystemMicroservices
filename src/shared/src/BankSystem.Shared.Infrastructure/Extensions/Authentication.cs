using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;

namespace BankSystem.Shared.Infrastructure.Extensions;

[ExcludeFromCodeCoverage]
public static class Authentication
{
    // Configure JWT authentication
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Get JWT options for authentication configuration
        var jwtSection = configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key not configured");

        // Configure JWT Authentication with proper validation
        services.AddAuthentication(options =>
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
                ValidateIssuerSigningKey = jwtSection.GetValue("ValidateIssuerSigningKey", true),
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero, // No tolerance for clock skew
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            // Add event handlers for enhanced security
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // Log authentication failures
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                    if (claimsIdentity?.Claims?.Any() != true)
                    {
                        context.Fail("Token has no claims");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}