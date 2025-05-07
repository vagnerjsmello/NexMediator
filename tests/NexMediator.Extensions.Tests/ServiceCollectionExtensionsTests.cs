using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Core;
using NexMediator.Extensions.Tests.Helpers;

namespace NexMediator.Extensions.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "AddNexMediator: without configure registers defaults and returns same collection")]
    public void AddNexMediator_NoConfigure_RegistersDefaults()
    {
        // Arrange: create a fresh service collection and register any test handler types needed for scanning
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        // Register dummy types for scanning
        services.AddSingleton<TestQueryHandler>();
        services.AddSingleton<TestNotificationHandler>();
        services.AddSingleton<IValidator<TestQuery>, TestQueryValidator>();

        // Act: call the extension method without providing a configure delegate
        var returned = services.AddNexMediator();

        // Assert: the extension returns the same IServiceCollection instance
        Assert.Same(services, returned);

        // Build the provider to resolve services
        var provider = services.BuildServiceProvider();

        // Assert: NexMediatorOptions is registered as a singleton
        var options = provider.GetService<NexMediatorOptions>();
        Assert.NotNull(options);

        // Assert: INexMediator resolves to our DefaultNexMediator implementation
        var mediator = provider.GetService<INexMediator>();
        Assert.IsType<DefaultNexMediator>(mediator);

        // Assert: the dummy request handler was discovered by the assembly scan
        var reqHandlers = provider.GetServices<INexRequestHandler<TestQuery, int>>();
        Assert.Contains(reqHandlers, h => h.GetType() == typeof(TestQueryHandler));

        // Assert: the dummy notification handler was discovered
        var notHandlers = provider.GetServices<INexNotificationHandler<TestNotification>>();
        Assert.Contains(notHandlers, h => h.GetType() == typeof(TestNotificationHandler));

        // Assert: the dummy validator was discovered
        var validators = provider.GetServices<IValidator<TestQuery>>();
        Assert.Contains(validators, v => v.GetType() == typeof(TestQueryValidator));
    }



    [Fact(DisplayName = "AddNexMediator: configure delegate is invoked")]
    public void AddNexMediator_WithConfigure_InvokesDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        bool called = false;

        // Act
        services.AddNexMediator(opts =>
        {
            called = true;
        });

        // Assert
        Assert.True(called, "Configure delegate should have been invoked");
    }




}
