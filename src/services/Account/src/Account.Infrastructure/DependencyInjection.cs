using System.Diagnostics.CodeAnalysis;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Infrastructure.Data;
using BankSystem.Account.Infrastructure.Repositories;
using BankSystem.Shared.Auditing;
using BankSystem.Shared.Domain.Events.Account;
using BankSystem.Shared.Infrastructure.DomainEvents;
using BankSystem.Shared.Infrastructure.Extensions;
using BankSystem.Shared.Kernel.Common;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankSystem.Account.Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Configure DbContext
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");

        ConfigureDb(services, configuration, connectionString);
        ConfigureInterceptors(services);
        ConfigureMessaging(services, configuration);

        return services;
    }

    private static void ConfigureDb(
        IServiceCollection services,
        IConfiguration configuration,
        string connectionString
    )
    {
        services.AddDbContext<AccountDbContext>(
            (sp, options) =>
            {
                options.UseNpgsql(
                    connectionString,
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                        sqlOptions.CommandTimeout(30);
                    }
                );

                foreach (var interceptor in sp.GetServices<SaveChangesInterceptor>())
                {
                    options.AddInterceptors(interceptor);
                }

                // Enable sensitive data logging only in development
                if (configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"))
                    options.EnableSensitiveDataLogging();
            }
        );
    }

    private static void ConfigureInterceptors(IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<SaveChangesInterceptor, DomainEventDispatchInterceptor>();
        services.AddScoped<SaveChangesInterceptor, AuditSaveChangesInterceptor>();
    }

    private static void ConfigureMessaging(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddMassTransitOutboxSupport<AccountDbContext>(
            configuration,
            DatabaseEngine.PostgreSql,
            "Account",
            configureMessageTypes: ConfigureAccountMessageTypes
        );
    }

    /// <summary>
    /// Configures message types specific to the Account domain.
    /// This method defines all domain events and message types that the Account service publishes.
    /// </summary>
    /// <param name="busConfigurator">The bus configurator to configure message types with.</param>
    /// <param name="_">The registration context for configuring consumers.</param>
    private static void ConfigureAccountMessageTypes(
        IServiceBusBusFactoryConfigurator busConfigurator,
        IRegistrationContext _
    )
    {
        // Configure Account domain publishing using best practices pattern
        // This creates the "account-events" topic for Account domain events
        busConfigurator.ConfigureDomainPublishing(
            "account",
            typeof(AccountCreatedEvent),
            typeof(AccountSuspendedEvent),
            typeof(AccountActivatedEvent),
            typeof(AccountClosedEvent),
            typeof(AccountFrozenEvent)
        );
    }
}
