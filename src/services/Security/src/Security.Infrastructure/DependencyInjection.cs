using BankSystem.Shared.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Security.Application.Configuration;
using Security.Application.Interfaces;
using Security.Domain.Entities;
using Security.Infrastructure.Data;
using Security.Infrastructure.Services;
using Security.Infrastructure.Validators;
using System.Diagnostics.CodeAnalysis;

namespace Security.Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options with validation
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Database connection string not configured");

        services.AddDbContext<SecurityDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });

            // Enable sensitive data logging only in development
            if (configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"))
                options.EnableSensitiveDataLogging();
        });

        // Configure Identity with enhanced security
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password requirements
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 4;

            // User requirements
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            // Lockout configuration
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // Sign-in requirements
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<SecurityDbContext>()
        .AddDefaultTokenProviders()
        .AddPasswordValidator<CustomPasswordValidator>();

        services.AddJwtAuthentication(configuration);

        // Configure Authorization policies using AddAuthorizationBuilder
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAuthenticatedUser", policy =>
                policy.RequireAuthenticatedUser())
            .AddPolicy("RequireAdminRole", policy =>
                policy.RequireRole("Admin"))
            .AddPolicy("RequireManagerRole", policy =>
                policy.RequireRole("Manager", "Admin"));

        // Register application services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();

        return services;
    }
}