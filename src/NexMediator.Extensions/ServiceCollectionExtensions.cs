using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Core;
using NexMediator.Pipeline.Behaviors;


namespace NexMediator.Extensions;

/// <summary>
/// Extension method to register NexMediator and its dependencies
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all NexMediator services in the DI container
    /// </summary>
    /// <param name="services">The service collection to add to</param>
    /// <param name="configure">A delegate to configure pipeline behaviors</param>
    public static IServiceCollection AddNexMediator(this IServiceCollection services, Action<NexMediatorOptions>? configure = null)
    {
        // Create configuration options with access to the service collection
        var options = new NexMediatorOptions(services);
        configure?.Invoke(options);

        //Minimal logging infra for behaviors only
        if (options.HasBehavior(typeof(LoggingBehavior<,>)))
        {
            services.TryAddSingleton<ILoggerFactory, LoggerFactory>();
            services.TryAddTransient(typeof(ILogger<>), typeof(Logger<>));
        }

        // Register mediator core components
        services.AddSingleton(options);
        services.AddSingleton<INexMediator, DefaultNexMediator>();

        // Automatically register request handlers, notification handlers, and validators
        services.Scan(scan => scan.FromApplicationDependencies()

            // Register all request handlers
            .AddClasses(c => c.AssignableTo(typeof(INexRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime()

            // Register all notification handlers
            .AddClasses(c => c.AssignableTo(typeof(INexNotificationHandler<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime()

            // Register all FluentValidation validators
            .AddClasses(c => c.AssignableTo(typeof(FluentValidation.IValidator<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );

        return services;
    }
}
