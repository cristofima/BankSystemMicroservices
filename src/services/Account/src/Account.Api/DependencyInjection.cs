using BankSystem.Account.Api.Middlewares;
using BankSystem.Account.Api.Services;
using BankSystem.Account.Application.Behaviours;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Infrastructure.Data;
using BankSystem.Shared.ServiceDefaults.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace BankSystem.Account.Api;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddWebApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add shared service defaults with Controllers configuration
        services.AddServiceDefaults(configuration,
            "Account API",
            configureControllers: options =>
        {
            // Global model validation
            options.ModelValidatorProviders.Clear();
        });

        services.AddHttpContextAccessor();
        services.AddScoped<IAuthenticatedUserService, AuthenticatedUserService>();

        services.AddTransient<ExceptionHandlingMiddleware>();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(Application.IAssemblyReference).Assembly);
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        // Add health checks
        services.AddDbContextHealthCheck<AccountDbContext>();

        return services;
    }
}