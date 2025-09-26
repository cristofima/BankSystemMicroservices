using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BankSystem.Account.Application.Mappings;
using BankSystem.Shared.Application.Extensions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BankSystem.Account.Application;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add MediatR with all handlers and pipeline behaviors from this assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddPipelineBehaviors();
        });

        // Add FluentValidation validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add AutoMapper with mapping profiles
        services.AddAutoMapper(typeof(AccountMappingProfile));

        return services;
    }
}
