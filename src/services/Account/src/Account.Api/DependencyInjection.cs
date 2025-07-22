using Asp.Versioning;
using BankSystem.Account.Api.Middlewares;
using BankSystem.Account.Api.Services;
using BankSystem.Account.Application.Behaviours;
using BankSystem.Account.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BankSystem.Account.Api;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddWebApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
        {
            // Global model validation
            options.ModelValidatorProviders.Clear();
        });

        // Configure OpenAPI/Scalar
        services.AddOpenApi();

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

        // Get JWT options for authentication configuration
        var jwtSection = configuration.GetSection("JWT");
        var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key not configured");

        // Configure JWT Authentication with proper validation
        services.AddAuthorization().AddAuthentication(options =>
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
        });

        services.AddHttpContextAccessor();
        services.AddScoped<IAuthenticatedUserService, AuthenticatedUserService>();

        services.AddTransient<ExceptionHandlingMiddleware>();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(Application.IAssemblyReference).Assembly);
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        return services;
    }
}