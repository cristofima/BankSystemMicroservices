using Asp.Versioning;
using BankSystem.Account.Api.Middlewares;
using BankSystem.Account.Api.Services;
using BankSystem.Account.Application.Behaviours;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Infrastructure.Extensions;
using System.Diagnostics.CodeAnalysis;
using BankSystem.Account.Infrastructure.Data;

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

        services.AddJwtAuthentication(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<IAuthenticatedUserService, AuthenticatedUserService>();

        services.AddTransient<ExceptionHandlingMiddleware>();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(Application.IAssemblyReference).Assembly);
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        // Add health checks
        services.AddHealthChecks()
            .AddDbContextCheck<AccountDbContext>("database");

        return services;
    }
}