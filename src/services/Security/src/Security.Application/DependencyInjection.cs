using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Security.Application.Services;
using Security.Domain.Interfaces;

namespace Security.Application;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
        );

        services.AddAutoMapper(typeof(Mapping.UserContactMappingProfile));

        // Register all validators from the Application assembly
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IGrpcValidationService, GrpcValidationService>();

        return services;
    }
}
