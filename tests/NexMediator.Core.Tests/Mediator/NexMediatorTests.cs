using FluentAssertions;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Core.Tests.Helpers;
using NexMediator.Core.Tests.Helpers.Broken;
using NexMediator.Extensions;
using NexMediator.Pipeline.Behaviors;
using System.Reflection;

namespace NexMediator.Core.Tests.Mediator;

/// <summary>
/// Integration tests for DefaultNexMediator covering Send, Publish, Stream, and options validation.
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
    /// Ctor: given null provider or options, throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Ctor_GivenNullProviderOrOptions_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var options = new NexMediatorOptions(services);
        var provider = services.BuildServiceProvider();

        Assert.Throws<ArgumentNullException>(() => new DefaultNexMediator(null!, options));
        Assert.Throws<ArgumentNullException>(() => new DefaultNexMediator(provider, null!));
    }

    /// <summary>
    /// Send: given proxy handler returns null Task, throws RuntimeBinderException.
    /// </summary>
    [Fact]
    public async Task Send_GivenProxyHandlerReturnsNullTask_ThrowsRuntimeBinderException()
    {
        var request = new CachedRequest();
        var proxy = DispatchProxy.Create<INexRequestHandler<CachedRequest, SampleResponse>, InvalidHandlerProxy>();

        var provider = BuildConfiguredServices(services =>
            services.AddSingleton<INexRequestHandler<CachedRequest, SampleResponse>>(proxy)
        );
        var mediator = provider.GetRequiredService<INexMediator>();

        Func<Task> act = () => mediator.Send<SampleResponse>(request);
        await act.Should().ThrowAsync<RuntimeBinderException>();
    }

    /// <summary>
    /// Send: given reflection handler returns null, throws RuntimeBinderException.
    /// </summary>
    [Fact]
    public async Task Send_GivenReflectionHandlerReturnsNull_ThrowsRuntimeBinderException()
    {
        var request = new CachedRequest();
        var provider = BuildConfiguredServices(services =>
            services.AddSingleton<INexRequestHandler<CachedRequest, SampleResponse>, ReflectionNullHandler>()
        );
        var mediator = provider.GetRequiredService<INexMediator>();

        Func<Task> act = () => mediator.Send<SampleResponse>(request);
        await act.Should().ThrowAsync<RuntimeBinderException>();
    }

    /// <summary>
    /// Send: given proxy behavior returns null, throws RuntimeBinderException.
    /// </summary>
    [Fact]
    public async Task Send_GivenProxyBehaviorReturnsNull_ThrowsRuntimeBinderException()
    {
        var request = new CachedRequest();
        var handlerMock = new Mock<INexRequestHandler<CachedRequest, SampleResponse>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<CachedRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SampleResponse());

        var proxy = DispatchProxy.Create<INexPipelineBehavior<CachedRequest, SampleResponse>, InvalidBehaviorProxy>();
        var proxyType = proxy.GetType();

        var provider = BuildConfiguredServices(services =>
        {
            services.AddSingleton(handlerMock.Object);
            services.AddSingleton(typeof(INexPipelineBehavior<CachedRequest, SampleResponse>), proxy);
            services.AddSingleton(proxyType, proxy);
        });
        var mediator = provider.GetRequiredService<INexMediator>();

        Func<Task> act = () => mediator.Send<SampleResponse>(request);
        await act.Should().ThrowAsync<RuntimeBinderException>();
    }

    /// <summary>
    /// Send: given concrete behavior returns null, throws RuntimeBinderException.
    /// </summary>
    [Fact]
    public async Task Send_GivenConcreteBehaviorReturnsNull_ThrowsRuntimeBinderException()
    {
        var provider = BuildConfiguredServices(services =>
        {
            services.AddSingleton<INexRequestHandler<BrokenRequest, string>, WorkingBrokenHandler>();
            services.AddSingleton<BrokenBehavior<BrokenRequest, string>>();
            services.AddSingleton(
                typeof(INexPipelineBehavior<BrokenRequest, string>),
                typeof(BrokenBehavior<BrokenRequest, string>)
            );
        });
        var mediator = provider.GetRequiredService<INexMediator>();
        var request = new BrokenRequest();

        Func<Task> act = () => mediator.Send<string>(request);
        await act.Should().ThrowAsync<RuntimeBinderException>();
    }

    /// <summary>
    /// Send: given valid request, calls handler and returns response.
    /// </summary>
    [Fact]
    public async Task Send_GivenValidRequest_CallsHandlerAndReturnsResponse()
    {
        var request = new SampleRequest { Data = "test" };
        var provider = BuildConfiguredServices(); // SampleRequestHandler is auto-registered

        var mediator = provider.GetRequiredService<INexMediator>();
        var result = await mediator.Send(request);

        result.Should().NotBeNull();
        result.Result.Should().Be("test processed");
    }

    /// <summary>
    /// Publish: given handlers registered, invokes all handlers once.
    /// </summary>
    [Fact]
    public async Task Publish_GivenHandlersRegistered_InvokesAllHandlers()
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
    /// Stream: given handler returns sequence, yields all items.
    /// </summary>
    [Fact]
    public async Task Stream_GivenHandlerReturnsSequence_YieldsAllItems()
    {
        var request = new SampleStreamRequest();
        var responses = new List<string> { "a", "b", "c" };

        var handlerMock = new Mock<INexStreamRequestHandler<SampleStreamRequest, string>>();
        handlerMock
            .Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
            .Returns(GetAsyncEnumerable(responses));

        var provider = BuildConfiguredServices(services =>
            services.AddSingleton(handlerMock.Object)
        );
        var mediator = provider.GetRequiredService<INexMediator>();

        var result = new List<string>();
        await foreach (var item in mediator.Stream(request))
        {
            result.Add(item);
        }
        result.Should().BeEquivalentTo(responses);
    }

    /// <summary>
    /// Publish: given no handlers, does not throw.
    /// </summary>
    [Fact]
    public async Task Publish_GivenNoHandlers_DoesNotThrow()
    {
        var notification = new SampleNotification();
        var mediator = BuildConfiguredServices().GetRequiredService<INexMediator>();

        Func<Task> act = () => mediator.Publish(notification);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Stream: given no handler registered, throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task Stream_GivenNoHandlerRegistered_ThrowsInvalidOperationException()
    {
        var mediator = BuildConfiguredServices().GetRequiredService<INexMediator>();
        var request = new SampleStreamRequest();

        Func<Task> act = async () =>
        {
            await foreach (var _ in mediator.Stream(request)) { }
        };
        await act.Should().ThrowAsync<InvalidOperationException>()
                  .WithMessage("*No service for type*INexStreamRequestHandler*");
    }

    /// <summary>
    /// Stream: given handler returns null, throws NullReferenceException.
    /// </summary>
    [Fact]
    public async Task Stream_GivenHandlerReturnsNull_ThrowsNullReferenceException()
    {
        var provider = BuildConfiguredServices(services =>
            services.AddSingleton<INexStreamRequestHandler<BrokenStreamRequest, string>, FakeInvalidStreamHandler>()
        );
        var mediator = provider.GetRequiredService<INexMediator>();
        var request = new BrokenStreamRequest();

        Func<Task> act = async () =>
        {
            await foreach (var _ in mediator.Stream(request)) { }
        };
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Send: given null request, throws ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task Send_GivenNullRequest_ThrowsArgumentNullException()
    {
        var mediator = NexMediatorBuilder.BuildEmptyMediator();

        Func<Task> act = () => mediator.Send<string>(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
                  .WithParameterName("request");
    }

    /// <summary>
    /// Publish: given null notification, throws ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task Publish_GivenNullNotification_ThrowsArgumentNullException()
    {
        var mediator = NexMediatorBuilder.BuildEmptyMediator();

        Func<Task> act = () => mediator.Publish<SampleNotification>(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
                  .WithParameterName("notification");
    }

    /// <summary>
    /// Stream: given null request, throws ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task Stream_GivenNullRequest_ThrowsArgumentNullException()
    {
        var mediator = NexMediatorBuilder.BuildEmptyMediator();

        Func<Task> act = async () =>
        {
            await foreach (var _ in mediator.Stream<string>(null!)) { }
        };
        await act.Should().ThrowAsync<ArgumentNullException>()
                  .WithParameterName("request");
    }

    /// <summary>
    /// Validate: given duplicate behavior orders, returns warnings.
    /// </summary>
    [Fact]
    public void Validate_GivenDuplicateBehaviorOrders_ReturnsWarnings()
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
