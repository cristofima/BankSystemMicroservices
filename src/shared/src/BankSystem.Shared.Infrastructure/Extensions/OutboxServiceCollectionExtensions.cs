using System.Diagnostics.CodeAnalysis;
using BankSystem.Shared.Kernel.Common;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankSystem.Shared.Infrastructure.Extensions;

[ExcludeFromCodeCoverage]
public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkOutbox<TDbContext>(
        this IServiceCollection services,
        DatabaseEngine databaseEngine
    )
        where TDbContext : DbContext
    {
        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddEntityFrameworkOutbox<TDbContext>(outboxConfigurator =>
            {
                switch (databaseEngine)
                {
                    case DatabaseEngine.SqlServer:
                        outboxConfigurator.UseSqlServer();
                        break;

                    case DatabaseEngine.PostgreSql:
                        outboxConfigurator.UsePostgres();
                        break;
                }

                // Query Configuration
                outboxConfigurator.QueryDelay = TimeSpan.FromSeconds(1);
                outboxConfigurator.DuplicateDetectionWindow = TimeSpan.FromMinutes(5);

                // Cleanup Configuration
                outboxConfigurator.DisableInboxCleanupService();

                // Use Bus Outbox for better performance
                outboxConfigurator.UseBusOutbox();
            });
        });

        return services;
    }
}
