using BankSystem.Shared.Application.Behaviours;
using Microsoft.Extensions.DependencyInjection;

namespace BankSystem.Shared.Application.Extensions;

public static class PipelineBehaviorExtensions
{
    /// <summary>
    /// Adds pipeline behaviors for cross-cutting concerns to MediatR configuration
    /// This should be called when configuring MediatR in the Application layer
    /// </summary>
    /// <param name="config">MediatR service configuration</param>
    /// <returns>The MediatR service configuration for method chaining</returns>
    public static MediatRServiceConfiguration AddPipelineBehaviors(
        this MediatRServiceConfiguration config
    )
    {
        // Add pipeline behaviors for cross-cutting concerns
        config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));

        return config;
    }
}
