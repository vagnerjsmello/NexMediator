using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Core.Tests.Helpers;
using NexMediator.Core.Tests.Helpers.Broken;
using NexMediator.Extensions;
using NexMediator.Pipeline.Behaviors;
using System.Reflection;

namespace NexMediator.Core.Tests.Mediator;

/// <summary>
/// Integration tests for NexMediator covering Send, Publish, Stream behaviors and exception handling.
/// </summary>
public class NexMediatorTests
{
    private ServiceProvider BuildConfiguredServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITransactionManager, FakeTransactionManager>();
        services.AddSingleton<ICache, FakeCache>();
        configure?.Invoke(services);
        services.AddNexMediator();
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Ensures that DefaultNexMediator throws ArgumentNullException when required dependencies are null.
    /// Logger is allowed to be null.
    /// </summary>
    [Fact]
    public void Constructor_Should_Throw_When_Required_Arguments_Are_Null()
    {
        var services = new ServiceCollection();
        var options = new NexMediatorOptions(services);
        var provider = services.BuildServiceProvider();
        var logger = Mock.Of<ILogger<DefaultNexMediator>>();

        Assert.Throws<ArgumentNullException>(() => new DefaultNexMediator(null!, logger, options));
        Assert.Throws<ArgumentNullException>(() => new DefaultNexMediator(provider, logger, null!));
    }

    /// <summary>
    /// Ensures that Send throws NullReferenceException when a proxy handler returns null Task.
    /// </summary>
    [Fact]
    public async Task Send_Should_Throw_NullReference_When_Proxy_Handler_Returns_Null()
    {
        var request = new CachedRequest();

        var proxy = DispatchProxy.Create<INexRequestHandler<CachedRequest, SampleResponse>, InvalidHandlerProxy>();

        var provider = BuildConfiguredServices(services =>
        {
            services.AddSingleton<INexRequestHandler<CachedRequest, SampleResponse>>(proxy);
        });

        var mediator = provider.GetRequiredService<INexMediator>();

        var act = () => mediator.Send<SampleResponse>(request);

        await act.Should().ThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Ensures that Send throws NullReferenceException when a reflection-invoked handler returns null.
    /// </summary>
    [Fact]
    public async Task Send_Should_Throw_NullReference_When_Reflection_Handler_Returns_Null()
    {
        var request = new CachedRequest();

        var provider = BuildConfiguredServices(services =>
        {
            services.AddSingleton<INexRequestHandler<CachedRequest, SampleResponse>, ReflectionNullHandler>();
        });

        var mediator = provider.GetRequiredService<INexMediator>();

        var act = () => mediator.Send<SampleResponse>(request);

        await act.Should().ThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Ensures that Send throws NullReferenceException when a pipeline behavior created via proxy returns null.
    /// </summary>
    [Fact]
    public async Task Send_Should_Throw_NullReference_When_Proxy_Behavior_Returns_Null()
    {
        var request = new CachedRequest();
        var handlerMock = new Mock<INexRequestHandler<CachedRequest, SampleResponse>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<CachedRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SampleResponse());

        var proxy = DispatchProxy.Create<INexPipelineBehavior<CachedRequest, SampleResponse>, InvalidBehaviorProxy>();

        var provider = BuildConfiguredServices(services =>
        {
            services.AddSingleton<INexRequestHandler<CachedRequest, SampleResponse>>(handlerMock.Object);
            services.AddSingleton(typeof(INexPipelineBehavior<CachedRequest, SampleResponse>), proxy);
        });

        var mediator = provider.GetRequiredService<INexMediator>();

        var act = () => mediator.Send<SampleResponse>(request);

        await act.Should().ThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Ensures that Send throws NullReferenceException when a concrete pipeline behavior returns null.
    /// </summary>
    [Fact]
    public async Task Send_Should_Throw_NullReference_When_Concrete_Behavior_Returns_Null()
    {
        var provider = BuildConfiguredServices(services =>
        {
            services.AddSingleton<INexRequestHandler<BrokenRequest, string>, WorkingBrokenHandler>();
            services.AddSingleton(typeof(INexPipelineBehavior<BrokenRequest, string>), typeof(BrokenBehavior<BrokenRequest, string>));
        });

        var mediator = provider.GetRequiredService<INexMediator>();
        var request = new BrokenRequest();

        var act = () => mediator.Send(request);

        await act.Should().ThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Validates that Send invokes the correct handler and returns the expected response.
    /// </summary>
    [Fact]
    public async Task Send_Should_Invoke_Handler_And_Return_Response()
    {
        var request = new SampleRequest { Data = "test" };
        var expected = new SampleResponse { Result = "test processed" };

        var provider = BuildConfiguredServices(); // SampleRequestHandler is auto-registered

        var mediator = provider.GetRequiredService<INexMediator>();

        var result = await mediator.Send(request);

        result.Should().NotBeNull();
        result.Result.Should().Be(expected.Result);
    }



    /// <summary>
    /// Ensures that all notification handlers are invoked when a notification is published.
    /// </summary>
    [Fact]
    public async Task Publish_Should_Invoke_All_NotificationHandlers()
    {
        var notification = new SampleNotification();
        var handler1 = new Mock<INexNotificationHandler<SampleNotification>>();
        var handler2 = new Mock<INexNotificationHandler<SampleNotification>>();

        var provider = BuildConfiguredServices(services =>
        {
            services.AddSingleton(handler1.Object);
            services.AddSingleton(handler2.Object);
        });

        var mediator = provider.GetRequiredService<INexMediator>();

        await mediator.Publish(notification);

        handler1.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        handler2.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Ensures that a stream request handler returns all expected results.
    /// </summary>
    [Fact]
    public async Task Stream_Should_Return_All_Items_From_Handler()
    {
        var request = new SampleStreamRequest();
        var responses = new List<string> { "a", "b", "c" };

        var handlerMock = new Mock<INexStreamRequestHandler<SampleStreamRequest, string>>();
        handlerMock
            .Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
            .Returns(GetAsyncEnumerable(responses));

        var provider = BuildConfiguredServices(services =>
        {
            services.AddSingleton<INexStreamRequestHandler<SampleStreamRequest, string>>(handlerMock.Object);
        });

        var mediator = provider.GetRequiredService<INexMediator>();

        var result = new List<string>();
        await foreach (var item in mediator.Stream(request))
        {
            result.Add(item);
        }

        result.Should().BeEquivalentTo(responses);
    }

    /// <summary>
    /// Ensures that Publish does not throw when there are no registered handlers.
    /// </summary>
    [Fact]
    public async Task Publish_Should_Not_Throw_When_No_Handlers()
    {
        var notification = new SampleNotification();
        var provider = BuildConfiguredServices();
        var mediator = provider.GetRequiredService<INexMediator>();

        var act = async () => await mediator.Publish(notification);

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Ensures that Stream throws an exception when no handler is registered.
    /// </summary>
    [Fact]
    public async Task Stream_Should_Throw_When_No_Handler_Registered()
    {
        var request = new SampleStreamRequest();
        var provider = BuildConfiguredServices();
        var mediator = provider.GetRequiredService<INexMediator>();

        var act = async () =>
        {
            await foreach (var _ in mediator.Stream(request)) { }
        };

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No service for type*INexStreamRequestHandler*");
    }

    /// <summary>
    /// Ensures that Stream throws NullReferenceException when the handler returns null.
    /// </summary>
    [Fact]
    public async Task Stream_Should_Throw_NullReference_When_Handler_Returns_Null()
    {
        var provider = BuildConfiguredServices(services =>
        {
            services.AddSingleton<INexStreamRequestHandler<BrokenStreamRequest, string>, FakeInvalidStreamHandler>();
        });

        var mediator = provider.GetRequiredService<INexMediator>();
        var request = new BrokenStreamRequest();

        var act = async () =>
        {
            await foreach (var _ in mediator.Stream(request)) { }
        };

        await act.Should().ThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Ensures that Send throws ArgumentNullException when the request is null.
    /// </summary>
    [Fact]
    public async Task Send_Should_Throw_When_Request_Is_Null()
    {
        var mediator = NexMediatorBuilder.BuildEmptyMediator();

        Func<Task> act = () => mediator.Send<string>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    /// <summary>
    /// Ensures that Publish throws ArgumentNullException when the notification is null.
    /// </summary>
    [Fact]
    public async Task Publish_Should_Throw_When_Notification_Is_Null()
    {
        var mediator = NexMediatorBuilder.BuildEmptyMediator();

        Func<Task> act = () => mediator.Publish<SampleNotification>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("notification");
    }

    /// <summary>
    /// Ensures that Stream throws ArgumentNullException when the request is null.
    /// </summary>
    [Fact]
    public async Task Stream_Should_Throw_When_Request_Is_Null()
    {
        var mediator = NexMediatorBuilder.BuildEmptyMediator();

        var act = async () =>
        {
            await foreach (var _ in mediator.Stream<string>(null!)) { }
        };

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    /// <summary>
    /// Ensures that Validate detects duplicate behavior order configuration.
    /// </summary>
    [Fact]
    public void Validate_Should_Return_DuplicateOrderWarnings()
    {
        var services = new ServiceCollection();
        var options = new NexMediatorOptions(services);

        options.AddBehavior(typeof(LoggingBehavior<,>), 1);
        options.AddBehavior(typeof(TransactionBehavior<,>), 1);

        var result = options.Validate();

        result.Should().Contain(r => r.Contains("Multiple behaviors with order 1"));
    }

    private async IAsyncEnumerable<string> GetAsyncEnumerable(IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            await Task.Delay(1);
            yield return value;
        }
    }
}
