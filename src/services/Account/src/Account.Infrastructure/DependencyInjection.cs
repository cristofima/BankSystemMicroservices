using System.Diagnostics.CodeAnalysis;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Infrastructure.Data;
using BankSystem.Account.Infrastructure.Repositories;
using BankSystem.Shared.Auditing;
using BankSystem.Shared.Infrastructure.DomainEvents;
using BankSystem.Shared.Infrastructure.Extensions;
using BankSystem.Shared.Kernel.Common;
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

        // Domain Events infra (emitter/dispatcher)
        services.AddDomainEventEmission();

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<SaveChangesInterceptor, DomainEventDispatchInterceptor>();
        services.AddScoped<SaveChangesInterceptor, AuditSaveChangesInterceptor>();

        services.AddEntityFrameworkOutbox<AccountDbContext>(
            configuration,
            DatabaseEngine.PostgreSql
        );

        return services;
    }
}
