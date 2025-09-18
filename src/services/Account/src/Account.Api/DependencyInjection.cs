using System.Diagnostics.CodeAnalysis;
using BankSystem.Account.Api.Middlewares;
using BankSystem.Account.Application.Behaviours;
using BankSystem.Shared.WebApiDefaults.Extensions;

namespace BankSystem.Account.Api;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Add shared service defaults with Controllers configuration
        services.AddWebApiDefaults(
            configuration,
            "Account API",
            configureControllers: options =>
            {
                // Global model validation
                options.ModelValidatorProviders.Clear();
            }
        );

        services.AddTransient<ExceptionHandlingMiddleware>();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(Application.IAssemblyReference).Assembly);
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        return services;
    }
}
